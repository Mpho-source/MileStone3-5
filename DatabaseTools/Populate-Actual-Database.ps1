# Populate the actual EduConnect project database with clearly marked synthetic test records.
# This is for functional testing, NOT evidence of real attendance.
# Close Access, EduConnect and Visual Studio debugging before running.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dbPath = Join-Path $root 'Edu_Connect.accdb'
$lockPath = Join-Path $root 'Edu_Connect.laccdb'
$backupFolder = Join-Path $root 'DatabaseBackups'
$tag = 'EDUCONNECT SYNTHETIC TEST ATTENDANCE - NOT REAL'
$limit = 10

if (-not [System.IO.File]::Exists($dbPath)) {
    throw "The actual application database is missing: $dbPath"
}
if ([System.IO.File]::Exists($lockPath)) {
    throw "The actual database is open or locked. Close Access and EduConnect, then retry. No data was changed."
}

Add-Type -AssemblyName System.Data

$connection = [System.Data.OleDb.OleDbConnection]::new(
    "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$dbPath")
$transaction = $null
$backup = $null
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
                if ([string]::Equals(([string]$entry['ColumnName']).Trim(),
                     $column,[System.StringComparison]::OrdinalIgnoreCase)) {
                    return $true
                }
            }
        } finally { $reader.Close() }
    }
    finally { $cmd.Dispose() }
    return $false
}

function InsertTestRequest(
    [int]$requestId, [int]$studentId, [int]$courseId,
    [datetime]$day, [datetime]$start, [datetime]$end
) {
    $cmd = $connection.CreateCommand()
    try {
        $cmd.Transaction = $transaction
        $cmd.CommandText = 'INSERT INTO [TUTORING_REQUEST] ' +
            '([RequestID],[StudentID],[CourseID],[SupportType],[PreferredDate],' +
            '[PreferredStartTime],[PreferredEndTime],[RequestDate],[Status]) ' +
            'VALUES (?,?,?,?,?,?,?,?,?)'
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
        $cmd.CommandText = 'INSERT INTO [TUTORING_SESSION] ' +
            '([SessionID],[RequestID],[TutorID],[SessionDate],[StartTime],' +
            '[EndTime],[SessionStatus],[Feedback],[AttendanceStatus]) ' +
            'VALUES (?,?,?,?,?,?,?,?,?)'
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

try {
    $connection.Open()

    $check = $connection.CreateCommand()
    try {
        $check.CommandText = 'SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [Feedback]=?'
        AddValue $check ([System.Data.OleDb.OleDbType]::VarWChar) $tag
        if ([int]$check.ExecuteScalar() -gt 0) {
            throw 'Synthetic test attendance is already present in the actual database. No duplicate sessions were created.'
        }
    } finally { $check.Dispose() }

    $studentIds = [System.Collections.Generic.List[int]]::new()
    # Existing GPA rows are required for the four-term chart.
    $academicKey = if (SchemaHasColumn 'STUDENTACADEMICRECORD' 'StudentNo') {
        '[STUDENT].[StudentNo]=[STUDENTACADEMICRECORD].[StudentNo]'
    } elseif (SchemaHasColumn 'STUDENTACADEMICRECORD' 'StudentID') {
        '[STUDENT].[StudentID]=[STUDENTACADEMICRECORD].[StudentID]'
    } else {
        throw 'STUDENTACADEMICRECORD must contain StudentNo or StudentID.'
    }
    $cmd = $connection.CreateCommand()
    try {
        $cmd.CommandText = "SELECT DISTINCT [STUDENT].[StudentID] " +
            "FROM [STUDENT] INNER JOIN [STUDENTACADEMICRECORD] ON $academicKey " +
            "ORDER BY [STUDENT].[StudentID]"
        $reader = $cmd.ExecuteReader()
        try {
            while ($reader.Read() -and $studentIds.Count -lt $limit) {
                if (-not $reader.IsDBNull(0)) {
                    $studentIds.Add([int]$reader.GetValue(0))
                }
            }
        } finally { $reader.Close() }
    } finally { $cmd.Dispose() }

    if ($studentIds.Count -eq 0) {
        throw 'No students with academic records were found. The database was not changed.'
    }

    $pairs = [System.Collections.Generic.List[object]]::new()
    $cmd = $connection.CreateCommand()
    try {
        $cmd.CommandText = 'SELECT [TutorID],[CourseID] FROM [TUTOR_COURSE] ORDER BY [TutorID],[CourseID]'
        $reader = $cmd.ExecuteReader()
        try {
            while ($reader.Read()) {
                if (-not $reader.IsDBNull(0) -and -not $reader.IsDBNull(1)) {
                    $pairs.Add([pscustomobject]@{
                        Tutor = [int]$reader.GetValue(0)
                        Course = [int]$reader.GetValue(1)
                    })
                }
            }
        } finally { $reader.Close() }
    } finally { $cmd.Dispose() }

    if ($pairs.Count -eq 0) {
        throw 'No TUTOR_COURSE assignments exist. Assign a course to a tutor first.'
    }

    $nextRequest = (QueryInteger 'SELECT MAX([RequestID]) FROM [TUTORING_REQUEST]') + 1
    $nextSession = (QueryInteger 'SELECT MAX([SessionID]) FROM [TUTORING_SESSION]') + 1

    # Snapshot the original database before writing, outside the database file itself.
    if (-not [System.IO.Directory]::Exists($backupFolder)) {
        [System.IO.Directory]::CreateDirectory($backupFolder) | Out-Null
    }
    $backup = Join-Path $backupFolder (
        'Edu_Connect_before_attendance_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.accdb')
    # Release the ACE connection before copying, so the backup is a closed-file snapshot.
    $connection.Close()
    [System.IO.File]::Copy($dbPath, $backup, $false)
    $connection.Open()
    Write-Host "BACKUP CREATED: $backup" -ForegroundColor Cyan

    Write-Host "POPULATING THE ACTUAL DATABASE: $dbPath" -ForegroundColor Yellow
    Write-Host 'These will be clearly marked synthetic test records, NOT genuine tutoring attendance.' -ForegroundColor Yellow

    $transaction = $connection.BeginTransaction()
    $months = @(2,5,8,10)
    for ($i=0; $i -lt $studentIds.Count; $i++) {
        for ($term=1; $term -le 4; $term++) {
            for ($slot=0; $slot -lt 2; $slot++) {
                $day = [datetime]::new(2026,$months[$term-1],4 + 2*$i + $slot)
                if ($day -ge [datetime]::Today) {
                    throw 'A generated historical date would fall in the future; no records were committed.'
                }
                $start = $day.AddHours(9)
                $end = $day.AddHours(10)
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
        $verify.CommandText = 'SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [Feedback]=?'
        AddValue $verify ([System.Data.OleDb.OleDbType]::VarWChar) $tag
        $persisted = [int]$verify.ExecuteScalar()
    } finally { $verify.Dispose() }

    if ($persisted -ne $created) {
        Write-Warning "The write committed, but only $persisted marked records were found when re-read; expected $created. Please inspect TUTORING_SESSION."
    } else {
        Write-Host "VERIFIED: $persisted completed sessions were re-read from TUTORING_SESSION." -ForegroundColor Green
    }
    Write-Host "SUCCESS: inserted $created synthetic completed sessions into the REAL Edu_Connect.accdb." -ForegroundColor Green
    Write-Host "Students: $($studentIds.Count); tutor-course assignments: $($pairs.Count)." -ForegroundColor Green
    Write-Host 'Existing GPA, account passwords, and existing sessions were not modified.' -ForegroundColor Green
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
