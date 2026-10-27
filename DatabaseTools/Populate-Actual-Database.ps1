# Populate the actual EduConnect project database with clearly marked synthetic test records.
# This is for functional testing, NOT evidence of real attendance.
# Close Access, EduConnect and Visual Studio debugging before running.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dbPath = Join-Path $root 'Edu_Connect.accdb'
$originalPath = $dbPath
$lockPath = Join-Path $root 'Edu_Connect.laccdb'
$backupFolder = Join-Path $root 'DatabaseBackups'
$tag = 'EDUCONNECT SYNTHETIC TEST ATTENDANCE - NOT REAL'
$limit = 15
$academicTag = 'EDUCONNECT SYNTHETIC TEST GPA - NOT REAL'

if (-not [System.IO.File]::Exists($dbPath)) {
    throw "The actual application database is missing: $dbPath"
}
if ([System.IO.File]::Exists($lockPath)) {
    throw "The actual database is open or locked. Close Access and EduConnect, then retry. No data was changed."
}

[System.IO.Directory]::CreateDirectory($backupFolder) | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss_fff'
$backup = Join-Path $backupFolder "Edu_Connect_before_academic_population_$stamp.accdb"
$working = Join-Path $backupFolder "Edu_Connect_academic_working_$stamp.accdb"
$preinstall = Join-Path $backupFolder "Edu_Connect_before_academic_install_$stamp.accdb"
[System.IO.File]::Copy($originalPath, $backup, $false)
[System.IO.File]::Copy($backup, $working, $false)
$dbPath = $working
Write-Host "ORIGINAL BACKED UP: $backup" -ForegroundColor Green
Write-Host 'All changes are prepared in a separate working database.' -ForegroundColor Cyan

Add-Type -AssemblyName System.Data

$connection = [System.Data.OleDb.OleDbConnection]::new(
    "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$dbPath")
$transaction = $null
$created = 0

function AddValue(
    [System.Data.OleDb.OleDbCommand]$command,
    [System.Data.OleDb.OleDbType]$type,
    [object]$value
) {
    $parameter = $command.Parameters.Add("@p$($command.Parameters.Count)", $type)
    $parameter.Value = $value
}

function QueryInteger([string]$sql) {
    $cmd = $connection.CreateCommand()
    try {
        $cmd.CommandText = $sql
        $value = $cmd.ExecuteScalar()
        if ($null -eq $value -or [System.DBNull]::Value -eq $value) { return 0 }
        return [int]$value
    }
    finally { $cmd.Dispose() }
}

function SchemaHasColumn([string]$table, [string]$column) {
    $cmd = $connection.CreateCommand()
    try {
        $cmd.CommandText = "SELECT * FROM [$table] WHERE 1=0"
        $reader = $cmd.ExecuteReader([System.Data.CommandBehavior]::SchemaOnly)
        try {
            foreach ($entry in $reader.GetSchemaTable().Rows) {
                if ([string]::Equals(([string]$entry['ColumnName']).TrimStart([char]0xFEFF),
                     $column,[System.StringComparison]::OrdinalIgnoreCase)) {
                    return $true
                }
            }
        } finally { $reader.Close() }
    }
    finally { $cmd.Dispose() }
    return $false
}

$schemaCache = @{}

function ActualName([string]$table, [string]$logical) {
    if (-not $schemaCache.ContainsKey($table)) {
        $map = @{}
        $cmd = $connection.CreateCommand()
        try {
            if ($null -ne $transaction) { $cmd.Transaction = $transaction }
            $cmd.CommandText = "SELECT * FROM [$table] WHERE 1=0"
            $reader = $cmd.ExecuteReader()
            try {
                for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                    $actual = $reader.GetName($i)
                    $normalized = $actual.TrimStart(
                        [char[]]@([char]0xFEFF, [char]0x200B, [char]0x2060, [char]' '))
                    $map[$normalized] = '[' + $actual.Replace(']', ']]') + ']'
                }
            }
            finally { $reader.Close() }
        }
        finally { $cmd.Dispose() }
        $schemaCache[$table] = $map
    }
    if (-not $schemaCache[$table].ContainsKey($logical)) {
        throw "Column $table.$logical is missing. Original database untouched."
    }
    return $schemaCache[$table][$logical]
}

function InsertColumns([string]$table, [string[]]$names) {
    $columns = @()
    foreach ($name in $names) {
        $columns += (ActualName $table $name)
    }
    return ($columns -join ',')
}

function ReadOrdinal([System.Data.OleDb.OleDbDataReader]$reader, [string]$requested) {
    for ($i=0; $i -lt $reader.FieldCount; $i++) {
        $actual = $reader.GetName($i)
        $clean = $actual.TrimStart(
            [char[]]@([char]0xFEFF, [char]0x200B, [char]0x2060, [char]' '))
        if ([string]::Equals($clean,$requested,
            [System.StringComparison]::OrdinalIgnoreCase)) {
            return $i
        }
    }
    throw "Could not find required field $requested in the Access query."
}

function InsertTestRequest(
    [int]$requestId, [int]$studentId, [string]$courseId,
    [datetime]$day, [datetime]$start, [datetime]$end
) {
    $cmd = $connection.CreateCommand()
    try {
        $cmd.Transaction = $transaction
        $cols = InsertColumns 'TUTORING_REQUEST' @(
            'RequestID','StudentID','CourseID','SupportType','PreferredDate',
            'PreferredStartTime','PreferredEndTime','RequestDate','Status')
        $cmd.CommandText = "INSERT INTO [TUTORING_REQUEST] ($cols) VALUES (?,?,?,?,?,?,?,?,?)"
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Integer) $requestId
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Integer) $studentId
        AddValue $cmd ([System.Data.OleDb.OleDbType]::VarWChar) ([string]$courseId)
        AddValue $cmd ([System.Data.OleDb.OleDbType]::VarWChar) 'Individual tutoring'
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Date) $day
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Date) $start
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Date) $end
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Date) ($day.AddDays(-14))
        AddValue $cmd ([System.Data.OleDb.OleDbType]::VarWChar) 'Scheduled'
        if ($cmd.ExecuteNonQuery() -ne 1) {
            throw "Could not create test request $requestId."
        }
    }
    finally { $cmd.Dispose() }
}

function InsertTestSession(
    [int]$sessionId, [int]$requestId, [int]$tutorId,
    [datetime]$day, [datetime]$start, [datetime]$end,
    [string]$attendance
) {
    $cmd = $connection.CreateCommand()
    try {
        $cmd.Transaction = $transaction
        $cols = InsertColumns 'TUTORING_SESSION' @(
            'SessionID','RequestID','TutorID','SessionDate','StartTime',
            'EndTime','SessionStatus','Feedback','AttendanceStatus')
        $cmd.CommandText = "INSERT INTO [TUTORING_SESSION] ($cols) VALUES (?,?,?,?,?,?,?,?,?)"
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Integer) $sessionId
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Integer) $requestId
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Integer) $tutorId
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Date) $day
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Date) $start
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Date) $end
        AddValue $cmd ([System.Data.OleDb.OleDbType]::VarWChar) 'Completed'
        AddValue $cmd ([System.Data.OleDb.OleDbType]::VarWChar) $tag
        AddValue $cmd ([System.Data.OleDb.OleDbType]::VarWChar) $attendance
        if ($cmd.ExecuteNonQuery() -ne 1) {
            throw "Could not create test session $sessionId."
        }
    }
    finally { $cmd.Dispose() }
}

function EnsureTable([string]$table, [string]$sql) {
    $exists = $false
    foreach ($r in $connection.GetSchema('Tables').Rows) {
        if ([string]::Equals([string]$r['TABLE_NAME'],$table,
            [System.StringComparison]::OrdinalIgnoreCase)) {
            $exists = $true
            break
        }
    }
    if (-not $exists) {
        $cmd = $connection.CreateCommand()
        try {
            $cmd.CommandText = $sql
            [void]$cmd.ExecuteNonQuery()
            Write-Host "Created missing table: $table"
        }
        finally { $cmd.Dispose() }
    }
}

function EnsureColumn([string]$table, [string]$column, [string]$type) {
    if (-not (SchemaHasColumn $table $column)) {
        $cmd = $connection.CreateCommand()
        try {
            $cmd.CommandText = "ALTER TABLE [$table] ADD COLUMN [$column] $type"
            [void]$cmd.ExecuteNonQuery()
            Write-Host "Added missing column: $table.$column"
        }
        finally { $cmd.Dispose() }
    }
}

function InsertTestGpa([string]$number, [string]$term, [double]$gpa, [datetime]$date) {
    $cmd = $connection.CreateCommand()
    try {
        $cmd.Transaction = $transaction
        $cols = InsertColumns 'STUDENTACADEMICRECORD' @(
            'StudentNo','AcademicPeriod','GPA','RecordDate','RecordSource')
        $cmd.CommandText = "INSERT INTO [STUDENTACADEMICRECORD] ($cols) VALUES (?,?,?,?,?)"
        AddValue $cmd ([System.Data.OleDb.OleDbType]::VarWChar) $number
        AddValue $cmd ([System.Data.OleDb.OleDbType]::VarWChar) $term
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Double) $gpa
        AddValue $cmd ([System.Data.OleDb.OleDbType]::Date) $date
        AddValue $cmd ([System.Data.OleDb.OleDbType]::VarWChar) $academicTag
        if ($cmd.ExecuteNonQuery() -ne 1) { throw "Unable to add synthetic GPA for $number / $term." }
    }
    finally { $cmd.Dispose() }
}

try {
    $connection.Open()

    EnsureTable 'ADMIN_LOGIN' ('CREATE TABLE [ADMIN_LOGIN] ' +
        '([AdminNo] TEXT(50) PRIMARY KEY,[Salt] TEXT(64),[PasswordHash] TEXT(128),[Iterations] LONG)')
    EnsureTable 'STUDENT_LOGIN' ('CREATE TABLE [STUDENT_LOGIN] ' +
        '([StudentNo] TEXT(50) PRIMARY KEY,[Salt] TEXT(64),[PasswordHash] TEXT(128),[Iterations] LONG)')
    EnsureTable 'TUTOR_LOGIN' ('CREATE TABLE [TUTOR_LOGIN] ' +
        '([TutorID] LONG PRIMARY KEY,[Salt] TEXT(64),[PasswordHash] TEXT(128),[Iterations] LONG)')
    EnsureTable 'STUDENTACADEMICRECORD' ('CREATE TABLE [STUDENTACADEMICRECORD] ' +
        '([AcademicRecordID] COUNTER PRIMARY KEY,[StudentNo] TEXT(50),[GPA] DOUBLE,' +
        '[AcademicPeriod] TEXT(20),[RecordDate] DATETIME,[RecordSource] TEXT(100))')
    EnsureColumn 'STUDENTACADEMICRECORD' 'RecordSource' 'TEXT(100)'
    EnsureColumn 'TUTORING_SESSION' 'AttendanceStatus' 'TEXT(20)'
    EnsureColumn 'TUTORING_SESSION' 'Feedback' 'TEXT(255)'

    $check = $connection.CreateCommand()
    try {
        $feedbackName = ActualName 'TUTORING_SESSION' 'Feedback'
        $check.CommandText = "SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE $feedbackName=?"
        AddValue $check ([System.Data.OleDb.OleDbType]::VarWChar) $tag
        if ([int]$check.ExecuteScalar() -gt 0) {
            throw 'Synthetic test attendance is already present in the actual database. No duplicate sessions were created.'
        }
    } finally { $check.Dispose() }

    $studentIds = [System.Collections.Generic.List[int]]::new()
    $studentNumbers = [System.Collections.Generic.Dictionary[int,string]]::new()
    $cmd = $connection.CreateCommand()
    try {
        $cmd.CommandText = 'SELECT * FROM [STUDENT]'
        $reader = $cmd.ExecuteReader()
        try {
            $idOrdinal = ReadOrdinal $reader 'StudentID'
            $noOrdinal = ReadOrdinal $reader 'StudentNo'
            while ($reader.Read() -and $studentIds.Count -lt $limit) {
                if ($reader.IsDBNull($idOrdinal) -or $reader.IsDBNull($noOrdinal)) { continue }
                $studentId = [int]$reader.GetValue($idOrdinal)
                $studentNo = ([string]$reader.GetValue($noOrdinal)).Trim()
                if ($studentId -gt 0 -and $studentNo.Length -gt 0 -and
                    -not $studentNumbers.ContainsKey($studentId)) {
                    $studentIds.Add($studentId)
                    $studentNumbers.Add($studentId,$studentNo)
                }
            }
        }
        finally { $reader.Close() }
    }
    finally { $cmd.Dispose() }

    $existingTerms = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    $cmd = $connection.CreateCommand()
    try {
        $cmd.CommandText = 'SELECT * FROM [STUDENTACADEMICRECORD]'
        $reader = $cmd.ExecuteReader()
        try {
            $noOrdinal = ReadOrdinal $reader 'StudentNo'
            $termOrdinal = ReadOrdinal $reader 'AcademicPeriod'
            while ($reader.Read()) {
                if (-not $reader.IsDBNull($noOrdinal) -and -not $reader.IsDBNull($termOrdinal)) {
                    [void]$existingTerms.Add(
                        ([string]$reader.GetValue($noOrdinal)).Trim() + '|' +
                        ([string]$reader.GetValue($termOrdinal)).Trim())
                }
            }
        }
        finally { $reader.Close() }
    }
    finally { $cmd.Dispose() }

    if ($studentIds.Count -eq 0) {
        throw 'No students with StudentID and StudentNo were found. No source data was changed.'
    }

    $pairs = [System.Collections.Generic.List[object]]::new()
    $cmd = $connection.CreateCommand()
    try {
        $cmd.CommandText = 'SELECT * FROM [TUTOR_COURSE]'
        $reader = $cmd.ExecuteReader()
        try {
            $tutorOrdinal = ReadOrdinal $reader 'TutorID'
            $courseOrdinal = ReadOrdinal $reader 'CourseID'
            while ($reader.Read()) {
                if (-not $reader.IsDBNull($tutorOrdinal) -and -not $reader.IsDBNull($courseOrdinal)) {
                    $pairs.Add([pscustomobject]@{
                        Tutor = [int]$reader.GetValue($tutorOrdinal)
                        Course = ([string]$reader.GetValue($courseOrdinal)).Trim()
                    })
                }
            }
        } finally { $reader.Close() }
    } finally { $cmd.Dispose() }

    if ($pairs.Count -eq 0) {
        throw 'No TUTOR_COURSE assignments exist. Assign a course to a tutor first.'
    }

    $requestField = ActualName 'TUTORING_REQUEST' 'RequestID'
    $nextRequest = (QueryInteger "SELECT MAX($requestField) FROM [TUTORING_REQUEST]") + 1
    $sessionField = ActualName 'TUTORING_SESSION' 'SessionID'
    $nextSession = (QueryInteger "SELECT MAX($sessionField) FROM [TUTORING_SESSION]") + 1

    Write-Host "POPULATING WORKING COPY: $dbPath" -ForegroundColor Cyan
    Write-Host 'Only synthetic test GPA and attendance records will be added.' -ForegroundColor Yellow

    $createdGpa = 0
    $patterns = @(
        @(67,71,75,81),
        @(77,72,66,59),
        @(52,48,45,47),
        @(64,69,67,73)
    )
    $transaction = $connection.BeginTransaction()
    $months = @(2,5,8,10)
    for ($i=0; $i -lt $studentIds.Count; $i++) {
        for ($term=1; $term -le 4; $term++) {
            $number = $studentNumbers[$studentIds[$i]]
            $period = "2026-T$term"
            $key = $number + '|' + $period
            if (-not $existingTerms.Contains($key)) {
                $pattern = $patterns[$i % $patterns.Count]
                $gpa = [double]($pattern[$term - 1] + ($i % 3) - 1)
                $date = [datetime]::new(2026,$months[$term-1],24)
                InsertTestGpa $number $period $gpa $date
                [void]$existingTerms.Add($key)
                $createdGpa++
            }
            for ($slot=0; $slot -lt 2; $slot++) {
                $sequence = 2*$i + $slot
                $day = [datetime]::new(2026,$months[$term-1],4 + ($sequence % 20))
                if ($day -ge [datetime]::Today) {
                    throw 'A generated historical date would fall in the future; no records were committed.'
                }
                $start = $day.AddHours(9 + [math]::Floor($sequence / 20))
                $end = $start.AddHours(1)
                $pair = $pairs[($i + $term + $slot) % $pairs.Count]
                # This is an artificial attendance pattern for functional testing only.
                $attendance = if (($i + $term + $slot) % 5 -eq 0) {
                    'Absent'
                } else { 'Attended' }

                InsertTestRequest $nextRequest $studentIds[$i] $pair.Course $day $start $end
                InsertTestSession $nextSession $nextRequest $pair.Tutor $day $start $end $attendance
                $nextRequest++
                $nextSession++
                $created++
            }
        }
    }
    $transaction.Commit()
    $transaction = $null

    # Read the table again so the notification reflects saved rows, not just an attempt.
    $verify = $connection.CreateCommand()
    try {
        $verifyFeedbackName = ActualName 'TUTORING_SESSION' 'Feedback'
        $verify.CommandText = "SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE $verifyFeedbackName=?"
        AddValue $verify ([System.Data.OleDb.OleDbType]::VarWChar) $tag
        $persisted = [int]$verify.ExecuteScalar()
    } finally { $verify.Dispose() }

    $sourceField = ActualName 'STUDENTACADEMICRECORD' 'RecordSource'
    $verifyGpa = QueryInteger "SELECT COUNT(*) FROM [STUDENTACADEMICRECORD] WHERE $sourceField='$academicTag'"
    if ($verifyGpa -lt $createdGpa) {
        throw "Academic validation failed: only $verifyGpa synthetic rows were found."
    }
    if ($persisted -ne $created) {
        throw "Attendance validation failed: $persisted records found, expected $created."
    }
    $connection.Close()
    [System.IO.File]::Replace($working, $originalPath, $preinstall)
    Write-Host "PROJECT DATABASE UPDATED: $originalPath" -ForegroundColor Green
    Write-Host "VERIFIED: $verifyGpa synthetic GPA rows and $persisted synthetic sessions." -ForegroundColor Green
    Write-Host "SUCCESS: created $createdGpa academic rows and $created completed test sessions." -ForegroundColor Green
    Write-Host "Students: $($studentIds.Count); tutor-course assignments: $($pairs.Count)." -ForegroundColor Green
    Write-Host 'Existing GPA, account passwords, and existing sessions were not modified; added records are synthetic.' -ForegroundColor Green
    Write-Host "Backup path: $backup" -ForegroundColor Green
    Write-Host 'Open EduConnect > Reports > Report 3 > Generate report. Attendance will no longer be empty for those students.' -ForegroundColor Cyan
}
catch {
    if ($null -ne $transaction) {
        try { $transaction.Rollback() } catch { }
    }
    $stage = if ($null -ne $transaction) { "No synthetic records were committed." } else { "Check whether the transaction has already committed." }
    Write-Host ("FAILED: " + $_.Exception.Message + " " + $stage) -ForegroundColor Red
    throw
}
finally {
    if ($null -ne $connection) { $connection.Dispose() }
}
