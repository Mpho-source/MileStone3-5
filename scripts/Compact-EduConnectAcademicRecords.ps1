param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Edu_Connect.accdb'),
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$DatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
$staging = 'STUDENTACADEMICRECORD_WIDE_STAGING'
$archive = 'STUDENTACADEMICRECORD_HISTORY'
$primary = 'STUDENTACADEMICRECORD'

if (-not (Test-Path -LiteralPath $DatabasePath)) { throw "Database missing: $DatabasePath" }
if (Test-Path -LiteralPath ([System.IO.Path]::ChangeExtension($DatabasePath, '.laccdb'))) {
    throw 'Close Microsoft Access and EduConnect before changing academic records.'
}

function Open-Access {
    foreach ($provider in @('Microsoft.ACE.OLEDB.12.0', 'Microsoft.ACE.OLEDB.16.0')) {
        $connection = New-Object System.Data.OleDb.OleDbConnection("Provider=$provider;Data Source=$DatabasePath;Persist Security Info=False;")
        try { $connection.Open(); return $connection }
        catch { $connection.Dispose() }
    }
    throw 'Microsoft Access Database Engine could not open the database.'
}

function Read-Data($connection, [string]$sql) {
    $table = New-Object System.Data.DataTable
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $adapter = New-Object System.Data.OleDb.OleDbDataAdapter($command)
    try { [void]$adapter.Fill($table) }
    finally { $adapter.Dispose(); $command.Dispose() }
    return ,$table
}

function Execute-Sql($connection, [string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    try { [void]$command.ExecuteNonQuery() }
    finally { $command.Dispose() }
}

function Release-Com($value) {
    if ($null -ne $value) {
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($value)
    }
}

$database = Open-Access
$grades = @{}
$existingCount = 0
try {
    $tables = @($database.GetSchema('Tables').Rows | ForEach-Object { [string]$_['TABLE_NAME'] })
    if ($tables -notcontains $primary) { throw "Missing required table: $primary" }
    $original = Read-Data $database "SELECT * FROM [$primary]"
    if ($original.Columns.Contains('AcademicYear') -and
        $original.Columns.Contains('T1') -and
        $original.Columns.Contains('T2') -and
        $original.Columns.Contains('T3') -and
        $original.Columns.Contains('T4')) {
        Write-Host "ALREADY COMPACT: $primary has T1, T2, T3 and T4 columns."
        Write-Host "Rows: $($original.Rows.Count). No data modified."
        return
    }
    if ($tables -contains $archive -or $tables -contains $staging) {
        throw "An archive or staging table already exists. Review $archive and $staging before rerunning."
    }
    foreach ($column in @('StudentNo','GPA','AcademicPeriod','RecordSource')) {
        if (-not $original.Columns.Contains($column)) {
            throw "Missing academic column $column; the database has not been changed."
        }
    }
    $existingCount = $original.Rows.Count
    foreach ($record in $original.Rows) {
        $number = ([string]$record['StudentNo']).Trim()
        $period = ([string]$record['AcademicPeriod']).Trim()
        if ([string]::IsNullOrWhiteSpace($number) -or $period -notmatch '^([0-9]{4})-T([1-4])$') {
            throw "Invalid StudentNo or AcademicPeriod in original academic records. Nothing changed."
        }
        if ($record['GPA'] -is [DBNull]) { throw "GPA is missing for $number, $period. Nothing changed." }
        $value = [double]$record['GPA']
        if ([double]::IsNaN($value) -or [double]::IsInfinity($value) -or $value -lt 0 -or $value -gt 100) {
            throw "Invalid GPA for $number, $period. Nothing changed."
        }
        $year = [int]$Matches[1]
        $term = [int]$Matches[2]
        $key = $number.ToUpperInvariant() + '|' + $year
        if (-not $grades.ContainsKey($key)) {
            $grades[$key] = [pscustomobject]@{
                StudentNo = $number
                AcademicYear = $year
                T1 = [DBNull]::Value
                T2 = [DBNull]::Value
                T3 = [DBNull]::Value
                T4 = [DBNull]::Value
                RecordSource = ([string]$record['RecordSource']).Trim()
            }
        }
        $row = $grades[$key]
        $column = 'T' + $term
        if ($row.$column -isnot [DBNull]) { throw "Duplicate academic grade: $number, $period. Nothing changed." }
        $row.$column = $value
        $source = ([string]$record['RecordSource']).Trim()
        if ($source -match 'Illustrative|Synthetic|Sample' -and $row.RecordSource -notmatch 'Illustrative|Synthetic|Sample') {
            $row.RecordSource = $source
        }
    }
    if ($grades.Count -eq 0) { throw 'No academic records found. Nothing changed.' }
    Write-Host "Database: $DatabasePath"
    Write-Host "Original normalized table: $existingCount rows"
    Write-Host "New compact table: $($grades.Count) student/year rows"
    Write-Host 'Columns: AcademicRecordID, StudentNo, AcademicYear, T1, T2, T3, T4, RecordSource'
    $grades.Values | Sort-Object StudentNo,AcademicYear |
        Select-Object StudentNo,AcademicYear,T1,T2,T3,T4 | Format-Table -AutoSize | Out-Host
    if (-not $Apply) {
        Write-Host 'PREVIEW ONLY. Original table unchanged. Run with -Apply after closing Access.'
        return
    }
}
finally { $database.Dispose() }

$engine = $null
$catalog = $null
try {
    $engine = New-Object -ComObject DAO.DBEngine.120
    $catalog = $engine.OpenDatabase($DatabasePath)
    foreach ($relation in $catalog.Relations) {
        if ([string]$relation.Table -eq $primary -or [string]$relation.ForeignTable -eq $primary) {
            throw "Existing relationship $($relation.Name) uses $primary. Migration cancelled; use the backup and review the relationship."
        }
    }
}
finally {
    if ($null -ne $catalog) { $catalog.Close(); Release-Com $catalog }
    Release-Com $engine
}

$backup = "$DatabasePath.before-wide-gpa-$(Get-Date -Format 'yyyyMMdd-HHmmss-fff')"
Copy-Item -LiteralPath $DatabasePath -Destination $backup -ErrorAction Stop
Write-Host "Backup created: $backup"

$database = Open-Access
try {
    Execute-Sql $database "CREATE TABLE [$staging] ([AcademicRecordID] LONG NOT NULL, [StudentNo] TEXT(32) NOT NULL, [AcademicYear] INTEGER NOT NULL, [T1] DOUBLE, [T2] DOUBLE, [T3] DOUBLE, [T4] DOUBLE, [RecordSource] TEXT(100))"
    $tx = $database.BeginTransaction()
    try {
        $id = 0
        foreach ($row in ($grades.Values | Sort-Object StudentNo,AcademicYear)) {
            $id++
            $cmd = $database.CreateCommand()
            $cmd.Transaction = $tx
            $cmd.CommandText = "INSERT INTO [$staging] ([AcademicRecordID],[StudentNo],[AcademicYear],[T1],[T2],[T3],[T4],[RecordSource]) VALUES (?,?,?,?,?,?,?,?)"
            $values = @($id,$row.StudentNo,$row.AcademicYear,$row.T1,$row.T2,$row.T3,$row.T4,$row.RecordSource)
            foreach ($value in $values) {
                $param = $cmd.CreateParameter()
                $param.Value = if ($null -eq $value) { [DBNull]::Value } else { $value }
                [void]$cmd.Parameters.Add($param)
            }
            try {
                if ($cmd.ExecuteNonQuery() -ne 1) { throw "Failed to save $($row.StudentNo)" }
            }
            finally { $cmd.Dispose() }
        }
        $cmd = $database.CreateCommand()
        $cmd.Transaction = $tx
        $cmd.CommandText = "SELECT COUNT(*) FROM [$staging]"
        try { $count = [int]$cmd.ExecuteScalar() }
        finally { $cmd.Dispose() }
        if ($count -ne $grades.Count) { throw "Staging verification failed: $count rows instead of $($grades.Count)." }
        $tx.Commit()
    }
    catch {
        try { $tx.Rollback() } catch {}
        throw
    }
    finally { $tx.Dispose() }
    Execute-Sql $database "CREATE UNIQUE INDEX [IX_StudentYear] ON [$staging] ([StudentNo],[AcademicYear])"
}
finally { $database.Dispose() }

$engine = $null
$catalog = $null
$renamedOriginal = $false
try {
    $engine = New-Object -ComObject DAO.DBEngine.120
    $catalog = $engine.OpenDatabase($DatabasePath)
    $catalog.TableDefs.Item($primary).Name = $archive
    $renamedOriginal = $true
    $catalog.TableDefs.Item($staging).Name = $primary
    foreach ($row in $grades.Values) {
        $queryName = 'Student_GPA_Terms_' + $row.AcademicYear
        foreach ($query in $catalog.QueryDefs) {
            if ([string]$query.Name -eq $queryName) {
                $query.SQL = "SELECT [StudentNo],[T1],[T2],[T3],[T4] FROM [$primary] WHERE [AcademicYear]=$($row.AcademicYear) ORDER BY [StudentNo]"
                break
            }
        }
    }
}
catch {
    if ($renamedOriginal) {
        try {
            $names = @($catalog.TableDefs | ForEach-Object { [string]$_.Name })
            if ($names -notcontains $primary) { $catalog.TableDefs.Item($archive).Name = $primary }
        } catch {}
    }
    throw "Could not complete table swap. Database backup is $backup. Error: $($_.Exception.Message)"
}
finally {
    if ($null -ne $catalog) { $catalog.Close(); Release-Com $catalog }
    Release-Com $engine
}

$database = Open-Access
try {
    $compact = Read-Data $database "SELECT [StudentNo],[AcademicYear],[T1],[T2],[T3],[T4] FROM [$primary]"
    $historical = Read-Data $database "SELECT [StudentNo],[GPA],[AcademicPeriod] FROM [$archive]"
    if ($compact.Rows.Count -ne $grades.Count -or $historical.Rows.Count -ne $existingCount) {
        throw "Final verification failed. Use the backup file: $backup"
    }
    Write-Host "SUCCESS: $primary now has $($compact.Rows.Count) student/year rows with T1-T4 columns."
    Write-Host "Original $existingCount rows preserved in $archive."
    Write-Host "Backup: $backup"
}
finally { $database.Dispose() }
