$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$original=Join-Path $root 'Edu_Connect.accdb'
$lock=Join-Path $root 'Edu_Connect.laccdb'
$folder=Join-Path $root 'DatabaseBackups'
$attendanceTag='EDUCONNECT SYNTHETIC TEST ATTENDANCE - NOT REAL'
$gpaTag='EDUCONNECT SYNTHETIC TEST GPA - NOT REAL'
$scheduledTag='EDUCONNECT SYNTHETIC TEST SCHEDULED - NOT REAL'
$scenarioTag='DEMO ONLY'
$connection=$null
$transaction=$null
if (-not [System.IO.File]::Exists($original)) { throw "Database missing: $original" }
if ([System.IO.File]::Exists($lock)) { throw 'Close Access and EduConnect before proceeding.' }
[System.IO.Directory]::CreateDirectory($folder) | Out-Null
$stamp=Get-Date -Format 'yyyyMMdd_HHmmss_fff'
$backup=Join-Path $folder "Edu_Connect_before_polish_$stamp.accdb"
$working=Join-Path $folder "Edu_Connect_polish_working_$stamp.accdb"
$replaceBackup=Join-Path $folder "Edu_Connect_before_polish_install_$stamp.accdb"
[System.IO.File]::Copy($original,$backup,$false)
[System.IO.File]::Copy($backup,$working,$false)
Write-Host "BACKUP: $backup" -ForegroundColor Green
Write-Host 'All data changes take place on a separate working copy. Only synthetic test records are edited.' -ForegroundColor Yellow

function Column([System.Data.DataTable]$table,[string]$requested) {
    foreach ($c in $table.Columns) {
        $name=([string]$c.ColumnName).TrimStart([char[]]@([char]0xFEFF,[char]0x200B,[char]0x2060,[char]' '))
        if ([string]::Equals($name,$requested,[System.StringComparison]::OrdinalIgnoreCase)) {
            return [string]$c.ColumnName
        }
    }
    throw "Required database field $requested is missing. Original database unchanged."
}
function Field([System.Data.DataRow]$row,[string]$column) {
    $actual=Column $row.Table $column
    if ($row.IsNull($actual)) { return $null }
    return $row[$actual]
}
function Q([string]$text) { return '['+$text.Replace(']',']]')+']' }
function CsvColumns([System.Data.DataTable]$schema,[string[]]$fields) {
    $names=@()
    foreach ($field in $fields) { $names+=(Q (Column $schema $field)) }
    return ($names -join ',')
}
function Load([string]$name) {
    $cmd=$connection.CreateCommand()
    try {
        $cmd.CommandText="SELECT * FROM [$name]"
        $reader=$cmd.ExecuteReader()
        try {
            $table=[System.Data.DataTable]::new()
            $table.Load($reader)
            return ,$table
        }
        finally { $reader.Close() }
    }
    finally { $cmd.Dispose() }
}
function Execute([string]$sql,[string[]]$types,[object[]]$values) {
    if ($types.Count -ne $values.Count) { throw "Parameter count mismatch." }
    $cmd=$connection.CreateCommand()
    try {
        $cmd.Transaction=$transaction
        $cmd.CommandText=$sql
        for($j=0;$j -lt $types.Count;$j++) {
            $kind=[System.Enum]::Parse([System.Data.OleDb.OleDbType],$types[$j])
            $p=$cmd.Parameters.Add("@p$j",$kind)
            $p.Value=$values[$j]
        }
        return [int]$cmd.ExecuteNonQuery()
    }
    finally { $cmd.Dispose() }
}
function Insert([string]$tableName,[System.Data.DataTable]$table,[string[]]$names,[string[]]$types,[object[]]$values) {
    $cols=CsvColumns $table $names
    $marks=(@('?')*$names.Count) -join ','
    if ((Execute "INSERT INTO [$tableName] ($cols) VALUES ($marks)" $types $values) -ne 1) {
        throw "Cannot insert synthetic test row in $tableName."
    }
}
function Update([string]$tableName,[System.Data.DataTable]$table,[string]$keyName,[int]$id,[string]$field,[string]$type,[object]$value) {
    $col=Q (Column $table $field)
    $key=Q (Column $table $keyName)
    if ((Execute "UPDATE [$tableName] SET $col=? WHERE $key=?" @($type,'Integer') @($value,$id)) -ne 1) {
        throw "Update failed on $tableName ID $id."
    }
}
function MaxID([System.Data.DataTable]$table,[string]$field) {
    $n=0
    foreach($row in $table.Rows) {
        $v=Field $row $field
        if ($null -ne $v -and [int]$v -gt $n) { $n=[int]$v }
    }
    return $n
}
function AddRequest([System.Data.DataTable]$schema,[int]$id,[int]$student,[string]$course,[datetime]$day,[string]$status,[datetime]$submitted) {
    $start=$day.Date.AddHours(10)
    $end=$start.AddHours(1)
    Insert 'TUTORING_REQUEST' $schema @('RequestID','StudentID','CourseID','SupportType','PreferredDate','PreferredStartTime','PreferredEndTime','RequestDate','Status') @('Integer','Integer','VarWChar','VarWChar','Date','Date','Date','Date','VarWChar') @($id,$student,$course,$scenarioTag,$day.Date,$start,$end,$submitted,$status)
}
function AddSession([System.Data.DataTable]$schema,[int]$id,[int]$request,[int]$tutor,[datetime]$day,[string]$status,[string]$attendance,[string]$tag) {
    $start=$day.Date.AddHours(10)
    $end=$start.AddHours(1)
    $attendanceValue=if($attendance -eq ''){[System.DBNull]::Value}else{$attendance}
    Insert 'TUTORING_SESSION' $schema @('SessionID','RequestID','TutorID','SessionDate','StartTime','EndTime','SessionStatus','Feedback','AttendanceStatus') @('Integer','Integer','Integer','Date','Date','Date','VarWChar','VarWChar','VarWChar') @($id,$request,$tutor,$day.Date,$start,$end,$status,$tag,$attendanceValue)
}

try {
    $connection=[System.Data.OleDb.OleDbConnection]::new("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$working")
    $connection.Open()
    $students=Load 'STUDENT'
    $tutors=Load 'TUTOR'
    $courses=Load 'COURSE'
    $assignments=Load 'TUTOR_COURSE'
    $grades=Load 'STUDENTACADEMICRECORD'
    $requests=Load 'TUTORING_REQUEST'
    $sessions=Load 'TUTORING_SESSION'
    foreach($name in @('StudentID','StudentNo')) { [void](Column $students $name) }
    [void](Column $tutors 'TutorID')
    [void](Column $courses 'CourseID')
    foreach($name in @('TutorID','CourseID')) { [void](Column $assignments $name) }
    foreach($name in @('AcademicRecordID','StudentNo','AcademicPeriod','GPA','RecordSource','RecordDate')) { [void](Column $grades $name) }
    foreach($name in @('RequestID','StudentID','CourseID','SupportType','PreferredDate','PreferredStartTime','PreferredEndTime','RequestDate','Status')) { [void](Column $requests $name) }
    foreach($name in @('SessionID','RequestID','TutorID','SessionDate','StartTime','EndTime','SessionStatus','AttendanceStatus','Feedback')) { [void](Column $sessions $name) }

    $people=@()
    foreach($row in $students.Rows) {
        $id=Field $row 'StudentID'
        $number=([string](Field $row 'StudentNo')).Trim()
        if($null -ne $id -and $number -ne '') { $people+=([pscustomobject]@{ID=[int]$id;Number=$number}) }
    }
    $people=@($people | Sort-Object ID | Select-Object -First 15)
    if($people.Count -eq 0) { throw 'No existing students can be used for the test data.' }
    $validTutors=[System.Collections.Generic.HashSet[int]]::new()
    foreach($row in $tutors.Rows) {
        $id=Field $row 'TutorID'
        if($null -ne $id) { [void]$validTutors.Add([int]$id) }
    }
    $validCourses=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach($row in $courses.Rows) {
        $id=Field $row 'CourseID'
        if($null -ne $id) { [void]$validCourses.Add(([string]$id).Trim()) }
    }
    $pairs=@()
    foreach($row in $assignments.Rows) {
        $id=Field $row 'TutorID'
        $course=Field $row 'CourseID'
        if($null -eq $id -or $null -eq $course) { continue }
        $id=[int]$id
        $course=([string]$course).Trim()
        if($validTutors.Contains($id) -and $validCourses.Contains($course)) {
            $pairs+=([pscustomobject]@{Tutor=$id;Course=$course})
        }
    }
    if($pairs.Count -eq 0) { throw 'No valid tutor-course pairs are available.' }

    $gradesByKey=@{}
    foreach($row in $grades.Rows) {
        $number=([string](Field $row 'StudentNo')).Trim()
        $period=([string](Field $row 'AcademicPeriod')).Trim()
        if($number -ne '' -and $period -ne '') {
            $key=($number+'|'+$period).ToUpperInvariant()
            if(-not $gradesByKey.ContainsKey($key)) { $gradesByKey[$key]=$row }
        }
    }
    $requestIndex=@{}
    $scenarios=@{}
    foreach($row in $requests.Rows) {
        $id=Field $row 'RequestID'
        if($null -eq $id) { continue }
        $requestIndex[[int]$id]=$row
        if(([string](Field $row 'SupportType')) -eq $scenarioTag) {
            $studentId=Field $row 'StudentID'
            if($null -ne $studentId) {
                $scenarioKey=([int]$studentId).ToString()+'|'+([string](Field $row 'Status'))
                $scenarios[$scenarioKey]=$true
            }
        }
    }
    $sessionIndex=@{}
    foreach($row in $sessions.Rows) {
        if(([string](Field $row 'Feedback')) -ne $attendanceTag) { continue }
        $requestId=Field $row 'RequestID'
        $day=Field $row 'SessionDate'
        if($null -eq $requestId -or $null -eq $day) { continue }
        if(-not $requestIndex.ContainsKey([int]$requestId)) { continue }
        $studentId=Field $requestIndex[[int]$requestId] 'StudentID'
        if($null -eq $studentId) { continue }
        $when=[datetime]$day
        if($when.Year -ne 2026) { continue }
        $term=[int][math]::Floor(($when.Month-1)/3)+1
        $key=([int]$studentId).ToString()+'|'+$term.ToString()
        if(-not $sessionIndex.ContainsKey($key)) { $sessionIndex[$key]=[System.Collections.ArrayList]::new() }
        [void]$sessionIndex[$key].Add($row)
    }

    $gpaPatterns=@(
        @(56,64,74,84),@(62,69,76,82),@(83,79,73,66),
        @(65,68,66,73),@(49,45,43,40),@(53,57,65,77)
    )
    $attendancePatterns=@(
        @(1,2,3,3),@(2,2,3,3),@(3,3,3,2),
        @(1,2,2,3),@(1,0,1,1),@(1,2,3,3)
    )
    $months=@(2,5,8,10)
    $oldGrades=$grades.Rows.Count
    $oldRequests=$requests.Rows.Count
    $oldSessions=$sessions.Rows.Count
    $nextRequest=(MaxID $requests 'RequestID')+1
    $nextSession=(MaxID $sessions 'SessionID')+1
    $newGpa=0
    $adjustedGpa=0
    $newAttendance=0
    $adjustedAttendance=0
    $completedRequests=0
    $newScenarios=0
    $newScheduled=0

    $transaction=$connection.BeginTransaction()
    for($i=0;$i -lt $people.Count;$i++) {
        $person=$people[$i]
        $gpaPlan=$gpaPatterns[$i%$gpaPatterns.Count]
        $attendancePlan=$attendancePatterns[$i%$attendancePatterns.Count]
        for($term=1;$term -le 4;$term++) {
            $period="2026-T$term"
            $grade=[double]($gpaPlan[$term-1]+($i%3)-1)
            $academicKey=($person.Number+'|'+$period).ToUpperInvariant()
            if($gradesByKey.ContainsKey($academicKey)) {
                $existing=$gradesByKey[$academicKey]
                if(([string](Field $existing 'RecordSource')) -eq $gpaTag) {
                    Update 'STUDENTACADEMICRECORD' $grades 'AcademicRecordID' ([int](Field $existing 'AcademicRecordID')) 'GPA' 'Double' $grade
                    $adjustedGpa++
                }
            }
            else {
                $recordDate=[datetime]::new(2026,$months[$term-1],24)
                Insert 'STUDENTACADEMICRECORD' $grades @('StudentNo','GPA','AcademicPeriod','RecordDate','RecordSource') @('VarWChar','Double','VarWChar','Date','VarWChar') @($person.Number,$grade,$period,$recordDate,$gpaTag)
                $newGpa++
            }
            $key=$person.ID.ToString()+'|'+$term.ToString()
            $known=@()
            if($sessionIndex.ContainsKey($key)) {
                $known=@($sessionIndex[$key] | Sort-Object { [int](Field $_ 'SessionID') })
            }
            if($known.Count -gt 3) { throw "Unexpected duplicate synthetic sessions for student $($person.ID) term $term." }
            $attendedTotal=[int]$attendancePlan[$term-1]
            for($slot=0;$slot -lt 3;$slot++) {
                $attendance=if($slot -lt $attendedTotal){'Attended'}else{'Absent'}
                if($slot -lt $known.Count) {
                    $session=$known[$slot]
                    Update 'TUTORING_SESSION' $sessions 'SessionID' ([int](Field $session 'SessionID')) 'AttendanceStatus' 'VarWChar' $attendance
                    $adjustedAttendance++
                    $requestId=[int](Field $session 'RequestID')
                    if($requestIndex.ContainsKey($requestId)) {
                        $request=$requestIndex[$requestId]
                        if(([string](Field $request 'Status')) -ne 'Completed') {
                            Update 'TUTORING_REQUEST' $requests 'RequestID' $requestId 'Status' 'VarWChar' 'Completed'
                            $completedRequests++
                        }
                    }
                }
                else {
                    $pair=$pairs[($i+$term+$slot)%$pairs.Count]
                    $day=[datetime]::new(2026,$months[$term-1],10+($i%12))
                    if($day.Date -ge [datetime]::Today) { throw "Invalid future historical date $day." }
                    AddRequest $requests $nextRequest $person.ID $pair.Course $day 'Completed' ($day.AddDays(-14))
                    AddSession $sessions $nextSession $nextRequest $pair.Tutor $day 'Completed' $attendance $attendanceTag
                    $newAttendance++
                    $nextRequest++
                    $nextSession++
                }
            }
        }
        $statuses=@('Submitted','Verified','Cancelled','Scheduled')
        for($j=0;$j -lt $statuses.Count;$j++) {
            $status=$statuses[$j]
            $scenarioKey=$person.ID.ToString()+'|'+$status
            if($scenarios.ContainsKey($scenarioKey)) { continue }
            $pair=$pairs[($i+$j)%$pairs.Count]
            $when=[datetime]::Today.AddDays(12+$i+$j)
            $submitted=[datetime]::Today.AddDays(-10+$j)
            AddRequest $requests $nextRequest $person.ID $pair.Course $when $status $submitted
            if($status -eq 'Scheduled') {
                AddSession $sessions $nextSession $nextRequest $pair.Tutor $when 'Scheduled' '' $scheduledTag
                $newScheduled++
                $nextSession++
            }
            $nextRequest++
            $newScenarios++
        }
    }
    $transaction.Commit()
    $transaction=$null

    $afterGrades=Load 'STUDENTACADEMICRECORD'
    $afterRequests=Load 'TUTORING_REQUEST'
    $afterSessions=Load 'TUTORING_SESSION'
    if($afterGrades.Rows.Count -ne $oldGrades+$newGpa -or
        $afterRequests.Rows.Count -ne $oldRequests+$newAttendance+$newScenarios -or
        $afterSessions.Rows.Count -ne $oldSessions+$newAttendance+$newScheduled) {
        throw 'Record-count validation failed. Original project database was not replaced.'
    }
    $syntheticGpa=0
    $syntheticCompleted=0
    $syntheticScheduled=0
    foreach($row in $afterGrades.Rows) {
        if(([string](Field $row 'RecordSource')) -eq $gpaTag) { $syntheticGpa++ }
    }
    foreach($row in $afterSessions.Rows) {
        $tag=[string](Field $row 'Feedback')
        if($tag -eq $attendanceTag) { $syntheticCompleted++ }
        if($tag -eq $scheduledTag) { $syntheticScheduled++ }
    }
    if($syntheticGpa -lt $newGpa -or $syntheticCompleted -lt $newAttendance -or $syntheticScheduled -lt $newScheduled) {
        throw 'Synthetic source-marker verification failed.'
    }
    $gpaSums=@(0.0,0.0,0.0,0.0)
    $gpaCounts=@(0,0,0,0)
    $attendanceTotals=@(0,0,0,0)
    $attendancePresent=@(0,0,0,0)
    $statusCounts=@{}
    foreach($status in @('Submitted','Verified','Cancelled','Scheduled','Completed')) {
        $statusCounts[$status]=0
    }
    foreach($row in $afterGrades.Rows) {
        if(([string](Field $row 'RecordSource')) -ne $gpaTag) { continue }
        $period=[string](Field $row 'AcademicPeriod')
        if(-not $period.StartsWith('2026-T')) { continue }
        $termNumber=0
        if(-not [int]::TryParse($period.Substring(6),[ref]$termNumber)) { continue }
        if($termNumber -lt 1 -or $termNumber -gt 4) { continue }
        $term=$termNumber-1
        $grade=Field $row 'GPA'
        if($null -eq $grade) { continue }
        $gpaSums[$term]+=[double]$grade
        $gpaCounts[$term]++
    }
    foreach($row in $afterSessions.Rows) {
        if(([string](Field $row 'Feedback')) -ne $attendanceTag) { continue }
        $day=Field $row 'SessionDate'
        if($null -eq $day -or ([datetime]$day).Year -ne 2026) { continue }
        $term=[int][math]::Floor((([datetime]$day).Month-1)/3)
        $attendance=[string](Field $row 'AttendanceStatus')
        if($attendance -ne 'Attended' -and $attendance -ne 'Absent') { continue }
        $attendanceTotals[$term]++
        if($attendance -eq 'Attended') { $attendancePresent[$term]++ }
    }
    foreach($row in $afterRequests.Rows) {
        if(([string](Field $row 'SupportType')) -ne $scenarioTag) { continue }
        $status=[string](Field $row 'Status')
        if($statusCounts.ContainsKey($status)) { $statusCounts[$status]++ }
    }
    $gpaAverages=@()
    $attendanceRates=@()
    for($term=0;$term -lt 4;$term++) {
        if($gpaCounts[$term] -eq 0 -or $attendanceTotals[$term] -eq 0) {
            throw "Missing academic or attendance test data for term $($term+1)."
        }
        $gpaAverages+=($gpaSums[$term]/$gpaCounts[$term])
        $attendanceRates+=(100.0*$attendancePresent[$term]/$attendanceTotals[$term])
    }
    if(([math]::Abs($gpaAverages[3]-$gpaAverages[0])) -lt 2 -or
       (($attendanceRates | Measure-Object -Maximum).Maximum -
        ($attendanceRates | Measure-Object -Minimum).Minimum) -lt 10) {
        throw 'The demo graph is still too flat. Original database was not replaced.'
    }
    foreach($status in @('Submitted','Verified','Cancelled','Scheduled','Completed')) {
        if($statusCounts[$status] -eq 0) {
            throw "Demo request status $status is missing. Original database was not replaced."
        }
    }

    $connection.Close()
    $connection.Dispose()
    $connection=$null
    [System.IO.File]::Replace($working,$original,$replaceBackup)
    Write-Host ''
    Write-Host 'SUCCESS: ACTUAL EDUCONNECT DATABASE UPDATED' -ForegroundColor Green
    Write-Host "GPA: $newGpa new, $adjustedGpa existing synthetic rows adjusted."
    Write-Host "Recorded attendance: $newAttendance new historical sessions, $adjustedAttendance existing synthetic sessions adjusted."
    Write-Host "Demo requests moved to Completed: $completedRequests"
    Write-Host "New Submitted, Verified, Cancelled and Scheduled test requests: $newScenarios"
    Write-Host "Future scheduled test sessions: $newScheduled"
    Write-Host "Existing rows before update: $oldGrades GPA, $oldRequests requests, $oldSessions sessions."
    Write-Host "Original database backup: $backup"
    for($term=0;$term -lt 4;$term++) {
        Write-Host ("Term {0}: average demo GPA {1:N1}%; recorded demo attendance {2:N1}%" -f ($term+1),$gpaAverages[$term],$attendanceRates[$term])
    }
    foreach($status in @('Submitted','Verified','Scheduled','Completed','Cancelled')) {
        Write-Host ("Demo {0} requests: {1}" -f $status,$statusCounts[$status])
    }
    Write-Host 'Report 3: select year 2026 and Show chart. All synthetic data must be identified as test data.' -ForegroundColor Cyan
}
catch {
    Write-Host 'FAILED: Your original database was not intentionally replaced. Keep the backup.' -ForegroundColor Red
    Write-Host "Original backup: $backup" -ForegroundColor Cyan
    throw
}
finally {
    if($null -ne $transaction) { try { $transaction.Rollback() } catch {} }
    if($null -ne $connection) { $connection.Dispose() }
}
