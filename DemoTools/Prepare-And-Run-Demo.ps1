# EduConnect demonstration attendance generator (Windows PowerShell 5.1)
# Only writes to DemoData\Edu_Connect.accdb, never to the original database.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $root 'Edu_Connect.accdb'
$demoFolder = Join-Path $root 'DemoData'
$demo = Join-Path $demoFolder 'Edu_Connect.accdb'
$marker = Join-Path $demoFolder 'SYNTHETIC_DEMO_RECORDS.txt'
$exe = Join-Path $root 'bin\Debug\NextGen_#7.exe'

if (-not (Test-Path -LiteralPath $source)) {
    throw "Main database missing: $source"
}
if (Test-Path -LiteralPath (Join-Path $root 'Edu_Connect.laccdb')) {
    throw 'Close Microsoft Access and EduConnect before creating the demo database.'
}

if (-not (Test-Path -LiteralPath $marker)) {
    if (Test-Path -LiteralPath $demo) {
        throw "A DemoData database already exists without this generator's marker. Nothing was overwritten. Back it up or rename it before retrying."
    }

    New-Item -Path $demoFolder -ItemType Directory -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $demo -ErrorAction Stop
    $db = [System.Data.OleDb.OleDbConnection]::new(
        "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$demo")
    $tx = $null

    try {
        $db.Open()
        function Invoke-DemoScalar([string]$sql) {
            $command = $db.CreateCommand()
            try {
                $command.CommandText = $sql
                $value = $command.ExecuteScalar()
                if ($null -eq $value -or $value -is [System.DBNull]) { return 0 }
                return [int]$value
            }
            finally { $command.Dispose() }
        }

        $nextRequest = (Invoke-DemoScalar 'SELECT MAX([RequestID]) FROM [TUTORING_REQUEST]') + 1
        $nextSession = (Invoke-DemoScalar 'SELECT MAX([SessionID]) FROM [TUTORING_SESSION]') + 1

        $studentIds = New-Object 'System.Collections.Generic.List[int]'
        $command = $db.CreateCommand()
        $command.CommandText = 'SELECT TOP 10 [StudentID] FROM [STUDENT] ORDER BY [StudentID]'
        $reader = $command.ExecuteReader()
        try {
            while ($reader.Read()) {
                if (-not $reader.IsDBNull(0)) {
                    $studentIds.Add([int]$reader.GetValue(0))
                }
            }
        }
        finally { $reader.Close(); $command.Dispose() }

        if ($studentIds.Count -eq 0) {
            throw 'No student records were found; no demo sessions can be generated.'
        }

        # Choose a genuinely assigned tutor-course pair, not an invented foreign key.
        $pair = $db.CreateCommand()
        $pair.CommandText = 'SELECT TOP 1 [TutorID],[CourseID] FROM [TUTOR_COURSE] ORDER BY [TutorID],[CourseID]'
        $reader = $pair.ExecuteReader()
        try {
            if (-not $reader.Read() -or $reader.IsDBNull(0) -or $reader.IsDBNull(1)) {
                throw 'Assign a course to a tutor in Manage Tutors before preparing demonstration sessions.'
            }
            $tutorId = [int]$reader.GetValue(0)
            $courseId = [int]$reader.GetValue(1)
        }
        finally { $reader.Close(); $pair.Dispose() }

        function Add-Param([System.Data.OleDb.OleDbCommand]$cmd,
                           [System.Data.OleDb.OleDbType]$kind, [object]$value) {
            $parameter = $cmd.Parameters.Add("@p$($cmd.Parameters.Count)", $kind)
            $parameter.Value = $value
        }

        function Insert-DemoRequest([int]$requestId, [int]$studentId,
                                     [int]$courseId, [datetime]$day,
                                     [datetime]$start, [datetime]$end) {
            $cmd = $db.CreateCommand()
            try {
                $cmd.Transaction = $tx
                $cmd.CommandText = 'INSERT INTO [TUTORING_REQUEST] ([RequestID],[StudentID],[CourseID],[SupportType],[PreferredDate],[PreferredStartTime],[PreferredEndTime],[RequestDate],[Status]) VALUES (?,?,?,?,?,?,?,?,?)'
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Integer) $requestId
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Integer) $studentId
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Integer) $courseId
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::VarWChar) 'Individual tutoring'
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Date) $day
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Date) $start
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Date) $end
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Date) ($day.AddDays(-10))
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::VarWChar) 'Scheduled'
                if ($cmd.ExecuteNonQuery() -ne 1) { throw 'Unable to insert demo request.' }
            }
            finally { $cmd.Dispose() }
        }

        function Insert-DemoSession([int]$sessionId, [int]$requestId, [int]$tutorId,
                                     [datetime]$day, [datetime]$start,
                                     [datetime]$end, [string]$attendance) {
            $cmd = $db.CreateCommand()
            try {
                $cmd.Transaction = $tx
                $cmd.CommandText = 'INSERT INTO [TUTORING_SESSION] ([SessionID],[RequestID],[TutorID],[SessionDate],[StartTime],[EndTime],[SessionStatus],[Feedback],[AttendanceStatus]) VALUES (?,?,?,?,?,?,?,?,?)'
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Integer) $sessionId
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Integer) $requestId
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Integer) $tutorId
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Date) $day
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Date) $start
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::Date) $end
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::VarWChar) 'Completed'
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::VarWChar) 'SYNTHETIC DEMO - not a real tutoring session'
                Add-Param $cmd ([System.Data.OleDb.OleDbType]::VarWChar) $attendance
                if ($cmd.ExecuteNonQuery() -ne 1) { throw 'Unable to insert demo session.' }
            }
            finally { $cmd.Dispose() }
        }

        $tx = $db.BeginTransaction()
        $created = 0
        $months = @(2, 5, 8, 10)  # Provisional calendar-quarter mapping in the current report.
        for ($i = 0; $i -lt $studentIds.Count; $i++) {
            for ($term = 1; $term -le 4; $term++) {
                for ($slot = 0; $slot -lt 2; $slot++) {
                    # For October all example sessions have occurred before 26 Oct 2026.
                    $day = [datetime]::new(2026, $months[$term-1], 3 + 2 * $i + $slot)
                    $start = $day.AddHours(10)
                    $end = $day.AddHours(11)

                    $pattern = ($i + $term) % 4
                    $attend = if ($pattern -eq 0) { $false } elseif ($pattern -eq 2) {
                        $true
                    } else { $slot -eq 0 }
                    $attendance = if ($attend) { 'Attended' } else { 'Absent' }

                    Insert-DemoRequest $nextRequest $studentIds[$i] $courseId $day $start $end
                    Insert-DemoSession $nextSession $nextRequest $tutorId $day $start $end $attendance
                    $nextRequest++
                    $nextSession++
                    $created++
                }
            }
        }
        $tx.Commit()
        $tx = $null
        $description = @"
SYNTHETIC EDUCONNECT DEMONSTRATION DATABASE
Created from a COPY of the local Edu_Connect.accdb.
Added $created artificial historical tutoring requests and completed sessions across
four provisional calendar quarters of 2026 for $($studentIds.Count) students.
The original database was not written to. Demo sessions must NOT be presented
as genuine attendance, genuine academic outcomes or evidence of effectiveness.
The GPA records were not changed.
"@
        [System.IO.File]::WriteAllText($marker, $description)
        Write-Host "Created $created DEMO sessions in: $demo" -ForegroundColor Green
        Write-Host 'The original Edu_Connect.accdb was NOT changed.' -ForegroundColor Green
    }
    catch {
        if ($null -ne $tx) {
            try { $tx.Rollback() } catch { }
        }
        $db.Dispose()
        Remove-Item -LiteralPath $demoFolder -Recurse -Force -ErrorAction SilentlyContinue
        throw
    }
    finally { $db.Dispose() }
}
else {
    Write-Host 'Existing synthetic demo database found; its records were preserved.' -ForegroundColor Yellow
}

$env:EDUCONNECT_DEMO_MODE = '1'
if (Test-Path -LiteralPath $exe) {
    Write-Host 'Opening EduConnect in DEMO mode. Sign in using your existing copied accounts.' -ForegroundColor Cyan
    & $exe
}
else {
    Write-Host 'Demo data is ready. Build EduConnect in Visual Studio, then rerun this script to open it.' -ForegroundColor Yellow
}
