param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) "Edu_Connect.accdb"),
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

$DatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
if (-not [System.IO.File]::Exists($DatabasePath)) {
    throw "The Access database was not found: $DatabasePath"
}

function Open-ProjectDatabase([string]$path) {
    foreach ($provider in @("Microsoft.ACE.OLEDB.12.0", "Microsoft.ACE.OLEDB.16.0")) {
        $db = [System.Data.OleDb.OleDbConnection]::new(
            "Provider=$provider;Data Source=$path;Persist Security Info=False;")
        try {
            $db.Open()
            return $db
        }
        catch {
            $db.Dispose()
        }
    }
    throw "Cannot open the Access database. Use PowerShell with the same bitness as the Access Database Engine."
}

function Read-Sessions([System.Data.OleDb.OleDbConnection]$db,
                       [System.Data.OleDb.OleDbTransaction]$tx = $null) {
    $table = [System.Data.DataTable]::new()
    $cmd = $db.CreateCommand()
    $cmd.CommandText = "SELECT [SessionID],[SessionDate],[StartTime],[EndTime] FROM [TUTORING_SESSION] ORDER BY [SessionID]"
    if ($null -ne $tx) { $cmd.Transaction = $tx }
    $adapter = [System.Data.OleDb.OleDbDataAdapter]::new($cmd)
    try { [void]$adapter.Fill($table) }
    finally { $adapter.Dispose(); $cmd.Dispose() }
    return ,$table
}

function Has-DatePart($value) {
    if ($null -eq $value -or $value -is [System.DBNull]) { return $false }
    return ([datetime]$value).Date -ne [datetime]::FromOADate(0).Date
}

function Just-Time($value) {
    if ($null -eq $value -or $value -is [System.DBNull]) {
        return [System.DBNull]::Value
    }
    return [datetime]::FromOADate(0).Add(([datetime]$value).TimeOfDay)
}

$db = Open-ProjectDatabase $DatabasePath
try {
    $sessions = Read-Sessions $db
    $changed = @($sessions.Rows | Where-Object {
        (Has-DatePart $_["StartTime"]) -or (Has-DatePart $_["EndTime"])
    })
    Write-Host "Database: $DatabasePath"
    Write-Host "Existing tutoring sessions: $($sessions.Rows.Count)"
    Write-Host "Sessions with dates in start/end time: $($changed.Count)"
    Write-Host "Session IDs, SessionDate, statuses, request IDs, tutor IDs and attendance will be preserved."

    if (-not $Apply) {
        Write-Host "PREVIEW ONLY - no data has been modified."
        Write-Host "Close Access and EduConnect, then rerun with -Apply to back up the database, normalise times and apply Short Time formatting."
        return
    }
}
finally { $db.Dispose() }

[System.Data.OleDb.OleDbConnection]::ReleaseObjectPool()

$backup = "$DatabasePath.session-time-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss-fff')"
try {
    $source = [System.IO.FileStream]::new(
        $DatabasePath, [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read, [System.IO.FileShare]::None)
    try {
        $target = [System.IO.FileStream]::new(
            $backup, [System.IO.FileMode]::CreateNew,
            [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
        try { $source.CopyTo($target) }
        finally { $target.Dispose() }
    }
    finally { $source.Dispose() }
}
catch [System.IO.IOException] {
    throw "Close Access, EduConnect and other database users before applying the repair. $($_.Exception.Message)"
}
Write-Host "Backup saved: $backup"

$db = Open-ProjectDatabase $DatabasePath
try {
    $tx = $db.BeginTransaction()
    try {
        $repaired = 0
        foreach ($row in $changed) {
            $cmd = [System.Data.OleDb.OleDbCommand]::new(
                "UPDATE [TUTORING_SESSION] SET [StartTime]=?, [EndTime]=? WHERE [SessionID]=?",
                $db, $tx)
            try {
                [void]$cmd.Parameters.Add("@Start", [System.Data.OleDb.OleDbType]::Date)
                $cmd.Parameters[0].Value = Just-Time $row["StartTime"]
                [void]$cmd.Parameters.Add("@End", [System.Data.OleDb.OleDbType]::Date)
                $cmd.Parameters[1].Value = Just-Time $row["EndTime"]
                [void]$cmd.Parameters.Add("@SessionID", [System.Data.OleDb.OleDbType]::Integer)
                $cmd.Parameters[2].Value = [int]$row["SessionID"]
                if ($cmd.ExecuteNonQuery() -ne 1) {
                    throw "Failed updating SessionID $($row["SessionID"])."
                }
                $repaired++
            }
            finally { $cmd.Dispose() }
        }

        $check = Read-Sessions $db $tx
        if ($check.Rows.Count -ne $sessions.Rows.Count) {
            throw "The number of sessions changed. Rolling back."
        }
        $index = @{}
        foreach ($row in $check.Rows) {
            $index[[string]$row["SessionID"]] = $row
        }
        foreach ($row in $sessions.Rows) {
            $key = [string]$row["SessionID"]
            if (-not $index.ContainsKey($key)) {
                throw "Session $key disappeared. Rolling back."
            }
            $other = $index[$key]
            if ([string]$row["SessionDate"] -ne [string]$other["SessionDate"]) {
                throw "Session date changed for $key. Rolling back."
            }
            foreach ($column in @("StartTime", "EndTime")) {
                if ($row[$column] -is [System.DBNull]) {
                    if ($other[$column] -isnot [System.DBNull]) {
                        throw "Missing time was modified for $key. Rolling back."
                    }
                }
                else {
                    if ($other[$column] -is [System.DBNull] -or
                        ([datetime]$row[$column]).TimeOfDay -ne ([datetime]$other[$column]).TimeOfDay -or
                        (Has-DatePart $other[$column])) {
                        throw "Clock time verification failed for session $key. Rolling back."
                    }
                }
            }
        }

        $tx.Commit()
        Write-Host "Repaired $repaired session row(s); session dates and clock times were preserved."
    }
    catch {
        try { $tx.Rollback() } catch { }
        throw
    }
    finally { $tx.Dispose() }
}
finally { $db.Dispose() }

[System.Data.OleDb.OleDbConnection]::ReleaseObjectPool()

$dao = $null
$daoDb = $null
try {
    $dao = New-Object -ComObject DAO.DBEngine.120 -ErrorAction Stop
    $daoDb = $dao.OpenDatabase($DatabasePath, $true, $false)
    $table = $daoDb.TableDefs.Item("TUTORING_SESSION")
    foreach ($column in @("StartTime", "EndTime")) {
        $field = $table.Fields.Item($column)
        try {
            $field.Properties.Item("Format").Value = "Short Time"
        }
        catch {
            $formatProperty = $field.CreateProperty("Format", 10, "Short Time")
            $field.Properties.Append($formatProperty)
        }
    }
    Write-Host "Access column display formats set: StartTime = Short Time, EndTime = Short Time."
}
catch {
    Write-Warning "Clock-time values were repaired but Access column display formats could not be changed automatically: $($_.Exception.Message)"
    Write-Warning "In Access Design View, set the Format property of StartTime and EndTime to Short Time, then save."
}
finally {
    if ($null -ne $daoDb) {
        try { $daoDb.Close() } catch { }
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($daoDb)
    }
    if ($null -ne $dao) {
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($dao)
    }
}
