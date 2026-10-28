param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [int]$KeepStudents = 5
)

$ErrorActionPreference = "Stop"
if ($KeepStudents -lt 1) { throw "KeepStudents must be at least 1." }
$dbPath = Join-Path $ProjectRoot "Edu_Connect.accdb"
if (-not (Test-Path -LiteralPath $dbPath)) { throw "The project database was not found: $dbPath" }
$lockPath = [System.IO.Path]::ChangeExtension($dbPath, ".laccdb")
if (Test-Path -LiteralPath $lockPath) {
    throw "Close Access, Visual Studio's running app, and other database users first. Lock file: $lockPath"
}

$backupFolder = Join-Path $ProjectRoot "DatabaseBackups"
[System.IO.Directory]::CreateDirectory($backupFolder) | Out-Null
$backup = Join-Path $backupFolder ("Edu_Connect_before_clean_start_" + (Get-Date -Format "yyyyMMdd_HHmmss") + ".accdb")
Copy-Item -LiteralPath $dbPath -Destination $backup -ErrorAction Stop
Write-Host ("Database backup created: " + $backup)

Add-Type -AssemblyName System.Data

function Execute-Command($connection, $transaction, [string]$sql, [object[]]$arguments = @()) {
    $cmd = New-Object System.Data.OleDb.OleDbCommand($sql, $connection)
    $cmd.Transaction = $transaction
    try {
        foreach ($argument in $arguments) {
            $type = [System.Data.OleDb.OleDbType]::VarWChar
            if ($argument -is [DateTime]) { $type = [System.Data.OleDb.OleDbType]::Date }
            elseif ($argument -is [bool]) { $type = [System.Data.OleDb.OleDbType]::Boolean }
            elseif ($argument -is [int]) { $type = [System.Data.OleDb.OleDbType]::Integer }
            $p = $cmd.Parameters.Add("?", $type)
            if ($null -eq $argument) { $p.Value = [DBNull]::Value }
            else { $p.Value = $argument }
        }
        return $cmd.ExecuteNonQuery()
    }
    finally { $cmd.Dispose() }
}

function Read-Table($connection, $transaction, [string]$sql) {
    $cmd = New-Object System.Data.OleDb.OleDbCommand($sql, $connection)
    $cmd.Transaction = $transaction
    $table = New-Object System.Data.DataTable
    $adapter = New-Object System.Data.OleDb.OleDbDataAdapter($cmd)
    try { [void]$adapter.Fill($table); return ,$table }
    finally { $adapter.Dispose(); $cmd.Dispose() }
}

function Has-Table($connection, $transaction, [string]$name) {
    try {
        $t = Read-Table $connection $transaction ("SELECT * FROM [" + $name + "] WHERE 1=0")
        return $true
    }
    catch { return $false }
}

$connection = New-Object System.Data.OleDb.OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$dbPath;Persist Security Info=False;")
try { $connection.Open() }
catch {
    throw ("Access could not open the database. Install the Access Database Engine matching PowerShell's bitness, and close Access first. " + $_.Exception.Message)
}

try {
    $tx = $connection.BeginTransaction()
    try {
        foreach ($required in @("STUDENT","TUTOR","ADMIN","TUTORING_REQUEST","TUTORING_SESSION","TUTOR_AVAILABILITY","TUTOR_COURSE")) {
            if (-not (Has-Table $connection $tx $required)) { throw "Required table $required is missing. Nothing has been committed." }
        }

        $students = Read-Table $connection $tx "SELECT [StudentID],[StudentNo] FROM [STUDENT] ORDER BY [StudentID]"
        if ($students.Rows.Count -lt $KeepStudents) {
            throw ("There are only " + $students.Rows.Count + " students; cannot keep " + $KeepStudents + ".")
        }
        $keep = @($students.Rows | Select-Object -First $KeepStudents)
        $drop = @($students.Rows | Select-Object -Skip $KeepStudents)

        [void](Execute-Command $connection $tx "DELETE FROM [TUTORING_SESSION]")
        [void](Execute-Command $connection $tx "DELETE FROM [TUTORING_REQUEST]")

        foreach ($optional in @("STUDENT_TERM_GPA","STUDENTACADEMICRECORD")) {
            if (Has-Table $connection $tx $optional) {
                [void](Execute-Command $connection $tx ("DELETE FROM [" + $optional + "]"))
            }
        }

        foreach ($student in $drop) {
            if (Has-Table $connection $tx "STUDENT_LOGIN") {
                [void](Execute-Command $connection $tx "DELETE FROM [STUDENT_LOGIN] WHERE [StudentNo]=?" @([string]$student["StudentNo"]))
            }
            [void](Execute-Command $connection $tx "DELETE FROM [STUDENT] WHERE [StudentID]=?" @([int]$student["StudentID"]))
        }

        [void](Execute-Command $connection $tx "DELETE FROM [TUTOR_AVAILABILITY]")
        $tutors = Read-Table $connection $tx "SELECT [TutorID] FROM [TUTOR] ORDER BY [TutorID]"
        $availabilitySchema = Read-Table $connection $tx "SELECT * FROM [TUTOR_AVAILABILITY] WHERE 1=0"
        $idName = @("AvailabilityID","TutorAvailabilityID") | Where-Object { $availabilitySchema.Columns.Contains($_) } | Select-Object -First 1
        $idIsAuto = $true
        if ($null -ne $idName) {
            $schemaCmd = New-Object System.Data.OleDb.OleDbCommand("SELECT * FROM [TUTOR_AVAILABILITY] WHERE 1=0", $connection)
            $schemaCmd.Transaction = $tx
            $schemaReader = $schemaCmd.ExecuteReader([System.Data.CommandBehavior]::SchemaOnly)
            try {
                $schema = $schemaReader.GetSchemaTable()
                foreach ($row in $schema.Rows) {
                    if ([string]$row["ColumnName"] -eq $idName) {
                        $idIsAuto = ($schema.Columns.Contains("IsAutoIncrement") -and $row["IsAutoIncrement"] -ne [DBNull]::Value -and [bool]$row["IsAutoIncrement"])
                        break
                    }
                }
            }
            finally { $schemaReader.Close(); $schemaCmd.Dispose() }
        }

        $baseDate = (Get-Date).Date.AddDays(1)
        $dates = @($baseDate, $baseDate.AddDays(7))
        $nextId = 1
        $created = 0
        foreach ($tutor in $tutors.Rows) {
            $tutorId = [int]$tutor["TutorID"]
            foreach ($day in $dates) {
                foreach ($range in @([pscustomobject]@{ Start=9; End=12 }, [pscustomobject]@{ Start=13; End=16 })) {
                    $start = $day.AddHours($range.Start)
                    $end = $day.AddHours($range.End)
                    if ($null -ne $idName -and -not $idIsAuto) {
                        $sql = "INSERT INTO [TUTOR_AVAILABILITY] ([$idName],[TutorID],[AvailableDate],[StartTime],[EndTime],[IsAvailable]) VALUES (?,?,?,?,?,?)"
                        $args = @($nextId, $tutorId, $day, $start, $end, $true)
                        $nextId++
                    }
                    else {
                        $sql = "INSERT INTO [TUTOR_AVAILABILITY] ([TutorID],[AvailableDate],[StartTime],[EndTime],[IsAvailable]) VALUES (?,?,?,?,?)"
                        $args = @($tutorId, $day, $start, $end, $true)
                    }
                    [void](Execute-Command $connection $tx $sql $args)
                    $created++
                }
            }
        }
        $tx.Commit()
        Write-Host ("CLEAN START COMPLETE. Students kept: " + $KeepStudents + ". Tutors retained: " + $tutors.Rows.Count + ". Availability slots created: " + $created + ". Requests: 0. Sessions: 0.")
        Write-Host "Administrators, tutor profiles, tutor accounts, and tutor course qualifications were preserved."
        Write-Host ("Available dates: " + $dates[0].ToString("dd MMM yyyy") + " and " + $dates[1].ToString("dd MMM yyyy"))
    }
    catch {
        try { $tx.Rollback() } catch {}
        throw
    }
    finally { $tx.Dispose() }

    if (Has-Table $connection $null "STUDENT_TERM_GPA") {
        $wide = Read-Table $connection $null "SELECT * FROM [STUDENT_TERM_GPA] WHERE 1=0"
        if ($wide.Columns.Contains("DataOrigin")) {
            try {
                [void](Execute-Command $connection $null "ALTER TABLE [STUDENT_TERM_GPA] DROP COLUMN [DataOrigin]")
                Write-Host "Removed obsolete DataOrigin column from STUDENT_TERM_GPA."
            }
            catch {
                Write-Warning ("DataOrigin could not be dropped automatically; the academic table is empty. " + $_.Exception.Message)
            }
        }
    }
    Write-Host ("Backup: " + $backup)
}
finally { $connection.Dispose() }
