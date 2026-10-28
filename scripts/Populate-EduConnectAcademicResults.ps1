param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) "Edu_Connect.accdb"),
    [int]$AcademicYear = 2026,
    [switch]$Apply
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

$DatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
if (-not (Test-Path -LiteralPath $DatabasePath)) {
    throw "Database not found: $DatabasePath"
}
if ($AcademicYear -lt 2000 -or $AcademicYear -gt 2100) {
    throw "Choose a valid academic year."
}

function Open-Access([string]$path) {
    $errors = @()
    foreach ($provider in @("Microsoft.ACE.OLEDB.12.0", "Microsoft.ACE.OLEDB.16.0")) {
        $db = New-Object System.Data.OleDb.OleDbConnection(
            "Provider=$provider;Data Source=$path;Persist Security Info=False;")
        try {
            $db.Open()
            return $db
        }
        catch {
            $errors += $_.Exception.Message
            $db.Dispose()
        }
    }
    throw "Unable to open the Access database. Check that the Access Database Engine matches the PowerShell architecture. $($errors -join ' | ')"
}

function Read-Data([System.Data.OleDb.OleDbConnection]$db, [string]$sql) {
    $table = New-Object System.Data.DataTable
    $command = $db.CreateCommand()
    $command.CommandText = $sql
    $adapter = New-Object System.Data.OleDb.OleDbDataAdapter($command)
    try { [void]$adapter.Fill($table) }
    finally { $adapter.Dispose(); $command.Dispose() }
    return ,$table
}

function Test-Column($table, [string]$name) {
    return $table.Columns.Contains($name)
}

$db = Open-Access $DatabasePath
try {
    $students = Read-Data $db "SELECT [StudentNo] FROM [STUDENT] ORDER BY [StudentNo]"
    $academic = Read-Data $db "SELECT * FROM [STUDENTACADEMICRECORD]"

    foreach ($required in @("StudentNo", "GPA", "AcademicPeriod", "RecordDate", "RecordSource")) {
        if (-not (Test-Column $academic $required)) {
            throw "Academic results were not changed. STUDENTACADEMICRECORD is missing '$required'."
        }
    }

    $sourceValue = "Illustrative records"
    $existing = New-Object "System.Collections.Generic.HashSet[string]" (
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($row in $academic.Rows) {
        $number = [string]$row["StudentNo"]
        $period = [string]$row["AcademicPeriod"]
        if (-not [string]::IsNullOrWhiteSpace($number) -and
            -not [string]::IsNullOrWhiteSpace($period)) {
            [void]$existing.Add("$($number.Trim())|$($period.Trim())")
        }
    }

    $planned = New-Object System.Collections.Generic.List[object]
    $seenStudents = New-Object "System.Collections.Generic.HashSet[string]" (
        [System.StringComparer]::OrdinalIgnoreCase)
    $studentIndex = 0
    foreach ($row in $students.Rows) {
        if ($row["StudentNo"] -is [System.DBNull]) { continue }
        $number = ([string]$row["StudentNo"]).Trim()
        if ([string]::IsNullOrWhiteSpace($number) -or
            -not $seenStudents.Add($number)) { continue }

        $base = 46 + (($studentIndex * 13) % 30)
        for ($term = 1; $term -le 4; $term++) {
            $period = "$AcademicYear-T$term"
            if ($existing.Contains("$number|$period")) { continue }

            $progress = switch ($studentIndex % 4) {
                0 { ($term - 1) * 5 }
                1 { -($term - 1) * 3 }
                2 { @(0, 4, 1, 7)[$term - 1] }
                3 { @(0, -2, 3, 1)[$term - 1] }
            }
            $grade = [math]::Max(35, [math]::Min(92, $base + $progress))
            $month = @(2, 5, 8, 10)[$term - 1]
            $recordDate = Get-Date -Year $AcademicYear -Month $month -Day 24
            $planned.Add([pscustomobject]@{
                StudentNo = $number
                AcademicPeriod = $period
                GPA = [double]$grade
                RecordDate = [datetime]$recordDate
            })
        }
        $studentIndex++
    }

    if ($studentIndex -eq 0) {
        throw "No student numbers were found. No academic results were changed."
    }

    Write-Host "Database: $DatabasePath"
    Write-Host "Academic year: $AcademicYear"
    Write-Host "Existing student accounts: $studentIndex"
    Write-Host "Existing academic records: $($academic.Rows.Count)"
    Write-Host "New illustrative records planned: $($planned.Count)"
    Write-Host "Sample planned records:"
    $planned | Select-Object -First 8 StudentNo, AcademicPeriod, GPA |
        Format-Table -AutoSize | Out-Host

    if (-not $Apply) {
        Write-Host "PREVIEW ONLY: nothing has been changed."
        Write-Host "To insert clearly identified sample grades, close Access and EduConnect and rerun with -Apply."
        return
    }
    if ($planned.Count -eq 0) {
        Write-Host "Nothing to insert. Existing academic grades have not been overwritten."
        return
    }
}
finally {
    $db.Dispose()
}

[System.Data.OleDb.OleDbConnection]::ReleaseObjectPool()

$backupPath = "$DatabasePath.academic-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss-fff')"
if (Test-Path -LiteralPath ([System.IO.Path]::ChangeExtension($DatabasePath, ".laccdb"))) {
    throw "Close Microsoft Access and all running EduConnect instances before applying academic results."
}
Copy-Item -LiteralPath $DatabasePath -Destination $backupPath -ErrorAction Stop
Write-Host "Backup created: $backupPath"

$db = Open-Access $DatabasePath
try {
    $schemaCmd = $db.CreateCommand()
    $schemaCmd.CommandText = "SELECT * FROM [STUDENTACADEMICRECORD] WHERE 1=0"
    $schemaReader = $schemaCmd.ExecuteReader([System.Data.CommandBehavior]::SchemaOnly)
    $isAutoId = $true
    try {
        $schema = $schemaReader.GetSchemaTable()
        foreach ($field in $schema.Rows) {
            if ([string]::Equals([string]$field["ColumnName"], "AcademicRecordID",
                [System.StringComparison]::OrdinalIgnoreCase)) {
                $isAutoId = ($field["IsAutoIncrement"] -ne [DBNull]::Value -and
                    [bool]$field["IsAutoIncrement"])
                break
            }
        }
    }
    finally { $schemaReader.Dispose(); $schemaCmd.Dispose() }

    $nextId = 0
    if (-not $isAutoId) {
        $idCmd = $db.CreateCommand()
        $idCmd.CommandText = "SELECT MAX([AcademicRecordID]) FROM [STUDENTACADEMICRECORD]"
        try {
            $max = $idCmd.ExecuteScalar()
            if ($null -ne $max -and $max -ne [DBNull]::Value) {
                $nextId = [int]$max
            }
        }
        finally { $idCmd.Dispose() }
    }

    $tx = $db.BeginTransaction()
    try {
        $inserted = 0
        foreach ($record in $planned) {
            $sql = if ($isAutoId) {
                "INSERT INTO [STUDENTACADEMICRECORD] ([StudentNo],[GPA],[AcademicPeriod],[RecordDate],[RecordSource]) VALUES (?,?,?,?,?)"
            }
            else {
                "INSERT INTO [STUDENTACADEMICRECORD] ([AcademicRecordID],[StudentNo],[GPA],[AcademicPeriod],[RecordDate],[RecordSource]) VALUES (?,?,?,?,?,?)"
            }
            $cmd = New-Object System.Data.OleDb.OleDbCommand($sql, $db, $tx)
            try {
                if (-not $isAutoId) {
                    $nextId++
                    [void]$cmd.Parameters.Add("@ID", [System.Data.OleDb.OleDbType]::Integer)
                    $cmd.Parameters[$cmd.Parameters.Count - 1].Value = $nextId
                }
                [void]$cmd.Parameters.Add("@StudentNo", [System.Data.OleDb.OleDbType]::VarWChar)
                $cmd.Parameters[$cmd.Parameters.Count - 1].Value = $record.StudentNo
                [void]$cmd.Parameters.Add("@GPA", [System.Data.OleDb.OleDbType]::Double)
                $cmd.Parameters[$cmd.Parameters.Count - 1].Value = $record.GPA
                [void]$cmd.Parameters.Add("@AcademicPeriod", [System.Data.OleDb.OleDbType]::VarWChar)
                $cmd.Parameters[$cmd.Parameters.Count - 1].Value = $record.AcademicPeriod
                [void]$cmd.Parameters.Add("@RecordDate", [System.Data.OleDb.OleDbType]::Date)
                $cmd.Parameters[$cmd.Parameters.Count - 1].Value = $record.RecordDate
                [void]$cmd.Parameters.Add("@RecordSource", [System.Data.OleDb.OleDbType]::VarWChar)
                $cmd.Parameters[$cmd.Parameters.Count - 1].Value = $sourceValue
                if ($cmd.ExecuteNonQuery() -ne 1) {
                    throw "Unable to insert academic result for $($record.StudentNo), $($record.AcademicPeriod)."
                }
                $inserted++
            }
            finally { $cmd.Dispose() }
        }

        $verify = New-Object System.Data.OleDb.OleDbCommand(
            "SELECT COUNT(*) FROM [STUDENTACADEMICRECORD] WHERE [RecordSource]=?", $db, $tx)
        try {
            [void]$verify.Parameters.Add("@Source", [System.Data.OleDb.OleDbType]::VarWChar)
            $verify.Parameters[0].Value = $sourceValue
            $count = [int]$verify.ExecuteScalar()
            if ($count -lt $inserted) {
                throw "Academic insert verification failed."
            }
        }
        finally { $verify.Dispose() }

        $tx.Commit()
        Write-Host "Saved $inserted illustrative academic results. All Student, Tutor and Admin records remain unchanged."
        Write-Host "Report 3 can now display academic averages for $AcademicYear. Tutoring attendance still requires completed sessions."
    }
    catch {
        try { $tx.Rollback() } catch { }
        throw
    }
    finally { $tx.Dispose() }
}
finally {
    $db.Dispose()
}
