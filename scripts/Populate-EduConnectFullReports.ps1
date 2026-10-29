param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Edu_Connect.accdb'),
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$DatabasePath = [IO.Path]::GetFullPath($DatabasePath)
if (-not (Test-Path -LiteralPath $DatabasePath)) { throw "Database not found: $DatabasePath" }
if (Test-Path ([IO.Path]::ChangeExtension($DatabasePath,'.laccdb'))) { throw 'Close Access and EduConnect first.' }
function Open-Db {
    foreach ($provider in @('Microsoft.ACE.OLEDB.12.0','Microsoft.ACE.OLEDB.16.0')) {
        $db = New-Object System.Data.OleDb.OleDbConnection("Provider=$provider;Data Source=$DatabasePath;Persist Security Info=False;")
        try { $db.Open(); return $db } catch { $db.Dispose() }
    }
    throw 'Access Database Engine unavailable.'
}
function Table($db,[string]$sql) {
    $t=New-Object System.Data.DataTable
    $c=$db.CreateCommand();$c.CommandText=$sql
    $a=New-Object System.Data.OleDb.OleDbDataAdapter($c)
    try {[void]$a.Fill($t)} finally {$a.Dispose();$c.Dispose()}
    return ,$t
}
function New-QueryCommand {
    param(
        [System.Data.OleDb.OleDbConnection]$Connection,
        [AllowNull()][System.Data.OleDb.OleDbTransaction]$Transaction,
        [string]$Sql,
        [object[]]$Values = @()
    )
    $expected = ([regex]::Matches($Sql, '\?')).Count
    if ($expected -ne $Values.Count) {
        throw "Parameter mismatch: SQL expects $expected value(s); received $($Values.Count). SQL: $Sql"
    }
    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    if ($null -ne $Transaction) { $command.Transaction = $Transaction }
    try {
        foreach ($value in $Values) {
            $parameter = $command.CreateParameter()
            $parameter.Value = if ($null -eq $value) { [DBNull]::Value } else { $value }
            [void]$command.Parameters.Add($parameter)
        }
        return $command
    }
    catch {
        $command.Dispose()
        throw
    }
}
function Scalar {
    param(
        [System.Data.OleDb.OleDbConnection]$Connection,
        [AllowNull()][System.Data.OleDb.OleDbTransaction]$Transaction,
        [string]$Sql,
        [object[]]$Values = @()
    )
    $command = New-QueryCommand -Connection $Connection -Transaction $Transaction -Sql $Sql -Values $Values
    try { return $command.ExecuteScalar() }
    catch { throw ("Access read failed. SQL: {0}. Cause: {1}" -f $Sql, $_.Exception.Message) }
    finally { $command.Dispose() }
}
function Exec {
    param(
        [System.Data.OleDb.OleDbConnection]$Connection,
        [AllowNull()][System.Data.OleDb.OleDbTransaction]$Transaction,
        [string]$Sql,
        [object[]]$Values = @()
    )
    $command = New-QueryCommand -Connection $Connection -Transaction $Transaction -Sql $Sql -Values $Values
    try { return $command.ExecuteNonQuery() }
    catch { throw ("Access write failed. SQL: {0}. Cause: {1}" -f $Sql, $_.Exception.Message) }
    finally { $command.Dispose() }
}
function Stamp([datetime]$date,[int]$hour){return $date.Date.AddHours($hour)}
$db=Open-Db
try {
    $students=Table $db 'SELECT [StudentID] FROM [STUDENT] ORDER BY [StudentID]'
    $tutors=Table $db 'SELECT [TutorID] FROM [TUTOR] ORDER BY [TutorID]'
    $links=Table $db 'SELECT [TutorID],[CourseID] FROM [TUTOR_COURSE] ORDER BY [TutorID],[CourseID]'
    $courses=Table $db 'SELECT [CourseID] FROM [COURSE]'
    $sessionRows=Table $db 'SELECT [SessionID],[TutorID],[SessionDate],[StartTime],[EndTime] FROM [TUTORING_SESSION]'
    if($students.Rows.Count -lt 5 -or $tutors.Rows.Count -lt 3 -or $links.Rows.Count -lt 5){throw 'Insufficient existing students, tutors or course assignments.'}
    $courseKeys=@{};foreach($r in $courses.Rows){$courseKeys[[string]$r['CourseID']]=$true}
    $eligible=@();foreach($r in $links.Rows){if($courseKeys.ContainsKey([string]$r['CourseID'])){$eligible+=,[pscustomobject]@{Tutor=[int]$r['TutorID'];Course=[int]$r['CourseID']}}}
    if($eligible.Count -lt 5){throw 'Fewer than five valid tutor/course assignments.'}
    $plan=New-Object System.Collections.ArrayList
    $terms=@(
        [datetime]'2026-02-10',[datetime]'2026-05-12',
        [datetime]'2026-08-11',[datetime]'2026-10-13'
    )
    for($i=0;$i -lt $students.Rows.Count;$i++) {
        for($slot=0;$slot -lt 2;$slot++) {
            $term=($i+2*$slot)%4
            $d=$terms[$term].AddDays($i)
            $choice=$eligible[($i+$term) % $eligible.Count]
            $hour=9+(($i+$term) % 6)
            $a=if((($i+2*$term)%5) -eq 0){'Absent'}else{'Attended'}
            $feedback=if($a -eq 'Attended'){'Reviewed course concepts and completed guided practice.'}else{'Student did not attend the scheduled session.'}
            [void]$plan.Add([pscustomobject]@{Student=[int]$students.Rows[$i]['StudentID'];Tutor=$choice.Tutor;Course=$choice.Course;Date=$d;Hour=$hour;Status='Completed';Attendance=$a;Feedback=$feedback})
        }
    }
    $demoTutor=@($eligible | Where-Object {$_.Tutor -eq 5} | Select-Object -First 1)
    if($demoTutor.Count -eq 0){throw 'TutorID 5 must have a valid TUTOR_COURSE assignment for the recording demo.'}
    $demoStudent=@($students.Rows | Where-Object {[int]$_['StudentID'] -eq 9} | Select-Object -First 1)
    if($demoStudent.Count -eq 0){throw 'StudentID 9 is needed for the existing tutor demonstration scenario.'}
    [void]$plan.Add([pscustomobject]@{
        Student=9;Tutor=5;Course=$demoTutor[0].Course
        Date=[datetime]'2026-10-27';Hour=16
        Status='Scheduled';Attendance=$null;Feedback=$null
    })
    for($i=1;$i -lt [Math]::Min(5,$students.Rows.Count);$i++){
        $choice=$eligible[$i % $eligible.Count]
        [void]$plan.Add([pscustomobject]@{Student=[int]$students.Rows[$i]['StudentID'];Tutor=$choice.Tutor;Course=$choice.Course;Date=([datetime]'2026-11-10').AddDays($i);Hour=10+($i%4);Status='Scheduled';Attendance=$null;Feedback=$null})
    }
    $overdue=@(
        for($i=0;$i -lt [Math]::Min(10,$students.Rows.Count);$i++){
            $choice=$eligible[$i % $eligible.Count]
            [pscustomobject]@{
                Student=[int]$students.Rows[$i]['StudentID']
                Course=$choice.Course
                Submitted=([datetime]'2026-10-05').AddDays($i)
                Preferred=([datetime]'2026-11-25').AddDays($i)
            }
        }
    )
    $outstandingExisting=0
    foreach($o in $overdue){
        $n=[int](Scalar $db $null 'SELECT COUNT(*) FROM [TUTORING_REQUEST] WHERE [StudentID]=? AND [CourseID]=? AND [RequestDate]=? AND [PreferredDate]=?' @($o.Student,$o.Course,$o.Submitted,$o.Preferred))
        if($n -gt 0){$outstandingExisting++}
    }
    $existingSlots=@()
    foreach($r in $sessionRows.Rows) {
        if($r['SessionDate'] -is [DBNull] -or $r['StartTime'] -is [DBNull] -or $r['EndTime'] -is [DBNull]) {continue}
        $existingSlots+=,[pscustomobject]@{
            Tutor=[int]$r['TutorID']
            Date=([datetime]$r['SessionDate']).Date
            Start=([datetime]$r['StartTime']).TimeOfDay
            End=([datetime]$r['EndTime']).TimeOfDay
        }
    }
    $pending=@(
        foreach($p in $plan) {
            $plannedStart=[TimeSpan]::FromHours($p.Hour)
            $plannedEnd=[TimeSpan]::FromHours($p.Hour+1)
            $overlap=@($existingSlots | Where-Object {
                $_.Tutor -eq $p.Tutor -and $_.Date -eq $p.Date -and
                $_.Start -lt $plannedEnd -and $_.End -gt $plannedStart
            })
            if($overlap.Count -eq 0){$p}
        }
    )
    Write-Host "Database: $DatabasePath"
    Write-Host "Existing sessions: $($sessionRows.Rows.Count)"
    $completedPlanned=@($plan | Where-Object {$_.Status -eq 'Completed'}).Count
    $scheduledPlanned=@($plan | Where-Object {$_.Status -eq 'Scheduled'}).Count
    Write-Host "Planned historical attendance sessions: $completedPlanned (spread across four terms)"
    Write-Host "Planned Scheduled sessions: $scheduledPlanned (1 past ready for TutorID 5, 4 future)"
    Write-Host "Planned sessions total: $($plan.Count)"
    Write-Host "New sessions to add: $($pending.Count)"
    Write-Host "Overlapping existing tutor sessions skipped: $($plan.Count-$pending.Count)"
    Write-Host "Planned overdue Submitted requests: $($overdue.Count)"
    Write-Host "Outstanding Submitted requests to add: $($overdue.Count-$outstandingExisting)"
    Write-Host 'Report 1 requires Submitted requests with RequestDate at least 7 days old, not scheduled sessions.'
    Write-Host 'Report 2 requires a date range covering the scheduled and completed session dates.'
    Write-Host 'Report 3 requires Completed sessions with Attended or Absent values in academic terms T1-T4.'
    Write-Host 'Sample data is for demonstration only. No existing rows or login/academic tables are deleted.'
    if(-not $Apply){Write-Host 'PREVIEW ONLY. No data changed. Run with -Apply to insert.';return}
} finally {$db.Dispose()}
$backup="$DatabasePath.full-report-backup-$(Get-Date -Format yyyyMMdd-HHmmss)"
Copy-Item -LiteralPath $DatabasePath -Destination $backup -ErrorAction Stop
Write-Host "Backup saved: $backup"
$db=Open-Db
try {
    $tx=$db.BeginTransaction()
    try {
        $maxRequest=Scalar $db $tx 'SELECT MAX([RequestID]) FROM [TUTORING_REQUEST]'
        $maxSession=Scalar $db $tx 'SELECT MAX([SessionID]) FROM [TUTORING_SESSION]'
        $maxAvailability=Scalar $db $tx 'SELECT MAX([AvailabilityID]) FROM [TUTOR_AVAILABILITY]'
        $rid=if($maxRequest -is [DBNull]){0}else{[int]$maxRequest}
        $sid=if($maxSession -is [DBNull]){0}else{[int]$maxSession}
        $aid=if($maxAvailability -is [DBNull]){0}else{[int]$maxAvailability}
        $availabilityInserted=0
        $n=0
        foreach($p in $pending){
            $start=Stamp ([datetime]'1899-12-30') $p.Hour
            $end=Stamp ([datetime]'1899-12-30') ($p.Hour+1)
            $exists=[int](Scalar $db $tx 'SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [TutorID]=? AND [SessionDate]=? AND [StartTime]=?' @($p.Tutor,$p.Date,$start))
            if($exists -gt 0){continue}
            $available=[int](Scalar $db $tx 'SELECT COUNT(*) FROM [TUTOR_AVAILABILITY] WHERE [TutorID]=? AND [AvailableDate]=? AND [StartTime]<=? AND [EndTime]>=?' @($p.Tutor,$p.Date,$start,$end))
            if($available -eq 0){
                $aid++
                $av=Exec $db $tx 'INSERT INTO [TUTOR_AVAILABILITY] ([AvailabilityID],[TutorID],[AvailableDate],[StartTime],[EndTime],[IsAvailable]) VALUES (?,?,?,?,?,?)' @($aid,$p.Tutor,$p.Date,$start,$end,$true)
                if($av -ne 1){throw "Could not insert tutor availability $aid"}
                $availabilityInserted++
            }
            $rid++;$sid++
            $r=Exec $db $tx 'INSERT INTO [TUTORING_REQUEST] ([RequestID],[StudentID],[CourseID],[SupportType],[PreferredDate],[PreferredStartTime],[PreferredEndTime],[RequestDate],[Status]) VALUES (?,?,?,?,?,?,?,?,?)' @($rid,$p.Student,$p.Course,'Individual tutoring',$p.Date,$start,$end,$p.Date.AddDays(-7),$p.Status)
            if($r -ne 1){throw "Could not insert request $rid"}
            $attendance=if($null -eq $p.Attendance){[DBNull]::Value}else{$p.Attendance}
            $feedback=if($null -eq $p.Feedback){[DBNull]::Value}else{$p.Feedback}
            $s=Exec $db $tx 'INSERT INTO [TUTORING_SESSION] ([SessionID],[RequestID],[TutorID],[SessionDate],[StartTime],[EndTime],[SessionStatus],[Feedback],[AttendanceStatus]) VALUES (?,?,?,?,?,?,?,?,?)' @($sid,$rid,$p.Tutor,$p.Date,$start,$end,$p.Status,$feedback,$attendance)
            if($s -ne 1){throw "Could not insert session $sid"}
            $n++
        }
        $newOutstanding=0
        foreach($o in $overdue){
            $existing=[int](Scalar $db $tx 'SELECT COUNT(*) FROM [TUTORING_REQUEST] WHERE [StudentID]=? AND [CourseID]=? AND [RequestDate]=? AND [PreferredDate]=?' @($o.Student,$o.Course,$o.Submitted,$o.Preferred))
            if($existing -gt 0){continue}
            $rid++
            $start=Stamp ([datetime]'1899-12-30') 9
            $end=Stamp ([datetime]'1899-12-30') 10
            $r=Exec $db $tx 'INSERT INTO [TUTORING_REQUEST] ([RequestID],[StudentID],[CourseID],[SupportType],[PreferredDate],[PreferredStartTime],[PreferredEndTime],[RequestDate],[Status]) VALUES (?,?,?,?,?,?,?,?,?)' @($rid,$o.Student,$o.Course,'Individual tutoring',$o.Preferred,$start,$end,$o.Submitted,'Submitted')
            if($r -ne 1){throw "Could not insert outstanding request $rid"}
            $newOutstanding++
        }
                foreach($pair in @(
            @('EDUCONNECT SYNTHETIC TEST: reviewed algorithms and solved practice questions.','Reviewed algorithms and worked through practice questions.'),
            @('EDUCONNECT SYNTHETIC TEST: student absent from scheduled session.','Student did not attend the scheduled session.')
        )){
            [void](Exec $db $tx 'UPDATE [TUTORING_SESSION] SET [Feedback]=? WHERE [Feedback]=?' @($pair[1],$pair[0]))
        }
        $outstandingTotal=[int](Scalar $db $tx "SELECT COUNT(*) FROM [TUTORING_REQUEST] WHERE [Status]='Submitted' AND [RequestDate] <= ?" @([datetime]::Today.AddDays(-7)))
        if($outstandingTotal -lt 1){throw 'Verification failed: Report 1 has no qualifying overdue Submitted requests.'}
        $termCounts=@()
        foreach($quarter in 1..4){
            $begin=([datetime]'2026-01-01').AddMonths(($quarter-1)*3)
            $finish=$begin.AddMonths(3)
            $termCounts+= [int](Scalar $db $tx "SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [SessionStatus]='Completed' AND [SessionDate] >= ? AND [SessionDate] < ? AND ([AttendanceStatus]='Attended' OR [AttendanceStatus]='Absent')" @($begin,$finish))
        }
        if(@($termCounts | Where-Object {$_ -lt 1}).Count -gt 0){throw 'Verification failed: attendance is missing for one or more terms.'}
        $scheduledTotal=[int](Scalar $db $tx "SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [SessionStatus]='Scheduled' AND [SessionDate] >= ? AND [SessionDate] < ?" @([datetime]'2026-01-01',[datetime]'2027-01-01'))
        $completedTotal=[int](Scalar $db $tx "SELECT COUNT(*) FROM [TUTORING_SESSION] WHERE [SessionStatus]='Completed' AND [SessionDate] >= ? AND [SessionDate] < ?" @([datetime]'2026-01-01',[datetime]'2027-01-01'))
        if($scheduledTotal -lt 1 -or $completedTotal -lt 1){throw 'Verification failed: workload report needs Scheduled and Completed records.'}
        $tx.Commit()
        Write-Host "SUCCESS: inserted $n linked tutoring requests and sessions."
        Write-Host "Tutor availability slots inserted: $availabilityInserted"
        Write-Host "2026 workload: $scheduledTotal Scheduled and $completedTotal Completed sessions"
        Write-Host "New outstanding Submitted requests: $newOutstanding. Qualifying outstanding requests total: $outstandingTotal"
        Write-Host ("Attendance-completed sessions by term T1-T4: " + ($termCounts -join ', '))
        Write-Host '2026 T1-T4 include attendance examples; tutor 5 has a past recording-eligible slot plus upcoming scheduled sessions.'
    } catch {try{$tx.Rollback()}catch{};throw}finally{$tx.Dispose()}
}finally{$db.Dispose()}
