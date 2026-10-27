$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$root=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$original=Join-Path $root 'Edu_Connect.accdb'
$lock=Join-Path $root 'Edu_Connect.laccdb'
$folder=Join-Path $root 'DatabaseBackups'
$marker='DEMO ONLY'
$connection=$null
$transaction=$null
if (-not [System.IO.File]::Exists($original)) { throw "Project database not found: $original" }
if ([System.IO.File]::Exists($lock)) { throw 'Close Access and EduConnect before proceeding.' }
[System.IO.Directory]::CreateDirectory($folder) | Out-Null
$stamp=Get-Date -Format 'yyyyMMdd_HHmmss_fff'
$backup=Join-Path $folder "Edu_Connect_before_admin_repair_$stamp.accdb"
$working=Join-Path $folder "Edu_Connect_admin_repair_working_$stamp.accdb"
$replaceBackup=Join-Path $folder "Edu_Connect_admin_repair_replaced_$stamp.accdb"
[System.IO.File]::Copy($original,$backup,$false)
[System.IO.File]::Copy($backup,$working,$false)
Write-Host "Original database backup: $backup" -ForegroundColor Green
Write-Host 'Repairs ONLY requests originally marked DEMO ONLY; all writes occur on a working copy.' -ForegroundColor Yellow

function Column([System.Data.DataTable]$table,[string]$name) {
    foreach($c in $table.Columns) {
        $actual=[string]$c.ColumnName
        $normalized=$actual.TrimStart([char[]]@([char]0xFEFF,[char]0x200B,[char]0x2060,[char]' '))
        $requested=$name.TrimStart([char[]]@([char]0xFEFF,[char]0x200B,[char]0x2060,[char]' '))
        if([string]::Equals($normalized,$requested,[System.StringComparison]::OrdinalIgnoreCase)) {
            return $actual
        }
    }
    throw "Missing required column $name in $($table.TableName). The project database was not changed."
}
function Field([System.Data.DataRow]$row,[string]$name) {
    $actual=Column $row.Table $name
    if($row.IsNull($actual)) { return $null }
    return $row[$actual]
}
function Q([string]$name) { return '['+$name.Replace(']',']]')+']' }
function Load([string]$name) {
    $command=$connection.CreateCommand()
    try {
        $command.CommandText="SELECT * FROM [$name]"
        $reader=$command.ExecuteReader()
        try {
            $data=[System.Data.DataTable]::new()
            $data.Load($reader)
            return ,$data
        }
        finally { $reader.Close() }
    }
    finally { $command.Dispose() }
}
function Execute([string]$sql,[string[]]$types,[object[]]$values) {
    if($types.Count -ne $values.Count) { throw 'Parameter count mismatch.' }
    $command=$connection.CreateCommand()
    try {
        $command.Transaction=$transaction
        $command.CommandText=$sql
        for($i=0;$i -lt $types.Count;$i++) {
            $kind=[System.Enum]::Parse([System.Data.OleDb.OleDbType],$types[$i])
            $parameter=$command.Parameters.Add("@p$i",$kind)
            $parameter.Value=$values[$i]
        }
        return [int]$command.ExecuteNonQuery()
    }
    finally { $command.Dispose() }
}
function ToTime([object]$value) {
    if($value -is [datetime]) { return ([datetime]$value).TimeOfDay }
    if($value -is [timespan]) { return [timespan]$value }
    return ([datetime]::Parse([string]$value)).TimeOfDay
}
function Overlaps([object]$start,[object]$end) {
    if($null -eq $start -or $null -eq $end) { return $false }
    return ((ToTime $start) -lt [timespan]::FromHours(11) -and
            (ToTime $end) -gt [timespan]::FromHours(10))
}
function SameDay([object]$when,[datetime]$day) {
    if($null -eq $when) { return $false }
    return ([datetime]$when).Date -eq $day.Date
}
function IsTrue([object]$value) {
    if($null -eq $value) { return $false }
    $text=([string]$value).Trim()
    return ($text -eq 'True' -or $text -eq 'Yes' -or $text -eq '1' -or $text -eq '-1')
}
function AvailabilityFieldInfo {
    $cmd=$connection.CreateCommand()
    try {
        $cmd.CommandText='SELECT * FROM [TUTOR_AVAILABILITY] WHERE 1=0'
        $reader=$cmd.ExecuteReader([System.Data.CommandBehavior]::SchemaOnly)
        try {
            foreach($entry in $reader.GetSchemaTable().Rows) {
                $name=[string]$entry['ColumnName']
                $normal=$name.TrimStart([char[]]@([char]0xFEFF,[char]' '))
                if($normal -eq 'AvailabilityID' -or $normal -eq 'TutorAvailabilityID') {
                    $auto=$false
                    if($entry.Table.Columns.Contains('IsAutoIncrement') -and
                        $entry['IsAutoIncrement'] -ne [System.DBNull]::Value) {
                        $auto=[bool]$entry['IsAutoIncrement']
                    }
                    return [pscustomobject]@{Name=$name;Automatic=$auto}
                }
            }
        }
        finally { $reader.Close() }
    }
    finally { $cmd.Dispose() }
    return $null
}
function MaxAvailabilityId([System.Data.DataTable]$table,[string]$name) {
    $max=0
    foreach($r in $table.Rows) {
        if(-not $r.IsNull($name) -and [int]$r[$name] -gt $max) { $max=[int]$r[$name] }
    }
    return $max
}

try {
    $connection=[System.Data.OleDb.OleDbConnection]::new(
        "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$working")
    $connection.Open()
    $requests=Load 'TUTORING_REQUEST'
    $sessions=Load 'TUTORING_SESSION'
    $availability=Load 'TUTOR_AVAILABILITY'
    $tutorCourses=Load 'TUTOR_COURSE'
    $tutors=Load 'TUTOR'
    $courses=Load 'COURSE'
    foreach($f in @('RequestID','Status','SupportType','CourseID','StudentID','PreferredDate',
                    'PreferredStartTime','PreferredEndTime')) { [void](Column $requests $f) }
    foreach($f in @('TutorID','CourseID')) { [void](Column $tutorCourses $f) }
    foreach($f in @('TutorID','AvailableDate','StartTime','EndTime','IsAvailable')) { [void](Column $availability $f) }
    foreach($f in @('TutorID','SessionDate','StartTime','EndTime','SessionStatus')) { [void](Column $sessions $f) }
    [void](Column $tutors 'TutorID')
    [void](Column $courses 'CourseID')

    $tutorSet=[System.Collections.Generic.HashSet[int]]::new()
    foreach($r in $tutors.Rows) {
        $v=Field $r 'TutorID'
        if($null -ne $v) { [void]$tutorSet.Add([int]$v) }
    }
    $courseSet=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach($r in $courses.Rows) {
        $v=Field $r 'CourseID'
        if($null -ne $v) { [void]$courseSet.Add(([string]$v).Trim()) }
    }
    $pairs=@()
    foreach($r in $tutorCourses.Rows) {
        $t=Field $r 'TutorID'
        $c=Field $r 'CourseID'
        if($null -eq $t -or $null -eq $c) { continue }
        $tid=[int]$t
        $cid=([string]$c).Trim()
        if($tutorSet.Contains($tid) -and $courseSet.Contains($cid)) {
            $pairs+=([pscustomobject]@{Tutor=$tid;Course=$cid})
        }
    }

    $demo=@()
    foreach($r in $requests.Rows) {
        if(([string](Field $r 'SupportType')).Trim() -eq $marker) { $demo+= $r }
    }
    if($demo.Count -eq 0) {
        Write-Host 'There are no DEMO ONLY Support Type records to repair. No database was replaced.' -ForegroundColor Yellow
        return
    }
    $verified=@($demo | Where-Object { ([string](Field $_ 'Status')).Trim() -eq 'Verified' })
    if($verified.Count -gt 0 -and $pairs.Count -eq 0) {
        throw 'No tutor-course assignments exist. Cannot create matchable Verified test requests.'
    }
    $availabilityId=AvailabilityFieldInfo
    $nextId=0
    if($null -ne $availabilityId -and -not $availabilityId.Automatic) {
        $nextId=(MaxAvailabilityId $availability $availabilityId.Name)+1
    }

    $assigned=@()
    $reserved=@{}
    $fixedSupport=0
    $slotsAdded=0
    $verifiedPrepared=0
    $txnRequestId=Q (Column $requests 'RequestID')
    $txnSupport=Q (Column $requests 'SupportType')
    $txnCourse=Q (Column $requests 'CourseID')
    $txnDate=Q (Column $requests 'PreferredDate')

    $transaction=$connection.BeginTransaction()
    foreach($r in $verified) {
        $id=[int](Field $r 'RequestID')
        $student=[int](Field $r 'StudentID')
        $currentCourse=([string](Field $r 'CourseID')).Trim()
        $qualified=@($pairs | Where-Object { $_.Course -eq $currentCourse })
        if($qualified.Count -eq 0) { $qualified=@($pairs) }
        $selected=$null
        $selectedDay=$null
        $existingCoverage=$false

        for($offset=0;$offset -lt 100 -and $null -eq $selected;$offset++) {
            $day=[datetime]::Today.AddDays(12+$offset)
            foreach($candidate in $qualified) {
                $tid=[int]$candidate.Tutor
                $token=$day.ToString('yyyyMMdd')+'|'+$tid.ToString()
                if($reserved.ContainsKey($token)) { continue }
                $conflict=$false
                foreach($session in $sessions.Rows) {
                    $t=Field $session 'TutorID'
                    if($null -eq $t -or [int]$t -ne $tid) { continue }
                    $status=[string](Field $session 'SessionStatus')
                    if($status -eq 'Cancelled' -or $status -eq 'Canceled') { continue }
                    if((SameDay (Field $session 'SessionDate') $day) -and
                       (Overlaps (Field $session 'StartTime') (Field $session 'EndTime'))) {
                        $conflict=$true
                        break
                    }
                }
                if($conflict) { continue }
                foreach($request in $requests.Rows) {
                    $other=Field $request 'RequestID'
                    $sId=Field $request 'StudentID'
                    if($null -eq $other -or $null -eq $sId) { continue }
                    if([int]$other -eq $id -or [int]$sId -ne $student) { continue }
                    $status=[string](Field $request 'Status')
                    if($status -eq 'Cancelled' -or $status -eq 'Completed') { continue }
                    if((SameDay (Field $request 'PreferredDate') $day) -and
                       (Overlaps (Field $request 'PreferredStartTime') (Field $request 'PreferredEndTime'))) {
                        $conflict=$true
                        break
                    }
                }
                if($conflict) { continue }
                foreach($entry in $assigned) {
                    if($entry.Student -eq $student -and $entry.Day -eq $day) {
                        $conflict=$true
                        break
                    }
                }
                if($conflict) { continue }

                $covered=$false
                $partial=$false
                foreach($slot in $availability.Rows) {
                    $t=Field $slot 'TutorID'
                    if($null -eq $t -or [int]$t -ne $tid) { continue }
                    if(-not (SameDay (Field $slot 'AvailableDate') $day)) { continue }
                    if(-not (IsTrue (Field $slot 'IsAvailable'))) { continue }
                    $start=Field $slot 'StartTime'
                    $end=Field $slot 'EndTime'
                    if($null -eq $start -or $null -eq $end) { continue }
                    if((ToTime $start) -le [timespan]::FromHours(10) -and
                       (ToTime $end) -ge [timespan]::FromHours(11)) {
                        $covered=$true
                    }
                    elseif((Overlaps $start $end)) { $partial=$true }
                }
                if($partial -and -not $covered) { continue }
                $selected=$candidate
                $selectedDay=$day
                $existingCoverage=$covered
                break
            }
        }
        if($null -eq $selected) {
            throw "No conflict-free tutor and future slot found for demo request $id."
        }
        if(-not $existingCoverage) {
            $fields=@('TutorID','AvailableDate','StartTime','EndTime','IsAvailable')
            $types=@('Integer','Date','Date','Date','Boolean')
            $values=@([int]$selected.Tutor,$selectedDay,
                      $selectedDay.AddHours(9),$selectedDay.AddHours(12),$true)
            if($null -ne $availabilityId -and -not $availabilityId.Automatic) {
                $fields=@($availabilityId.Name)+$fields
                $types=@('Integer')+$types
                $values=@($nextId)+$values
                $nextId++
            }
            $columns=@()
            foreach($col in $fields) { $columns+=(Q (Column $availability $col)) }
            $placeholders=(@('?')*$fields.Count)-join ','
            $sql="INSERT INTO [TUTOR_AVAILABILITY] ("+($columns-join ',')+") VALUES ($placeholders)"
            if((Execute $sql $types $values) -ne 1) { throw "Unable to create test tutor availability for request $id." }
            $slotsAdded++
        }
        if($currentCourse -ne [string]$selected.Course) {
            if((Execute "UPDATE [TUTORING_REQUEST] SET $txnCourse=? WHERE $txnRequestId=?" @('VarWChar','Integer') @([string]$selected.Course,$id)) -ne 1) {
                throw "Unable to correct course on synthetic Verified request $id."
            }
        }
        $requestStart=Q (Column $requests 'PreferredStartTime')
        $requestEnd=Q (Column $requests 'PreferredEndTime')
        if((Execute "UPDATE [TUTORING_REQUEST] SET $txnDate=?,$requestStart=?,$requestEnd=? WHERE $txnRequestId=?" @('Date','Date','Date','Integer') @($selectedDay,$selectedDay.AddHours(10),$selectedDay.AddHours(11),$id)) -ne 1) {
            throw "Unable to schedule synthetic Verified request $id."
        }
        $reserved[$selectedDay.ToString('yyyyMMdd')+'|'+[int]$selected.Tutor]=$true
        $assigned+=([pscustomobject]@{Request=$id;Student=$student;Tutor=[int]$selected.Tutor;Day=$selectedDay;Course=[string]$selected.Course})
        $verifiedPrepared++
    }

    foreach($r in $demo) {
        $id=[int](Field $r 'RequestID')
        if((Execute "UPDATE [TUTORING_REQUEST] SET $txnSupport=? WHERE $txnRequestId=? AND $txnSupport=?" @('VarWChar','Integer','VarWChar') @('Individual Tutoring',$id,$marker)) -ne 1) {
            throw "Could not fix Support Type for synthetic request $id."
        }
        $fixedSupport++
    }
    $transaction.Commit()
    $transaction=$null

    $requestsAfter=Load 'TUTORING_REQUEST'
    $availabilityAfter=Load 'TUTOR_AVAILABILITY'
    if($requestsAfter.Rows.Count -ne $requests.Rows.Count -or
        $availabilityAfter.Rows.Count -ne $availability.Rows.Count+$slotsAdded) {
        throw 'Row count verification failed. Original database was not replaced.'
    }
    foreach($r in $requestsAfter.Rows) {
        if(([string](Field $r 'SupportType')).Trim() -eq $marker) {
            throw 'DEMO ONLY still appears in Support Type. Original database was not replaced.'
        }
    }
    foreach($entry in $assigned) {
        $found=$false
        foreach($r in $requestsAfter.Rows) {
            if([int](Field $r 'RequestID') -ne $entry.Request) { continue }
            $found=([string](Field $r 'Status')).Trim() -eq 'Verified' -and
                ([string](Field $r 'CourseID')).Trim() -eq $entry.Course -and
                (SameDay (Field $r 'PreferredDate') $entry.Day)
            break
        }
        if(-not $found) { throw "Verified request $($entry.Request) failed post-write validation." }
        $covered=$false
        foreach($slot in $availabilityAfter.Rows) {
            $id=Field $slot 'TutorID'
            if($null -eq $id -or [int]$id -ne $entry.Tutor) { continue }
            if(-not (SameDay (Field $slot 'AvailableDate') $entry.Day)) { continue }
            if(-not (IsTrue (Field $slot 'IsAvailable'))) { continue }
            if((ToTime (Field $slot 'StartTime')) -le [timespan]::FromHours(10) -and
               (ToTime (Field $slot 'EndTime')) -ge [timespan]::FromHours(11)) {
                $covered=$true
                break
            }
        }
        if(-not $covered) { throw "No verified availability for request $($entry.Request)." }
    }

    $connection.Close()
    $connection.Dispose()
    $connection=$null
    [System.IO.File]::Replace($working,$original,$replaceBackup)
    Write-Host ''
    Write-Host 'ADMIN DEMO BOOKING REPAIR SUCCESSFUL' -ForegroundColor Green
    Write-Host "Valid Support Type values restored: $fixedSupport"
    Write-Host "Verified test requests prepared for assignment: $verifiedPrepared"
    Write-Host "Tutor availability slots created for those requests: $slotsAdded"
    Write-Host "No real requests, completed sessions, or login accounts were modified."
    Write-Host "Original database backup: $backup"
}
catch {
    Write-Host 'Repair failed. The original database was not intentionally replaced.' -ForegroundColor Red
    Write-Host "Original database backup: $backup" -ForegroundColor Yellow
    throw
}
finally {
    if($null -ne $transaction) { try { $transaction.Rollback() } catch {} }
    if($null -ne $connection) { $connection.Dispose() }
}
