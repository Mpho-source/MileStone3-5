param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Edu_Connect.accdb')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data

function Exists-Table($db, [string]$name) {
    foreach ($row in $db.GetSchema('Tables').Rows) {
        if (([string]$row['TABLE_NAME']).Equals($name, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Load-Rows($db, [string]$tableName) {
    $result = New-Object System.Data.DataTable
    $adapter = [System.Data.OleDb.OleDbDataAdapter]::new("SELECT * FROM [$tableName]", $db)
    try { [void]$adapter.Fill($result) }
    finally { $adapter.Dispose() }
    return ,$result
}

function Field($record, [string]$name) {
    if (-not $record.Table.Columns.Contains($name)) { return $null }
    $value = $record[$name]
    if ($null -eq $value -or $value -is [DBNull]) { return $null }
    return $value
}

function Execute-Sql($db, [string]$sql) {
    $command = $db.CreateCommand()
    try {
        $command.CommandText = $sql
        return $command.ExecuteNonQuery()
    }
    finally { $command.Dispose() }
}

function Add-Grade($items, [string]$studentNo, [int]$year, [int]$term,
    $grade, [string]$source) {
    if ([string]::IsNullOrWhiteSpace($studentNo)) { throw 'A GPA record has no student number.' }
    if ($year -lt 1900 -or $year -gt 2200 -or $term -lt 1 -or $term -gt 4) {
        throw "An academic record for $studentNo has an invalid year or term."
    }
    if ($null -eq $grade) { return }
    $gpa = 0.0
    if (-not [double]::TryParse([string]$grade,
        [Globalization.NumberStyles]::Float,
        [Globalization.CultureInfo]::InvariantCulture, [ref]$gpa)) {
        throw "Non-numeric GPA for $studentNo / $year / Term $term."
    }
    if ([double]::IsNaN($gpa) -or [double]::IsInfinity($gpa) -or
        $gpa -lt 0 -or $gpa -gt 100) {
        throw "GPA is outside 0-100 for $studentNo / $year / Term $term."
    }

    $key = $studentNo.Trim().ToUpperInvariant() + '/' + $year
    if (-not $items.ContainsKey($key)) {
        $items[$key] = @{
            StudentNo = $studentNo.Trim()
            AcademicYear = $year
            Term1 = $null
            Term2 = $null
            Term3 = $null
            Term4 = $null
            RecordSource = 'Recorded data'
        }
    }
    $current = $items[$key]
    $column = 'Term' + $term
    if ($null -ne $current[$column] -and
        [math]::Abs([double]$current[$column] - $gpa) -gt 0.000001) {
        throw "Conflicting GPA values for $studentNo / $year / $column. Resolve the conflict before migrating."
    }
    $current[$column] = $gpa
    if ($source -match 'SYNTHETIC|Illustrative') {
        $current['RecordSource'] = 'Illustrative records'
    }
}

$dbPath = (Resolve-Path -LiteralPath $DatabasePath).Path
if (-not $dbPath.EndsWith('.accdb', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Provide the path to the Edu_Connect.accdb file.'
}
$handle = $null
try {
    $handle = [IO.File]::Open($dbPath, [IO.FileMode]::Open,
        [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
}
catch {
    throw 'Close Microsoft Access and EduConnect before running the migration. The database is currently locked.'
}
finally { if ($null -ne $handle) { $handle.Dispose() } }

$folder = Split-Path -Parent $dbPath
$backups = Join-Path $folder 'DatabaseBackups'
if (-not (Test-Path -LiteralPath $backups)) {
    [void](New-Item -ItemType Directory -Path $backups)
}
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backup = Join-Path $backups "Edu_Connect_before_term_layout_$stamp.accdb"
$working = Join-Path $folder "Edu_Connect_term_migration_$stamp.accdb"
Copy-Item -LiteralPath $dbPath -Destination $backup
Copy-Item -LiteralPath $dbPath -Destination $working
Write-Host "Original database backed up to: $backup"

$db = $null
try {
    $conn = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$working;Persist Security Info=False;"
    $db = [System.Data.OleDb.OleDbConnection]::new($conn)
    $db.Open()

    $records = @{}
    $studentsById = @{}
    if (Exists-Table $db 'STUDENT') {
        $students = Load-Rows $db 'STUDENT'
        foreach ($student in $students.Rows) {
            $id = Field $student 'StudentID'
            $number = Field $student 'StudentNo'
            if ($null -ne $id -and $null -ne $number) {
                $studentsById[[int]$id] = [string]$number
            }
        }
    }

    $oldTablePresent = Exists-Table $db 'STUDENTACADEMICRECORD'
    $alreadyCompact = $false
    if ($oldTablePresent) {
        $old = Load-Rows $db 'STUDENTACADEMICRECORD'
        $alreadyCompact = $old.Columns.Contains('AcademicYear') -and
            $old.Columns.Contains('Term1') -and $old.Columns.Contains('Term2') -and
            $old.Columns.Contains('Term3') -and $old.Columns.Contains('Term4')
        if ($alreadyCompact) {
            Write-Host 'Academic records already use Term1-Term4 columns. No migration needed.' -ForegroundColor Green
            return
        }
        if (-not $old.Columns.Contains('AcademicPeriod') -or -not $old.Columns.Contains('GPA')) {
            throw 'The academic-record table has an unrecognised schema. No change was made.'
        }
        foreach ($record in $old.Rows) {
            $number = [string](Field $record 'StudentNo')
            if ([string]::IsNullOrWhiteSpace($number)) {
                $id = Field $record 'StudentID'
                if ($null -ne $id -and $studentsById.ContainsKey([int]$id)) {
                    $number = $studentsById[[int]$id]
                }
            }
            $period = [string](Field $record 'AcademicPeriod')
            $periodMatch = [regex]::Match($period, '^([0-9]{4})-T([1-4])$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if (-not $periodMatch.Success) {
                throw "Unsupported academic period '$period' for student $number."
            }
            Add-Grade $records $number ([int]$periodMatch.Groups[1].Value) ([int]$periodMatch.Groups[2].Value) (Field $record 'GPA') ([string](Field $record 'RecordSource'))
        }
    }

    if (Exists-Table $db 'STUDENT_TERM_GPA') {
        $previousCompact = Load-Rows $db 'STUDENT_TERM_GPA'
        foreach ($record in $previousCompact.Rows) {
            $number = [string](Field $record 'StudentNo')
            $year = [int](Field $record 'AcademicYear')
            for ($term = 1; $term -le 4; $term++) {
                Add-Grade $records $number $year $term (Field $record ('T' + $term)) ([string](Field $record 'DataOrigin'))
            }
        }
    }

    if ($records.Count -eq 0) {
        throw 'There are no valid academic records to consolidate. Original database unchanged.'
    }

    [void](Execute-Sql $db ('CREATE TABLE [STUDENTACADEMICRECORD_MIGRATING] (' +
        '[StudentNo] TEXT(40) NOT NULL, [AcademicYear] LONG NOT NULL,' +
        '[Term1] DOUBLE, [Term2] DOUBLE, [Term3] DOUBLE, [Term4] DOUBLE,' +
        '[RecordSource] TEXT(100),' +
        'CONSTRAINT [PK_STUDENT_TERMS_TEMP] PRIMARY KEY ([StudentNo],[AcademicYear]))'))

    foreach ($row in $records.Values) {
        $cmd = $db.CreateCommand()
        try {
            $cmd.CommandText = 'INSERT INTO [STUDENTACADEMICRECORD_MIGRATING] ' +
                '([StudentNo],[AcademicYear],[Term1],[Term2],[Term3],[Term4],[RecordSource]) ' +
                'VALUES (?,?,?,?,?,?,?)'
            [void]$cmd.Parameters.Add('@StudentNo', [Data.OleDb.OleDbType]::VarWChar)
            [void]$cmd.Parameters.Add('@Year', [Data.OleDb.OleDbType]::Integer)
            for ($term = 1; $term -le 4; $term++) {
                [void]$cmd.Parameters.Add('@Term' + $term, [Data.OleDb.OleDbType]::Double)
            }
            [void]$cmd.Parameters.Add('@Source', [Data.OleDb.OleDbType]::VarWChar)
            $values = @($row.StudentNo, $row.AcademicYear, $row.Term1, $row.Term2,
                $row.Term3, $row.Term4, $row.RecordSource)
            for ($i = 0; $i -lt 7; $i++) {
                $cmd.Parameters[$i].Value = if ($null -eq $values[$i]) {
                    [DBNull]::Value
                }
                else { $values[$i] }
            }
            if ($cmd.ExecuteNonQuery() -ne 1) {
                throw "Failed to save student $($row.StudentNo) to the new table."
            }
        }
        finally { $cmd.Dispose() }
    }

    $check = Load-Rows $db 'STUDENTACADEMICRECORD_MIGRATING'
    if ($check.Rows.Count -ne $records.Count) {
        throw 'Validation failed: the migrated student/year row count is incorrect.'
    }
    foreach ($row in $check.Rows) {
        $key = ([string]$row['StudentNo']).Trim().ToUpperInvariant() + '/' + [int]$row['AcademicYear']
        $expected = $records[$key]
        if ($null -eq $expected) { throw "Validation failed: unexpected row $key." }
        for ($term = 1; $term -le 4; $term++) {
            $name = 'Term' + $term
            $stored = Field $row $name
            $original = $expected[$name]
            if (($null -eq $stored) -ne ($null -eq $original)) {
                throw "Validation failed for $key / $name."
            }
            if ($null -ne $stored -and
                [math]::Abs([double]$stored - [double]$original) -gt 0.000001) {
                throw "Validation failed: GPA mismatch for $key / $name."
            }
        }
    }

    if ($oldTablePresent) {
        [void](Execute-Sql $db 'DROP TABLE [STUDENTACADEMICRECORD]')
    }
    [void](Execute-Sql $db ('CREATE TABLE [STUDENTACADEMICRECORD] (' +
        '[StudentNo] TEXT(40) NOT NULL, [AcademicYear] LONG NOT NULL,' +
        '[Term1] DOUBLE, [Term2] DOUBLE, [Term3] DOUBLE, [Term4] DOUBLE,' +
        '[RecordSource] TEXT(100),' +
        'CONSTRAINT [PK_STUDENT_ACADEMIC_YEAR] PRIMARY KEY ([StudentNo],[AcademicYear]))'))
    [void](Execute-Sql $db ('INSERT INTO [STUDENTACADEMICRECORD] ' +
        '([StudentNo],[AcademicYear],[Term1],[Term2],[Term3],[Term4],[RecordSource]) ' +
        'SELECT [StudentNo],[AcademicYear],[Term1],[Term2],[Term3],[Term4],[RecordSource] ' +
        'FROM [STUDENTACADEMICRECORD_MIGRATING]'))
    [void](Execute-Sql $db 'DROP TABLE [STUDENTACADEMICRECORD_MIGRATING]')
    if (Exists-Table $db 'STUDENT_TERM_GPA') {
        [void](Execute-Sql $db 'DROP TABLE [STUDENT_TERM_GPA]')
    }

    $final = Load-Rows $db 'STUDENTACADEMICRECORD'
    if ($final.Rows.Count -ne $records.Count) {
        throw 'Final GPA table validation failed. Original database unchanged.'
    }
    $db.Close()
    $db.Dispose()
    $db = $null

    [IO.File]::Replace($working, $dbPath, $null)
    Write-Host "DONE. STUDENTACADEMICRECORD now has $($records.Count) student/year rows with Term1, Term2, Term3 and Term4." -ForegroundColor Green
    Write-Host "Backed-up original: $backup" -ForegroundColor Green
    Write-Host 'Open Access again to see the consolidated table. Rebuild EduConnect from the latest integration branch.'
}
catch {
    Write-Host "Migration stopped: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Your original database is unchanged. Backup: $backup" -ForegroundColor Yellow
    throw
}
finally {
    if ($null -ne $db) { $db.Dispose() }
    if (Test-Path -LiteralPath $working) {
        Remove-Item -LiteralPath $working -Force -ErrorAction SilentlyContinue
    }
}
