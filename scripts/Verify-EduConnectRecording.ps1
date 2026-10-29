param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Edu_Connect.accdb')
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Data
$DatabasePath=[IO.Path]::GetFullPath($DatabasePath)
if (-not (Test-Path -LiteralPath $DatabasePath)) {throw "Database missing: $DatabasePath"}
function Open-Db {
    foreach($provider in @('Microsoft.ACE.OLEDB.12.0','Microsoft.ACE.OLEDB.16.0')) {
        $db=New-Object System.Data.OleDb.OleDbConnection("Provider=$provider;Data Source=$DatabasePath")
        try {$db.Open();return $db} catch {$db.Dispose()}
    }
    throw 'Unable to open Access database.'
}
function Read-Data($db,[string]$sql) {
    $table=New-Object System.Data.DataTable
    $cmd=$db.CreateCommand();$cmd.CommandText=$sql
    $adapter=New-Object System.Data.OleDb.OleDbDataAdapter($cmd)
    try {[void]$adapter.Fill($table)} finally {$adapter.Dispose();$cmd.Dispose()}
    return ,$table
}
function Show-Result([string]$name,[bool]$ok,[string]$message){
    $state=if($ok){'PASS'}else{'NOT READY'}
    Write-Host "[$state] $name : $message"
}
$db=Open-Db
try {
    $requests=Read-Data $db 'SELECT [RequestID],[StudentID],[Status],[RequestDate] FROM [TUTORING_REQUEST]'
    $sessions=Read-Data $db 'SELECT [SessionID],[RequestID],[TutorID],[SessionDate],[StartTime],[EndTime],[SessionStatus],[AttendanceStatus] FROM [TUTORING_SESSION]'
    $academic=Read-Data $db 'SELECT * FROM [STUDENTACADEMICRECORD]'
    $requestStatus=@{}
    foreach($r in $requests.Rows){$requestStatus[[int]$r['RequestID']]=([string]$r['Status']).Trim()}
    $old=0
    foreach($r in $requests.Rows){
        if($r['RequestDate'] -is [DBNull]){continue}
        if(([string]$r['Status']).Trim() -eq 'Submitted' -and ([datetime]$r['RequestDate']).Date -le [datetime]::Today.AddDays(-7)) {$old++}
    }
    $scheduled=0;$completed=0;$ready=0
    $termCounts=@(0,0,0,0)
    foreach($r in $sessions.Rows){
        if($r['SessionDate'] -is [DBNull]){continue}
        $date=([datetime]$r['SessionDate']).Date
        $status=([string]$r['SessionStatus']).Trim()
        $attendance=([string]$r['AttendanceStatus']).Trim()
        if($date.Year -eq 2026) {
            if($status -eq 'Scheduled'){$scheduled++}
            if($status -eq 'Completed'){$completed++}
            if($status -eq 'Completed' -and ($attendance -eq 'Attended' -or $attendance -eq 'Absent')) {
                $q=[int][Math]::Floor(($date.Month-1)/3)
                $termCounts[$q]++
            }
        }
        if([int]$r['TutorID'] -ne 5 -or $status -ne 'Scheduled' -or
           $r['RequestID'] -is [DBNull] -or $r['EndTime'] -is [DBNull]) {continue}
        $rid=[int]$r['RequestID']
        if(-not $requestStatus.ContainsKey($rid) -or $requestStatus[$rid] -ne 'Scheduled'){continue}
        if($date -lt [datetime]::Today -or ($date -eq [datetime]::Today -and
           $date.Add(([datetime]$r['EndTime']).TimeOfDay) -lt [datetime]::Now)) {$ready++}
    }
    $isWide=$academic.Columns.Contains('AcademicYear') -and
        $academic.Columns.Contains('T1') -and $academic.Columns.Contains('T2') -and
        $academic.Columns.Contains('T3') -and $academic.Columns.Contains('T4')
    Write-Host "Database verified: $DatabasePath"
    Write-Host "Total requests: $($requests.Rows.Count); total sessions: $($sessions.Rows.Count)"
    Show-Result 'Tutor recording (Tut0005)' ($ready -gt 0) "$ready past Scheduled session(s) eligible for attendance"
    Show-Result 'Admin Report 1' ($old -gt 0) "$old Submitted requests at least 7 days old"
    Show-Result 'Admin Report 2' ($scheduled -gt 0 -and $completed -gt 0) "$scheduled Scheduled and $completed Completed sessions in 2026"
    Show-Result 'Admin Report 3' (@($termCounts|Where-Object {$_ -eq 0}).Count -eq 0) ("2026 attendance recorded T1/T2/T3/T4: " + ($termCounts -join '/'))
    Show-Result 'Wide GPA table' $isWide "STUDENTACADEMICRECORD: $($academic.Rows.Count) row(s)"
    Write-Host 'This check does not test Visual Studio build or PDF printing.'
} finally {$db.Dispose()}
