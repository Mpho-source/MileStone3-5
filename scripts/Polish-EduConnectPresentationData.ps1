param(
    [string]$DatabasePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Edu_Connect.accdb'),
    [int]$AcademicYear = 2026,
    [switch]$Apply
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Data
$DatabasePath=[IO.Path]::GetFullPath($DatabasePath)
if (-not (Test-Path -LiteralPath $DatabasePath)) {throw "Database not found: $DatabasePath"}
if (Test-Path ([IO.Path]::ChangeExtension($DatabasePath,'.laccdb'))) {throw 'Close Microsoft Access and EduConnect first.'}
$queryName="Student_GPA_Terms_$AcademicYear"
$probe=New-Object System.Data.OleDb.OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$DatabasePath")
try {
    $probe.Open()
    $command=$probe.CreateCommand()
    $command.CommandText="SELECT * FROM [STUDENTACADEMICRECORD] WHERE 1=0"
    $reader=$command.ExecuteReader([System.Data.CommandBehavior]::SchemaOnly)
    try {
        $columns=@($reader.GetSchemaTable().Rows | ForEach-Object { [string]$_['ColumnName'] })
        $compact=($columns -contains 'AcademicYear') -and
            ($columns -contains 'T1') -and ($columns -contains 'T2') -and
            ($columns -contains 'T3') -and ($columns -contains 'T4')
    }
    finally { $reader.Dispose(); $command.Dispose() }
}
finally { $probe.Dispose() }
if ($compact) {
    $sql="SELECT [StudentNo],[T1],[T2],[T3],[T4] FROM [STUDENTACADEMICRECORD] WHERE [AcademicYear]=$AcademicYear ORDER BY [StudentNo]"
}
else {
    $fields=@(1..4 | ForEach-Object { "Max(IIf([AcademicPeriod]='$AcademicYear-T$_',[GPA],Null)) AS [T$_]" })
    $sql="SELECT [StudentNo], "+($fields -join ', ')+" FROM [STUDENTACADEMICRECORD] WHERE [AcademicPeriod] Like '$AcademicYear-T*' GROUP BY [StudentNo] ORDER BY [StudentNo]"
}
Write-Host 'Presentation-ready saved query:' $queryName
Write-Host 'Fields: StudentNo, T1, T2, T3, T4'
Write-Host 'The original 40 academic records remain unchanged.'
Write-Host 'Corrects only two exact sample-feedback strings if they exist.'
if (-not $Apply) {Write-Host 'PREVIEW ONLY. Use -Apply to create GPA query and clean sample feedback.';return}
$backup="$DatabasePath.presentation-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Copy-Item -LiteralPath $DatabasePath -Destination $backup -ErrorAction Stop
Write-Host "Backup: $backup"
$engine=$null;$database=$null
try {
    $engine=New-Object -ComObject DAO.DBEngine.120
    $database=$engine.OpenDatabase($DatabasePath)
    $found=$false
    foreach($q in $database.QueryDefs){if($q.Name -eq $queryName){$found=$true;break}}
    if($found){$database.QueryDefs.Delete($queryName)}
    [void]$database.CreateQueryDef($queryName,$sql)
    Write-Host "GPA query created: $queryName"
}
finally {
    if($null -ne $database){$database.Close();[void][Runtime.InteropServices.Marshal]::ReleaseComObject($database)}
    if($null -ne $engine){[void][Runtime.InteropServices.Marshal]::ReleaseComObject($engine)}
}
$db=$null
try {
    $db=New-Object System.Data.OleDb.OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$DatabasePath")
    $db.Open()
    $tx=$db.BeginTransaction()
    try {
        $replacements=@(
            @('EDUCONNECT SYNTHETIC TEST: reviewed algorithms and solved practice questions.','Reviewed algorithms and worked through practice questions.'),
            @('EDUCONNECT SYNTHETIC TEST: student absent from scheduled session.','Student did not attend the scheduled session.')
        )
        foreach($pair in $replacements) {
            $cmd=$db.CreateCommand();$cmd.Transaction=$tx
            $cmd.CommandText='UPDATE [TUTORING_SESSION] SET [Feedback]=? WHERE [Feedback]=?'
            $p=$cmd.CreateParameter();$p.Value=$pair[1];[void]$cmd.Parameters.Add($p)
            $p=$cmd.CreateParameter();$p.Value=$pair[0];[void]$cmd.Parameters.Add($p)
            $n=$cmd.ExecuteNonQuery();$cmd.Dispose()
            Write-Host "Feedback records reformatted: $n"
        }
        $tx.Commit()
    } catch {try{$tx.Rollback()}catch{};throw} finally{$tx.Dispose()}
}
finally {if($null -ne $db){$db.Dispose()}}
Write-Host "SUCCESS: GPA presentation query saved; sample feedback made readable. Academic and login records preserved."
