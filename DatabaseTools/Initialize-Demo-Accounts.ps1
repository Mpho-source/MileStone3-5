$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$databasePath = Join-Path $root 'Edu_Connect.accdb'
$lockPath = Join-Path $root 'Edu_Connect.laccdb'
$backupFolder = Join-Path $root 'DatabaseBackups'

if (-not [System.IO.File]::Exists($databasePath)) {
    throw "Project database not found: $databasePath"
}
if ([System.IO.File]::Exists($lockPath)) {
    throw 'Close Access and EduConnect before running this script. The database is locked.'
}

Write-Host 'DEMO ONLY: all existing active staff, students and tutors will share the password you enter.' -ForegroundColor Yellow
Write-Host 'Do not use this on a live database containing real student records.' -ForegroundColor Yellow
$securePassword = Read-Host 'Enter the shared demo password' -AsSecureString
$pointer = [IntPtr]::Zero
$password = $null
try {
    $pointer = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $password = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
}
finally {
    if ($pointer -ne [IntPtr]::Zero) {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
    $securePassword.Dispose()
}

if ([string]::IsNullOrEmpty($password) -or $password.Length -lt 12 -or $password.Length -gt 128 -or
    $password -cnotmatch '[A-Z]' -or $password -cnotmatch '[a-z]' -or $password -notmatch '[0-9]') {
    throw 'Password must have 12-128 characters, uppercase, lowercase and a number.'
}

[System.IO.Directory]::CreateDirectory($backupFolder) | Out-Null
$backupPath = Join-Path $backupFolder (
    'Edu_Connect_before_demo_accounts_' +
    (Get-Date -Format 'yyyyMMdd_HHmmss_fff') + '.accdb')
[System.IO.File]::Copy($databasePath, $backupPath, $false)
Write-Host "Original database backed up to: $backupPath" -ForegroundColor Green

Add-Type -AssemblyName System.Data
$connection = [System.Data.OleDb.OleDbConnection]::new(
    "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$databasePath")
$transaction = $null
$iterations = 120000

function TestTable([string]$name) {
    foreach ($row in $connection.GetSchema('Tables').Rows) {
        if ([string]::Equals(
            [string]$row['TABLE_NAME'], $name,
            [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function GetColumns([string]$table) {
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = "SELECT * FROM [$table] WHERE 1=0"
        $reader = $command.ExecuteReader()
        try {
            $names = @()
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $names += $reader.GetName($i)
            }
            return $names
        }
        finally { $reader.Close() }
    }
    finally { $command.Dispose() }
}

function TestColumn([string]$table, [string]$name) {
    foreach ($actual in @(GetColumns $table)) {
        if ([string]::Equals(
            [string]$actual, $name,
            [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function ExecuteDDL([string]$sql) {
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = $sql
        [void]$command.ExecuteNonQuery()
    }
    finally { $command.Dispose() }
}

function EnsureTable([string]$name, [string]$ddl) {
    if (-not (TestTable $name)) {
        ExecuteDDL $ddl
        Write-Host "Created table: $name"
    }
}

function EnsureColumn([string]$table, [string]$column, [string]$type) {
    if (-not (TestColumn $table $column)) {
        ExecuteDDL "ALTER TABLE [$table] ADD COLUMN [$column] $type"
        Write-Host "Added column: $table.$column"
    }
}

function ReadKeys([string]$sql, [bool]$numeric) {
    $values = [System.Collections.ArrayList]::new()
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = $sql
        $reader = $command.ExecuteReader()
        try {
            while ($reader.Read()) {
                if ($reader.IsDBNull(0)) { continue }
                if ($numeric) {
                    $value = [int]$reader.GetValue(0)
                    if ($value -gt 0 -and -not $values.Contains($value)) {
                        [void]$values.Add($value)
                    }
                }
                else {
                    $value = ([string]$reader.GetValue(0)).Trim()
                    if ($value.Length -gt 0 -and -not $values.Contains($value)) {
                        [void]$values.Add($value)
                    }
                }
            }
        }
        finally { $reader.Close() }
    }
    finally { $command.Dispose() }
    return $values.ToArray()
}

function CountRows([string]$table) {
    $command = $connection.CreateCommand()
    try {
        $command.CommandText = "SELECT COUNT(*) FROM [$table]"
        return [int]$command.ExecuteScalar()
    }
    finally { $command.Dispose() }
}

function SetRolePasswords(
    [string]$table,
    [string]$keyColumn,
    [object[]]$keys,
    [bool]$numeric
) {
    $keyType = if ($numeric) {
        [System.Data.OleDb.OleDbType]::Integer
    }
    else {
        [System.Data.OleDb.OleDbType]::VarWChar
    }
    $total = 0
    foreach ($key in $keys) {
        $salt = New-Object byte[] 16
        $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($salt) }
        finally { $rng.Dispose() }

        $derive = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
            $password, $salt, $iterations)
        try { $hash = $derive.GetBytes(32) }
        finally { $derive.Dispose() }

        $saltValue = [Convert]::ToBase64String($salt)
        $hashValue = [Convert]::ToBase64String($hash)
        $existing = $connection.CreateCommand()
        try {
            $existing.Transaction = $transaction
            $existing.CommandText = "SELECT COUNT(*) FROM [$table] WHERE [$keyColumn]=?"
            [void]$existing.Parameters.Add('@key', $keyType)
            $existing.Parameters[0].Value = $key
            $found = [int]$existing.ExecuteScalar() -gt 0
        }
        finally { $existing.Dispose() }

        $command = $connection.CreateCommand()
        try {
            $command.Transaction = $transaction
            if ($found) {
                $command.CommandText =
                    "UPDATE [$table] SET [Salt]=?,[PasswordHash]=?,[Iterations]=? WHERE [$keyColumn]=?"
            }
            else {
                $command.CommandText =
                    "INSERT INTO [$table] ([Salt],[PasswordHash],[Iterations],[$keyColumn]) VALUES (?,?,?,?)"
            }
            [void]$command.Parameters.Add('@salt', [System.Data.OleDb.OleDbType]::VarWChar)
            [void]$command.Parameters.Add('@hash', [System.Data.OleDb.OleDbType]::VarWChar)
            [void]$command.Parameters.Add('@iterations', [System.Data.OleDb.OleDbType]::Integer)
            [void]$command.Parameters.Add('@key', $keyType)
            $command.Parameters[0].Value = $saltValue
            $command.Parameters[1].Value = $hashValue
            $command.Parameters[2].Value = $iterations
            $command.Parameters[3].Value = $key
            if ($command.ExecuteNonQuery() -ne 1) {
                throw "Could not update the $table credentials for an account."
            }
            $total++
        }
        finally { $command.Dispose() }
    }
    return $total
}

try {
    $connection.Open()
    foreach ($table in @('ADMIN', 'STUDENT', 'TUTOR', 'TUTORING_REQUEST', 'TUTORING_SESSION')) {
        if (-not (TestTable $table)) {
            throw "Missing required base table: $table. The backup is unchanged."
        }
    }
    foreach ($pair in @(
        @('ADMIN', 'AdminNo'),
        @('ADMIN', 'IsActive'),
        @('STUDENT', 'StudentNo'),
        @('TUTOR', 'TutorID')
    )) {
        if (-not (TestColumn $pair[0] $pair[1])) {
            $actualColumns = @(GetColumns $pair[0]) -join ', '
            throw "Missing required column: $($pair[0]).$($pair[1]). Actual columns: $actualColumns"
        }
    }

    $adminKeys = @(ReadKeys 'SELECT [AdminNo] FROM [ADMIN] WHERE [IsActive]=True' $false)
    $studentKeys = @(ReadKeys 'SELECT [StudentNo] FROM [STUDENT]' $false)
    $tutorKeys = @(ReadKeys 'SELECT [TutorID] FROM [TUTOR]' $true)

    if ($adminKeys.Count -eq 0) {
        throw 'No active ADMIN records are available. Existing records were not altered.'
    }

    $requestsBefore = CountRows 'TUTORING_REQUEST'
    $sessionsBefore = CountRows 'TUTORING_SESSION'

    EnsureTable 'ADMIN_LOGIN' (
        'CREATE TABLE [ADMIN_LOGIN] ([AdminNo] TEXT(50) PRIMARY KEY,' +
        ' [Salt] TEXT(64), [PasswordHash] TEXT(128), [Iterations] LONG)'
    )
    EnsureTable 'STUDENT_LOGIN' (
        'CREATE TABLE [STUDENT_LOGIN] ([StudentNo] TEXT(50) PRIMARY KEY,' +
        ' [Salt] TEXT(64), [PasswordHash] TEXT(128), [Iterations] LONG)'
    )
    EnsureTable 'TUTOR_LOGIN' (
        'CREATE TABLE [TUTOR_LOGIN] ([TutorID] LONG PRIMARY KEY,' +
        ' [Salt] TEXT(64), [PasswordHash] TEXT(128), [Iterations] LONG)'
    )
    EnsureTable 'STUDENTACADEMICRECORD' (
        'CREATE TABLE [STUDENTACADEMICRECORD] ([AcademicRecordID] COUNTER PRIMARY KEY,' +
        ' [StudentNo] TEXT(50), [GPA] DOUBLE, [AcademicPeriod] TEXT(20),' +
        ' [RecordDate] DATETIME)'
    )

    foreach ($table in @('ADMIN_LOGIN', 'STUDENT_LOGIN', 'TUTOR_LOGIN')) {
        EnsureColumn $table 'Salt' 'TEXT(64)'
        EnsureColumn $table 'PasswordHash' 'TEXT(128)'
        EnsureColumn $table 'Iterations' 'LONG'
    }
    EnsureColumn 'TUTORING_SESSION' 'AttendanceStatus' 'TEXT(20)'
    EnsureColumn 'TUTORING_SESSION' 'Feedback' 'TEXT(255)'
    EnsureColumn 'TUTORING_SESSION' 'SessionDate' 'DATETIME'

    foreach ($pair in @(
        @('ADMIN_LOGIN', 'AdminNo'),
        @('STUDENT_LOGIN', 'StudentNo'),
        @('TUTOR_LOGIN', 'TutorID')
    )) {
        if (-not (TestColumn $pair[0] $pair[1])) {
            throw "Invalid existing login table: $($pair[0])"
        }
    }

    $transaction = $connection.BeginTransaction()
    try {
        $adminUpdated = SetRolePasswords 'ADMIN_LOGIN' 'AdminNo' $adminKeys $false
        $studentUpdated = SetRolePasswords 'STUDENT_LOGIN' 'StudentNo' $studentKeys $false
        $tutorUpdated = SetRolePasswords 'TUTOR_LOGIN' 'TutorID' $tutorKeys $true
        $transaction.Commit()
        $transaction = $null
    }
    catch {
        if ($null -ne $transaction) {
            $transaction.Rollback()
            $transaction = $null
        }
        throw
    }

    if ((CountRows 'TUTORING_REQUEST') -ne $requestsBefore -or
        (CountRows 'TUTORING_SESSION') -ne $sessionsBefore) {
        throw 'The number of requests or sessions unexpectedly changed. Inspect the original backup.'
    }

    Write-Host ''
    Write-Host 'DEMO ACCOUNTS READY' -ForegroundColor Green
    Write-Host "Administrators configured: $adminUpdated"
    Write-Host "Students configured: $studentUpdated"
    Write-Host "Tutors configured: $tutorUpdated"
    Write-Host "Tutoring requests preserved: $requestsBefore"
    Write-Host "Tutoring sessions preserved: $sessionsBefore"
    Write-Host "Original backup: $backupPath"
    Write-Host 'Open Visual Studio and press F5. Select your role and sign in with the shared password.' -ForegroundColor Cyan
    Write-Host 'Missing historical GPA records were not fabricated.' -ForegroundColor Yellow
}
catch {
    Write-Host "Setup failed. Original backup: $backupPath" -ForegroundColor Red
    throw
}
finally {
    if ($null -ne $transaction) {
        $transaction.Rollback()
    }
    $connection.Dispose()
    $password = $null
}
