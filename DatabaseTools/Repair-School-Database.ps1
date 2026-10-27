$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$databasePath = Join-Path $root 'Edu_Connect.accdb'
$lockPath = Join-Path $root 'Edu_Connect.laccdb'
$backupFolder = Join-Path $root 'DatabaseBackups'
$applicationPath = Join-Path $root 'bin\Debug\NextGen_#7.exe'

if (-not [System.IO.File]::Exists($databasePath)) {
    throw "The project database was not found: $databasePath"
}
if ([System.IO.File]::Exists($lockPath)) {
    throw 'The database is currently open. Close Microsoft Access and EduConnect before repairing it.'
}

[System.IO.Directory]::CreateDirectory($backupFolder) | Out-Null
$backupPath = Join-Path $backupFolder (
    'Edu_Connect_before_schema_repair_' +
    (Get-Date -Format 'yyyyMMdd_HHmmss_fff') + '.accdb')
[System.IO.File]::Copy($databasePath, $backupPath, $false)
Write-Host "Backup saved: $backupPath" -ForegroundColor Cyan

$connection = [System.Data.OleDb.OleDbConnection]::new(
    "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$databasePath")
$changes = [System.Collections.Generic.List[string]]::new()
$needsSetup = $false
$activeAdmins = [System.Collections.Generic.List[string]]::new()

function TestTable([string]$tableName) {
    foreach ($row in $connection.GetSchema('Tables').Rows) {
        if ([string]::Equals(
            [string]$row['TABLE_NAME'], $tableName,
            [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function TestColumn([string]$tableName, [string]$columnName) {
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = "SELECT * FROM [$tableName] WHERE 1=0"
        $reader = $command.ExecuteReader([System.Data.CommandBehavior]::SchemaOnly)
        try {
            foreach ($row in $reader.GetSchemaTable().Rows) {
                if ([string]::Equals(
                    ([string]$row['ColumnName']).Trim([char[]]@([char]0xFEFF, [char]' ')),
                    $columnName,
                    [System.StringComparison]::OrdinalIgnoreCase)) {
                    return $true
                }
            }
        }
        finally { $reader.Close() }
    }
    finally { $command.Dispose() }
    return $false
}

function ExecuteSql([string]$sql) {
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = $sql
        [void]$command.ExecuteNonQuery()
    }
    finally { $command.Dispose() }
}

function EnsureTable([string]$table, [string]$ddl) {
    if (-not (TestTable $table)) {
        ExecuteSql $ddl
        $changes.Add("Created table $table")
    }
}

function EnsureColumn([string]$table, [string]$column, [string]$type) {
    if (-not (TestColumn $table $column)) {
        ExecuteSql "ALTER TABLE [$table] ADD COLUMN [$column] $type"
        $changes.Add("Added column $table.$column")
    }
}

try {
    $connection.Open()

    foreach ($required in @(
        'ADMIN', 'STUDENT', 'TUTOR', 'TUTOR_COURSE',
        'TUTORING_REQUEST', 'TUTORING_SESSION'
    )) {
        if (-not (TestTable $required)) {
            throw "The underlying table $required is missing. No repair was attempted."
        }
    }

    EnsureTable 'ADMIN_LOGIN' (
        'CREATE TABLE [ADMIN_LOGIN] ' +
        '([AdminNo] TEXT(50) PRIMARY KEY, [Salt] TEXT(64), ' +
        '[PasswordHash] TEXT(128), [Iterations] LONG)'
    )
    EnsureTable 'STUDENT_LOGIN' (
        'CREATE TABLE [STUDENT_LOGIN] ' +
        '([StudentNo] TEXT(50) PRIMARY KEY, [Salt] TEXT(64), ' +
        '[PasswordHash] TEXT(128), [Iterations] LONG)'
    )
    EnsureTable 'TUTOR_LOGIN' (
        'CREATE TABLE [TUTOR_LOGIN] ' +
        '([TutorID] LONG PRIMARY KEY, [Salt] TEXT(64), ' +
        '[PasswordHash] TEXT(128), [Iterations] LONG)'
    )
    EnsureTable 'STUDENTACADEMICRECORD' (
        'CREATE TABLE [STUDENTACADEMICRECORD] ' +
        '([AcademicRecordID] COUNTER PRIMARY KEY, ' +
        '[StudentNo] TEXT(50), [GPA] DOUBLE, ' +
        '[AcademicPeriod] TEXT(20), [RecordDate] DATETIME)'
    )

    foreach ($login in @('ADMIN_LOGIN', 'STUDENT_LOGIN', 'TUTOR_LOGIN')) {
        EnsureColumn $login 'Salt' 'TEXT(64)'
        EnsureColumn $login 'PasswordHash' 'TEXT(128)'
        EnsureColumn $login 'Iterations' 'LONG'
    }

    EnsureColumn 'TUTORING_SESSION' 'AttendanceStatus' 'TEXT(20)'
    EnsureColumn 'TUTORING_SESSION' 'Feedback' 'TEXT(255)'
    EnsureColumn 'TUTORING_SESSION' 'SessionDate' 'DATETIME'

    foreach ($check in @(
        @('ADMIN_LOGIN', 'AdminNo'),
        @('ADMIN_LOGIN', 'PasswordHash'),
        @('STUDENT_LOGIN', 'StudentNo'),
        @('TUTOR_LOGIN', 'TutorID'),
        @('STUDENTACADEMICRECORD', 'StudentNo'),
        @('STUDENTACADEMICRECORD', 'GPA'),
        @('STUDENTACADEMICRECORD', 'AcademicPeriod'),
        @('TUTORING_SESSION', 'AttendanceStatus'),
        @('TUTORING_SESSION', 'Feedback'),
        @('TUTORING_SESSION', 'SessionDate')
    )) {
        if (-not (TestColumn $check[0] $check[1])) {
            throw "Required schema element is missing: $($check[0]).$($check[1])"
        }
    }

    $command = $connection.CreateCommand()
    try {
        $command.CommandText = 'SELECT COUNT(*) FROM [ADMIN_LOGIN]'
        $needsSetup = [int]$command.ExecuteScalar() -eq 0
    }
    finally { $command.Dispose() }

    $command = $connection.CreateCommand()
    try {
        $command.CommandText =
            'SELECT [AdminNo] FROM [ADMIN] WHERE [IsActive]=True ORDER BY [AdminNo]'
        $reader = $command.ExecuteReader()
        try {
            while ($reader.Read()) {
                if (-not $reader.IsDBNull(0)) {
                    $activeAdmins.Add([string]$reader.GetValue(0))
                }
            }
        }
        finally { $reader.Close() }
    }
    finally { $command.Dispose() }
}
catch {
    Write-Host 'Repair could not finish. The pre-repair backup has been preserved.' -ForegroundColor Red
    Write-Host "Backup: $backupPath" -ForegroundColor Yellow
    throw
}
finally { $connection.Dispose() }

Write-Host ''
Write-Host "Schema repair completed for: $databasePath" -ForegroundColor Green
if ($changes.Count -eq 0) {
    Write-Host 'All required tables and columns were already present.' -ForegroundColor Cyan
}
else {
    foreach ($change in $changes) {
        Write-Host " - $change" -ForegroundColor Green
    }
}
Write-Host 'Your existing students, tutors, tutoring requests and sessions were not reset.' -ForegroundColor Green
Write-Host 'The repair does not recreate unavailable GPA values or account passwords.' -ForegroundColor Yellow

if (-not $needsSetup) {
    Write-Host 'ADMIN_LOGIN already contains an account. Initial Administrator Setup is not required.' -ForegroundColor Green
    exit 0
}

if ($activeAdmins.Count -eq 0) {
    Write-Host 'No active administrator was found in ADMIN. Create or activate an authorised admin record before setting a password.' -ForegroundColor Yellow
    exit 0
}

Write-Host ''
Write-Host 'Available active administrator numbers:' -ForegroundColor Cyan
foreach ($admin in $activeAdmins) { Write-Host " - $admin" }
Write-Host ''
Write-Host 'Administrator setup must now be completed once for this database.' -ForegroundColor Yellow

$generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $bytes = New-Object byte[] 24
    $generator.GetBytes($bytes)
    $setupKey = [Convert]::ToBase64String($bytes)
}
finally { $generator.Dispose() }

if (-not [System.IO.File]::Exists($applicationPath)) {
    Write-Host 'Build the solution in Visual Studio, then run this script again to launch setup.' -ForegroundColor Yellow
    exit 0
}

Write-Host ''
Write-Host 'ONE-TIME SETUP KEY (enter this in Initial Administrator Setup):' -ForegroundColor Cyan
Write-Host $setupKey -ForegroundColor Yellow
Write-Host 'Keep this key private. It is valid only while the application opened by this script runs.' -ForegroundColor Yellow
Write-Host 'Choose Staff and one of the existing active administrator numbers above.' -ForegroundColor Cyan
Write-Host 'Create a personal password with at least 12 characters (uppercase, lowercase and digit).' -ForegroundColor Cyan

$env:EDUCONNECT_ADMIN_SETUP_KEY = $setupKey
try {
    Start-Process -FilePath $applicationPath -WorkingDirectory (Split-Path $applicationPath -Parent) -Wait
}
finally {
    Remove-Item Env:\EDUCONNECT_ADMIN_SETUP_KEY -ErrorAction SilentlyContinue
    $setupKey = $null
}
