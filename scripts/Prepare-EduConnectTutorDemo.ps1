param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Edu_Connect.accdb'),
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$DatabasePath = [IO.Path]::GetFullPath($DatabasePath)
if (-not (Test-Path -LiteralPath $DatabasePath)) { throw "Database not found: $DatabasePath" }
if (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($DatabasePath, '.laccdb'))) { throw 'Close Access and EduConnect before continuing.' }

function Open-Db([string]$path) {
    foreach ($provider in @('Microsoft.ACE.OLEDB.12.0','Microsoft.ACE.OLEDB.16.0')) {
        $db = New-Object System.Data.OleDb.OleDbConnection("Provider=$provider;Data Source=$path;Persist Security Info=False;")
        try { $db.Open(); return $db } catch { $db.Dispose() }
    }
    throw 'Unable to open Access database. Check Access Database Engine architecture.'
}
function Scalar($db,$tx,[string]$sql,[object[]]$values=@()) {
    $c=$db.CreateCommand(); $c.CommandText=$sql; if($null -ne $tx){$c.Transaction=$tx}
    foreach($v in $values){$p=$c.CreateParameter();$p.Value=$v;[void]$c.Parameters.Add($p)}
    try {return $c.ExecuteScalar()} finally {$c.Dispose()}
}
function Exec($db,$tx,[string]$sql,[object[]]$values) {
    $c=$db.CreateCommand();$c.CommandText=$sql;$c.Transaction=$tx
    foreach($v in $values){$p=$c.CreateParameter();$p.Value=$v;[void]$c.Parameters.Add($p)}
    try{return $c.ExecuteNonQuery()} finally {$c.Dispose()}
}
function Next-Id($db,$tx,[string]$table,[string]$key) {
    $v=Scalar $db $tx "SELECT MAX([$key]) FROM [$table]"
    if($null -eq $v -or $v -is [DBNull]) {return 1}
    return ([int]$v+1)
}
function Time-Val([int]$hour) { return ([datetime]'1899-12-30').AddHours($hour) }
$examples=@(
    [pscustomobject]@{Date=[datetime]'2026-10-20';Start=10;End=11;Attendance='Attended';Status='Completed';Feedback='EDUCONNECT SYNTHETIC TEST: reviewed algorithms and solved practice questions.'},
    [pscustomobject]@{Date=[datetime]'2026-10-22';Start=14;End=15;Attendance='Absent';Status='Completed';Feedback='EDUCONNECT SYNTHETIC TEST: student absent from scheduled session.'},
    [pscustomobject]@{Date=[datetime]'2026-10-27';Start=10;End=11;Attendance=$null;Status='Scheduled';Feedback=$null}
)
$db=Open-Db $DatabasePath
try {
    if([int](Scalar $db $null 'SELECT COUNT(*) FROM [STUDENT] WHERE [StudentID]=?' @(9)) -ne 1){throw 'StudentID 9 missing.'}
    if([int](Scalar $db $null 'SELECT COUNT(*) FROM [COURSE] WHERE [CourseID]=?' @(3)) -ne 1){throw 'CourseID 3 missing.'}
    if([int](Scalar $db $null 'SELECT COUNT(*) FROM [TUTOR] WHERE [TutorID]=?' @(5)) -ne 1){throw 'TutorID 5 missing.'}
    if([int](Scalar $db $null 'SELECT COUNT(*) FROM [TUTOR_COURSE] WHERE [TutorID]=? AND [CourseID]=?' @(5,3)) -lt 1){throw 'TutorID 5 does not teach CourseID 3.'}
    $existing=0
    foreach($e in $examples){
        if($e.Date.AddHours($e.End) -ge [datetime]::Now){throw "Example session has not finished: $($e.Date)"}
        $n=[int](Scalar $db $null 'SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [TutorID]=? AND [SessionDate]=? AND [StartTime]=?' @(5,$e.Date,(Time-Val $e.Start)))
        if($n -gt 1){throw "Duplicate sessions exist for $($e.Date). Review before proceeding."}
        if($n -eq 1){$existing++}
    }
    Write-Host "Database: $DatabasePath"
    Write-Host "Already matching session dates: $existing / 3"
    Write-Host 'Sample session plan for TutorID 5, StudentID 9, CourseID 3:'
    foreach($e in $examples){Write-Host (" {0:yyyy-MM-dd} {1:00}:00-{2:00}:00 | {3} | {4}" -f $e.Date,$e.Start,$e.End,$e.Status,$e.Attendance)}
    Write-Host 'Two completed sample attendance records; one past Scheduled session to complete through Tutor UI.'
    Write-Host 'Existing sessions and all login, GPA, course, and availability records are preserved.'
    if(-not $Apply){Write-Host 'PREVIEW ONLY - nothing changed. Use -Apply to add missing sample sessions.';return}
} finally {$db.Dispose()}

$backup="$DatabasePath.tutor-demo-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Copy-Item -LiteralPath $DatabasePath -Destination $backup -ErrorAction Stop
Write-Host "Backup: $backup"
$db=Open-Db $DatabasePath
try {
    $tx=$db.BeginTransaction()
    try {
        $inserted=0
        foreach($e in $examples){
            $date=$e.Date; $start=Time-Val $e.Start; $end=Time-Val $e.End
            $exists=[int](Scalar $db $tx 'SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [TutorID]=? AND [SessionDate]=? AND [StartTime]=?' @(5,$date,$start))
            if($exists -gt 0){Write-Host ("Skipping existing sample slot: {0:yyyy-MM-dd} {1:00}:00" -f $date,$e.Start);continue}
            $rid=Next-Id $db $tx 'TUTORING_REQUEST' 'RequestID'
            $sid=Next-Id $db $tx 'TUTORING_SESSION' 'SessionID'
            $r=Exec $db $tx 'INSERT INTO [TUTORING_REQUEST] ([RequestID],[StudentID],[CourseID],[SupportType],[PreferredDate],[PreferredStartTime],[PreferredEndTime],[RequestDate],[Status]) VALUES (?,?,?,?,?,?,?,?,?)' @($rid,9,3,'Individual tutoring',$date,$start,$end,$date.AddDays(-7),$e.Status)
            if($r -ne 1){throw "Failed to insert request $rid"}
            $attendance=if($null -eq $e.Attendance){[DBNull]::Value}else{$e.Attendance}
            $feedback=if($null -eq $e.Feedback){[DBNull]::Value}else{$e.Feedback}
            $s=Exec $db $tx 'INSERT INTO [TUTORING_SESSION] ([SessionID],[RequestID],[TutorID],[SessionDate],[StartTime],[EndTime],[SessionStatus],[Feedback],[AttendanceStatus]) VALUES (?,?,?,?,?,?,?,?,?)' @($sid,$rid,5,$date,$start,$end,$e.Status,$feedback,$attendance)
            if($s -ne 1){throw "Failed to insert session $sid"}
            $inserted++
            Write-Host ("Created sample Request {0} and Session {1}: {2:yyyy-MM-dd}, {3}" -f $rid,$sid,$date,$e.Status)
        }
        $tx.Commit()
        Write-Host "SUCCESS: $inserted sample session(s) added. TutorID 5 can record attendance on the past Scheduled session."
    } catch {try{$tx.Rollback()}catch{};throw} finally{$tx.Dispose()}
} finally{$db.Dispose()}
