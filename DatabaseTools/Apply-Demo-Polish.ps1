throw 'This older demo updater has been retired because it inserted DEMO ONLY into Support Type and created tutor-unmatchable requests. Use DatabaseTools/Apply-Admin-Repair.ps1 instead.'
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $root 'NextGen_#7.csproj'
$database = Join-Path $root 'Edu_Connect.accdb'
if (-not [System.IO.File]::Exists($project)) {
    throw "Run this script from the complete EduConnect project. Missing: $project"
}
if (-not [System.IO.File]::Exists($database)) {
    throw "The project database was not found: $database"
}
if ([System.IO.File]::Exists((Join-Path $root 'Edu_Connect.laccdb'))) {
    throw 'Close Access and EduConnect first.'
}

$revision = '7a591e49f826865a560f09abe80f32fb03f91bd8'
$files = @(
    'Reports.cs',
    'ManageBookings.cs',
    'ManageBookings.Designer.cs',
    'TutorSessions.cs',
    'DatabaseTools/Polish-Synthetic-Demo-Data.ps1'
)
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss_fff'
$folder = Join-Path $root "DatabaseBackups\DemoPolishSource_$stamp"
$staging = Join-Path $folder 'staging'
$originals = Join-Path $folder 'originals'
[System.IO.Directory]::CreateDirectory($staging) | Out-Null
[System.IO.Directory]::CreateDirectory($originals) | Out-Null

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -ne $git) {
    Push-Location $root
    try {
        $changed = @(& git status --porcelain -- Reports.cs ManageBookings.cs ManageBookings.Designer.cs TutorSessions.cs)
        if ($LASTEXITCODE -ne 0) { throw 'Unable to check the local working tree.' }
        if ($changed.Count -gt 0) {
            throw 'Local C# edits were detected. No files were replaced. Preserve or commit those edits before running this updater.'
        }
    }
    finally { Pop-Location }
}

foreach ($name in $files) {
    $uri = "https://raw.githubusercontent.com/Mpho-source/MileStone3-5/$revision/$name"
    $target = Join-Path $staging $name
    $targetDir = [System.IO.Path]::GetDirectoryName($target)
    [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null
    Invoke-WebRequest -UseBasicParsing -Uri $uri -OutFile $target
    if (-not [System.IO.File]::Exists($target) -or
        ([System.IO.FileInfo]$target).Length -eq 0) {
        throw "Download failed for $name. Project source and database remain unchanged."
    }
}
Write-Host 'All updated source files downloaded and checked.' -ForegroundColor Cyan

foreach ($name in $files) {
    $target = Join-Path $root $name
    $saved = Join-Path $originals $name
    $savedDir = [System.IO.Path]::GetDirectoryName($saved)
    [System.IO.Directory]::CreateDirectory($savedDir) | Out-Null
    if ([System.IO.File]::Exists($target)) {
        [System.IO.File]::Copy($target,$saved,$false)
    }
    $download = Join-Path $staging $name
    [System.IO.File]::Copy($download,$target,$true)
}
Write-Host "Previous source copies saved in: $originals" -ForegroundColor Green
Write-Host 'New line-chart labels, booking statuses and tutor attendance workflow installed.' -ForegroundColor Green

$population = Join-Path $root 'DatabaseTools\Polish-Synthetic-Demo-Data.ps1'
Write-Host 'Starting backup-first test-data update on the real project database...' -ForegroundColor Cyan
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $population
if ($LASTEXITCODE -ne 0) {
    throw "The test-data update reported an error. Your original database was not intentionally replaced. Source backups: $originals"
}

Write-Host ''
Write-Host 'SOURCE AND DATABASE POLISH COMPLETE' -ForegroundColor Green
Write-Host 'Rebuild EduConnect in Visual Studio, then open Reports > Report 3 > 2026.' -ForegroundColor Cyan
Write-Host "Source backups: $originals"
