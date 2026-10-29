param(
    [string]$DatabasePath = (Join-Path (Get-Location).Path 'Edu_Connect.accdb'),
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$DatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
if (-not (Test-Path -LiteralPath $DatabasePath)) { throw "Database not found: $DatabasePath" }
if (Test-Path -LiteralPath ([System.IO.Path]::ChangeExtension($DatabasePath, '.laccdb'))) {
    throw 'Close Microsoft Access and EduConnect before running this script. The database lock file exists.'
}

function Open-Database([string]$path) {
    foreach ($provider in @('Microsoft.ACE.OLEDB.12.0','Microsoft.ACE.OLEDB.16.0')) {
        $connection = New-Object System.Data.OleDb.OleDbConnection("Provider=$provider;Data Source=$path;Persist Security Info=False;")
        try { $connection.Open(); return $connection }
        catch { $connection.Dispose() }
    }
    throw 'Could not open the Access database. Check Access Database Engine architecture.'
}
function Query-Table($connection, [string]$sql, $tx = $null) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    if ($null -ne $tx) { $command.Transaction = $tx }
    $adapter = New-Object System.Data.OleDb.OleDbDataAdapter($command)
    $table = New-Object System.Data.DataTable
    try { [void]$adapter.Fill($table); return ,$table }
    finally { $adapter.Dispose(); $command.Dispose() }
}
function Scalar($connection, [string]$sql, $tx = $null) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    if ($null -ne $tx) { $command.Transaction = $tx }
    try { return $command.ExecuteScalar() }
    finally { $command.Dispose() }
}
function Exec-Sql($connection, $tx, [string]$sql) {
    $command = $connection.CreateCommand()
    $command.Transaction = $tx
    $command.CommandText = $sql
    try { return [int]$command.ExecuteNonQuery() }
    finally { $command.Dispose() }
}
function Require-Count($connection, $transaction, [string]$table, [int]$expected) {
    $actual = [int](Scalar $connection "SELECT COUNT(*) FROM [$table]" $transaction)
    if ($actual -ne $expected) { throw "Verification failed for $table : expected $expected, found $actual. Transaction will roll back." }
}

$connection = Open-Database $DatabasePath
try {
    $required = @('STUDENT','STUDENT_LOGIN','TUTOR','TUTOR_LOGIN','ADMIN','ADMIN_LOGIN','COURSE','TUTOR_COURSE','TUTOR_AVAILABILITY','STUDENTACADEMICRECORD','TUTORING_REQUEST','TUTORING_SESSION')
    $tableNames = @($connection.GetSchema('Tables').Rows | ForEach-Object { [string]$_['TABLE_NAME'] })
    foreach ($name in $required) {
        if (-not ($tableNames -contains $name)) { throw "Missing table $name; no changes made." }
    }
    $allUserTables = @($tableNames | Where-Object { $_ -notlike 'MSys*' -and $_ -notlike '~*' -and $_ -notlike 'USys*' })
    $unexpected = @($allUserTables | Where-Object { $required -notcontains $_ })
    if ($unexpected.Count -gt 0) { throw "Extra tables need review before deleting requests: $($unexpected -join ', '). No changes made." }

    $protected = @('STUDENT','STUDENT_LOGIN','TUTOR','TUTOR_LOGIN','ADMIN','ADMIN_LOGIN','COURSE','TUTOR_COURSE','TUTOR_AVAILABILITY','STUDENTACADEMICRECORD')
    $protectedCounts = @{}
    foreach ($name in $protected) { $protectedCounts[$name] = [int](Scalar $connection "SELECT COUNT(*) FROM [$name]") }

    $requests = Query-Table $connection 'SELECT [RequestID],[StudentID],[CourseID],[PreferredDate],[PreferredStartTime],[PreferredEndTime],[Status] FROM [TUTORING_REQUEST] WHERE [RequestID] IN (1,9)'
    if ($requests.Rows.Count -ne 2) { throw 'Both requests 1 and 9 must exist. Nothing changed.' }
    $sessionOne = Query-Table $connection 'SELECT [SessionID],[RequestID],[SessionDate] FROM [TUTORING_SESSION] WHERE [SessionID] = 1'
    if ($sessionOne.Rows.Count -ne 1 -or [int]$sessionOne.Rows[0]['RequestID'] -ne 1) {
        throw 'Expected SessionID 1 to belong to RequestID 1. Nothing changed.'
    }
    $requestOne = @($requests.Rows | Where-Object { [int]$_['RequestID'] -eq 1 })[0]
    $scheduledDate = ([datetime]$requestOne['PreferredDate']).Date
    $otherSessionsKept = [int](Scalar $connection 'SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [RequestID] IN (1,9)')
    $orphanSessions = [int](Scalar $connection 'SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [RequestID] NOT IN (SELECT [RequestID] FROM [TUTORING_REQUEST]) OR [RequestID] IS NULL')
    $beforeRequests = [int](Scalar $connection 'SELECT COUNT(*) FROM [TUTORING_REQUEST]')
    $beforeSessions = [int](Scalar $connection 'SELECT COUNT(*) FROM [TUTORING_SESSION]')
    Write-Host "Database: $DatabasePath"
    Write-Host "Requests: $beforeRequests -> 2 (preserve IDs 1 and 9; preserve their statuses)"
    Write-Host "Sessions: $beforeSessions -> $otherSessionsKept (only those linked to requests 1 and 9)"
    Write-Host "Orphan sessions to remove (included in deletion): $orphanSessions"
    Write-Host "Session #1 date -> $($scheduledDate.ToString('yyyy-MM-dd'))"
    foreach ($name in ($protected | Sort-Object)) { Write-Host "Preserve $name : $($protectedCounts[$name]) records" }
    if (-not $Apply) { Write-Host 'PREVIEW ONLY. No data changed. Run again with -Apply to perform cleanup.'; return }

    $connection.Close()
    [System.Data.OleDb.OleDbConnection]::ReleaseObjectPool()
    $backupPath = "$DatabasePath.before-team-cleanup-$(Get-Date -Format 'yyyyMMdd-HHmmss').accdb"
    Copy-Item -LiteralPath $DatabasePath -Destination $backupPath -ErrorAction Stop
    Write-Host "Backup created: $backupPath"
    $connection.Open()
    $tx = $connection.BeginTransaction()
    try {
        $removedSessions = Exec-Sql $connection $tx 'DELETE FROM [TUTORING_SESSION] WHERE [RequestID] NOT IN (1,9) OR [RequestID] IS NULL'
        $removedRequests = Exec-Sql $connection $tx 'DELETE FROM [TUTORING_REQUEST] WHERE [RequestID] NOT IN (1,9)'
        $dateSql = "UPDATE [TUTORING_SESSION] SET [SessionDate] = #$($scheduledDate.ToString('MM/dd/yyyy'))# WHERE [SessionID] = 1 AND [RequestID] = 1"
        [void](Exec-Sql $connection $tx $dateSql)
        Require-Count $connection $tx 'TUTORING_REQUEST' 2
        Require-Count $connection $tx 'TUTORING_SESSION' $otherSessionsKept
        foreach ($name in $protected) { Require-Count $connection $tx $name $protectedCounts[$name] }
        $orphanCheck = [int](Scalar $connection 'SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [RequestID] NOT IN (SELECT [RequestID] FROM [TUTORING_REQUEST]) OR [RequestID] IS NULL' $tx)
        if ($orphanCheck -ne 0) { throw "Orphan sessions remain: $orphanCheck" }
        $dateRow = Query-Table $connection 'SELECT [SessionDate] FROM [TUTORING_SESSION] WHERE [SessionID]=1' $tx
        if ($dateRow.Rows.Count -ne 1 -or ([datetime]$dateRow.Rows[0]['SessionDate']).Date -ne $scheduledDate) { throw 'Session #1 date verification failed.' }
        $tx.Commit()
        Write-Host "SUCCESS: deleted $removedRequests requests and $removedSessions sessions. Requests 1 and 9 retained."
        Write-Host 'Student, Tutor, Admin, login, GPA, course, and tutor availability tables were not modified.'
        Write-Host "Backup: $backupPath"
    }
    catch {
        try { $tx.Rollback() } catch {}
        throw
    }
    finally { $tx.Dispose() }
}
finally { $connection.Dispose() }
