param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) "Edu_Connect.accdb"),
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

$DatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
if (-not (Test-Path -LiteralPath $DatabasePath)) {
    throw "Cannot find the project database: $DatabasePath"
}

function Open-Database([string]$path) {
    foreach ($provider in @("Microsoft.ACE.OLEDB.12.0", "Microsoft.ACE.OLEDB.16.0")) {
        $db = [System.Data.OleDb.OleDbConnection]::new(
            "Provider=$provider;Data Source=$path;Persist Security Info=False;")
        try {
            $db.Open()
            return $db
        }
        catch { $db.Dispose() }
    }
    throw "Access Database Engine is unavailable for this PowerShell process."
}

function Load-Slots($db, $transaction = $null) {
    $table = [System.Data.DataTable]::new()
    $cmd = $db.CreateCommand()
    $cmd.CommandText = "SELECT [AvailabilityID],[TutorID],[AvailableDate],[StartTime],[EndTime],[IsAvailable] FROM [TUTOR_AVAILABILITY] ORDER BY [AvailabilityID]"
    if ($null -ne $transaction) { $cmd.Transaction = $transaction }
    $adapter = [System.Data.OleDb.OleDbDataAdapter]::new($cmd)
    try { [void]$adapter.Fill($table) }
    finally { $adapter.Dispose(); $cmd.Dispose() }
    return ,$table
}

function Need-Normalization($value) {
    if ($null -eq $value -or $value -is [System.DBNull]) { return $false }
    return ([datetime]$value).Date -ne [datetime]::FromOADate(0).Date
}

$db = Open-Database $DatabasePath
try {
    $slots = Load-Slots $db
    $affected = @($slots.Rows | Where-Object {
        (Need-Normalization $_["StartTime"]) -or
        (Need-Normalization $_["EndTime"])
    })
    Write-Host "Database: $DatabasePath"
    Write-Host "Availability records: $($slots.Rows.Count)"
    Write-Host "Records with dates embedded in time fields: $($affected.Count)"
    Write-Host "The separate AvailableDate and IsAvailable fields will not be changed."
    if (-not $Apply) {
        Write-Host "PREVIEW ONLY. Close Access and EduConnect; rerun with -Apply to back up and normalise time fields."
        return
    }
    if ($affected.Count -eq 0) {
        Write-Host "Time values already contain no extra date portion."
        return
    }
}
finally { $db.Dispose() }

[System.Data.OleDb.OleDbConnection]::ReleaseObjectPool()

$backupPath = "$DatabasePath.availability-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss-fff')"
try {
    $source = [System.IO.FileStream]::new($DatabasePath,
        [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::None)
    try {
        $destination = [System.IO.FileStream]::new($backupPath,
            [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write,
            [System.IO.FileShare]::None)
        try { $source.CopyTo($destination) }
        finally { $destination.Dispose() }
    }
    finally { $source.Dispose() }
}
catch [System.IO.IOException] {
    throw "Cannot safely back up the database while it is open. Close Microsoft Access and EduConnect. $($_.Exception.Message)"
}
Write-Host "Backup: $backupPath"

$db = Open-Database $DatabasePath
try {
    $tx = $db.BeginTransaction()
    try {
        foreach ($row in $affected) {
            $start = if ($row["StartTime"] -is [System.DBNull]) {
                [System.DBNull]::Value
            } else {
                [datetime]::FromOADate(0).Add(([datetime]$row["StartTime"]).TimeOfDay)
            }
            $end = if ($row["EndTime"] -is [System.DBNull]) {
                [System.DBNull]::Value
            } else {
                [datetime]::FromOADate(0).Add(([datetime]$row["EndTime"]).TimeOfDay)
            }
            $cmd = [System.Data.OleDb.OleDbCommand]::new(
                "UPDATE [TUTOR_AVAILABILITY] SET [StartTime]=?,[EndTime]=? WHERE [AvailabilityID]=?", $db, $tx)
            try {
                [void]$cmd.Parameters.Add("@Start", [System.Data.OleDb.OleDbType]::Date)
                $cmd.Parameters[0].Value = $start
                [void]$cmd.Parameters.Add("@End", [System.Data.OleDb.OleDbType]::Date)
                $cmd.Parameters[1].Value = $end
                [void]$cmd.Parameters.Add("@ID", [System.Data.OleDb.OleDbType]::Integer)
                $cmd.Parameters[2].Value = [int]$row["AvailabilityID"]
                if ($cmd.ExecuteNonQuery() -ne 1) {
                    throw "Could not update AvailabilityID $($row["AvailabilityID"])."
                }
            }
            finally { $cmd.Dispose() }
        }

        $reloaded = Load-Slots $db $tx
        if ($reloaded.Rows.Count -ne $slots.Rows.Count) {
            throw "Availability record count changed unexpectedly."
        }
        $byId = @{}
        foreach ($row in $reloaded.Rows) {
            $byId[[string]$row["AvailabilityID"]] = $row
        }
        foreach ($row in $slots.Rows) {
            $key = [string]$row["AvailabilityID"]
            if (-not $byId.ContainsKey($key)) {
                throw "Availability record $key is missing."
            }
            $after = $byId[$key]
            foreach ($field in @("TutorID", "AvailableDate", "IsAvailable")) {
                if ([string]$row[$field] -ne [string]$after[$field]) {
                    throw "Protected field $field changed for availability $key."
                }
            }
            foreach ($field in @("StartTime", "EndTime")) {
                $wasNull = $row[$field] -is [System.DBNull]
                $nowNull = $after[$field] -is [System.DBNull]
                if ($wasNull -ne $nowNull) {
                    throw "A time value became null for availability $key."
                }
                if (-not $wasNull) {
                    $oldTime = ([datetime]$row[$field]).TimeOfDay
                    $newTime = ([datetime]$after[$field]).TimeOfDay
                    if ($oldTime -ne $newTime -or (Need-Normalization $after[$field])) {
                        throw "Time validation failed for availability $key, field $field."
                    }
                }
            }
        }
        $tx.Commit()
        Write-Host "Updated $($affected.Count) availability row(s), preserving dates, tutors, availability flags and clock times."
        Write-Host "In Microsoft Access Design View, set StartTime and EndTime Format to Short Time for 10:00 display."
    }
    catch {
        try { $tx.Rollback() } catch { }
        throw
    }
    finally { $tx.Dispose() }
}
finally { $db.Dispose() }
