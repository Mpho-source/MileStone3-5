param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) "Edu_Connect.accdb"),
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

$DatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
if (-not (Test-Path -LiteralPath $DatabasePath)) {
    throw "Database not found: $DatabasePath"
}

$lockFile = [System.IO.Path]::ChangeExtension($DatabasePath, ".laccdb")
if (Test-Path -LiteralPath $lockFile) {
    throw "Close Microsoft Access and EduConnect before resetting the database."
}

function New-DatabaseConnection([string]$path) {
    foreach ($provider in @("Microsoft.ACE.OLEDB.12.0", "Microsoft.ACE.OLEDB.16.0")) {
        $db = New-Object System.Data.OleDb.OleDbConnection(
            "Provider=$provider;Data Source=$path;Persist Security Info=False;")
        try {
            $db.Open()
            return $db
        }
        catch {
            $db.Dispose()
        }
    }
    throw "Unable to open Access. Install the Access Database Engine matching your PowerShell architecture."
}

function Has-Table($db, [string]$name) {
    foreach ($row in $db.GetSchema("Tables").Rows) {
        if ([string]::Equals([string]$row["TABLE_NAME"], $name,
            [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Read-Table($db, [string]$sql) {
    $result = New-Object System.Data.DataTable
    $command = $db.CreateCommand()
    $command.CommandText = $sql
    $adapter = New-Object System.Data.OleDb.OleDbDataAdapter($command)
    try { [void]$adapter.Fill($result) }
    finally { $adapter.Dispose(); $command.Dispose() }
    Write-Output -NoEnumerate $result
}

function Get-Count($db, [string]$table, $transaction = $null) {
    $cmd = $db.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM [$table]"
    if ($null -ne $transaction) { $cmd.Transaction = $transaction }
    try { return [int]$cmd.ExecuteScalar() }
    finally { $cmd.Dispose() }
}

function Execute-Statement($db, $tx, [string]$sql, [object[]]$parameters = @()) {
    $cmd = $db.CreateCommand()
    $cmd.Transaction = $tx
    $cmd.CommandText = $sql
    foreach ($parameter in $parameters) {
        [void]$cmd.Parameters.Add("@p", [System.Data.OleDb.OleDbType]::Integer)
        $cmd.Parameters[$cmd.Parameters.Count - 1].Value = [int]$parameter
    }
    try { return $cmd.ExecuteNonQuery() }
    finally { $cmd.Dispose() }
}

function Get-Time([object]$value) {
    if ($null -eq $value -or $value -is [System.DBNull]) {
        throw "Missing time"
    }
    if ($value -is [datetime]) { return $value.TimeOfDay }
    if ($value -is [timespan]) { return $value }
    return [datetime]::Parse([string]$value,
        [System.Globalization.CultureInfo]::InvariantCulture).TimeOfDay
}

$db = New-DatabaseConnection $DatabasePath
try {
    $mustExist = @("STUDENT", "TUTOR", "ADMIN", "COURSE",
        "TUTORING_REQUEST", "TUTORING_SESSION", "TUTOR_AVAILABILITY")
    foreach ($table in $mustExist) {
        if (-not (Has-Table $db $table)) {
            throw "Reset aborted: missing required table $table."
        }
    }

    $protected = @("STUDENT", "TUTOR", "ADMIN", "COURSE", "TUTOR_COURSE",
        "STUDENT_LOGIN", "TUTOR_LOGIN", "ADMIN_LOGIN")
    $originalCounts = @{}
    foreach ($table in $protected) {
        if (Has-Table $db $table) {
            $originalCounts[$table] = Get-Count $db $table
        }
    }

    $studentIds = @{}
    foreach ($row in (Read-Table $db "SELECT [StudentID] FROM [STUDENT]").Rows) {
        if (-not ($row["StudentID"] -is [System.DBNull])) {
            $studentIds[[string]$row["StudentID"]] = $true
        }
    }
    $courseIds = @{}
    foreach ($row in (Read-Table $db "SELECT [CourseID] FROM [COURSE]").Rows) {
        if (-not ($row["CourseID"] -is [System.DBNull])) {
            $courseIds[[string]$row["CourseID"]] = $true
        }
    }

    $valid = @(
        foreach ($row in (Read-Table $db "SELECT [RequestID],[StudentID],[CourseID],[PreferredDate],[PreferredStartTime],[PreferredEndTime] FROM [TUTORING_REQUEST]").Rows) {
            try {
                if (-not $studentIds.ContainsKey([string]$row["StudentID"]) -or
                    -not $courseIds.ContainsKey([string]$row["CourseID"])) { continue }
                $day = ([datetime]$row["PreferredDate"]).Date
                $start = Get-Time $row["PreferredStartTime"]
                $end = Get-Time $row["PreferredEndTime"]
                if ($end -le $start -or $day.Add($start) -le [datetime]::Now) { continue }
                [pscustomobject]@{
                    RequestID = [int]$row["RequestID"]
                    SessionStart = $day.Add($start)
                }
            }
            catch { continue }
        }
    )
    $retained = @($valid | Sort-Object SessionStart -Descending |
        Select-Object -First 2)
    if ($retained.Count -lt 2) {
        throw "Reset aborted: fewer than two future requests have valid students, courses and times."
    }

    Write-Host "Database: $DatabasePath"
    Write-Host "Requests to preserve: $($retained[0].RequestID), $($retained[1].RequestID)"
    Write-Host "Existing protected records:"
    foreach ($table in ($originalCounts.Keys | Sort-Object)) {
        Write-Host "  $table = $($originalCounts[$table])"
    }

    if (-not $Apply) {
        Write-Host "PREVIEW ONLY. No data was modified. Run with -Apply to back up and reset operational records."
        return
    }

    $backup = "$DatabasePath.backup-$(Get-Date -Format yyyyMMdd-HHmmss)"
    Copy-Item -LiteralPath $DatabasePath -Destination $backup -ErrorAction Stop
    Write-Host "Backup created: $backup"

    $transaction = $db.BeginTransaction()
    try {
        [void](Execute-Statement $db $transaction "DELETE FROM [TUTORING_SESSION]")
        [void](Execute-Statement $db $transaction "DELETE FROM [TUTOR_AVAILABILITY]")
        [void](Execute-Statement $db $transaction "DELETE FROM [TUTORING_REQUEST] WHERE [RequestID] NOT IN (?,?)" @($retained[0].RequestID, $retained[1].RequestID))
        [void](Execute-Statement $db $transaction "UPDATE [TUTORING_REQUEST] SET [Status]='Submitted' WHERE [RequestID] IN (?,?)" @($retained[0].RequestID, $retained[1].RequestID))
        foreach ($table in @("STUDENT_TERM_GPA", "STUDENTACADEMICRECORD")) {
            if (Has-Table $db $table) {
                [void](Execute-Statement $db $transaction "DELETE FROM [$table]")
            }
        }

        if ((Get-Count $db "TUTORING_REQUEST" $transaction) -ne 2) {
            throw "Reset verification failed: expected exactly two preserved requests."
        }
        foreach ($table in $originalCounts.Keys) {
            if ((Get-Count $db $table $transaction) -ne $originalCounts[$table]) {
                throw "Protected table changed: $table."
            }
        }
        $transaction.Commit()
        Write-Host "Operational reset committed. All protected table counts were preserved."
        Write-Host "Two submitted tutoring requests remain. Tutors must now enter real availability before matching."
    }
    catch {
        try { $transaction.Rollback() } catch {}
        throw
    }
    finally { $transaction.Dispose() }
}
finally {
    $db.Dispose()
}
