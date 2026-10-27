$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $root 'NextGen_#7.csproj'
$database = Join-Path $root 'Edu_Connect.accdb'
$source = Join-Path $root 'AdminRequests.cs'
$lock = Join-Path $root 'Edu_Connect.laccdb'

if (-not [System.IO.File]::Exists($project) -or -not [System.IO.File]::Exists($database)) {
    throw 'The complete EduConnect project and its database must be present in this folder.'
}
if ([System.IO.File]::Exists($lock)) {
    throw 'Close Microsoft Access and EduConnect before applying the repair.'
}

$revision = 'e5b0686928ecd0651522ba7329f2794524228ee4'
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss_fff'
$folder = Join-Path $root "DatabaseBackups\AdminRepairSource_$stamp"
[System.IO.Directory]::CreateDirectory($folder) | Out-Null

$files = @(
    @{Name='AdminRequests.cs';Destination=$source},
    @{Name='DatabaseTools/Repair-Admin-Demo-Bookings.ps1';Destination=(Join-Path $root 'DatabaseTools\Repair-Admin-Demo-Bookings.ps1')}
)
$downloads = @()
foreach ($item in $files) {
    $uri = "https://raw.githubusercontent.com/Mpho-source/MileStone3-5/$revision/$($item.Name)"
    $download = Join-Path $folder ([System.IO.Path]::GetFileName($item.Name))
    Invoke-WebRequest -UseBasicParsing -Uri $uri -OutFile $download
    if (-not [System.IO.File]::Exists($download) -or
        ([System.IO.FileInfo]$download).Length -lt 1000) {
        throw "Download was incomplete: $($item.Name). No project files were changed."
    }
    $downloads += [pscustomobject]@{Name=$item.Name;Path=$download;Destination=$item.Destination}
}

$localSourceBackup = Join-Path $folder 'AdminRequests_before_repair.cs'
if ([System.IO.File]::Exists($source)) {
    [System.IO.File]::Copy($source,$localSourceBackup,$false)
}

$repair = @($downloads | Where-Object { $_.Name -like '*.ps1' })[0]
Write-Host 'Running repair on a separate Access database copy...' -ForegroundColor Cyan
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $repair.Path -ProjectRoot $root
if ($LASTEXITCODE -ne 0) {
    throw 'Database repair did not complete. The source file was not replaced. Keep the Access backup.'
}

$updatedSource = @($downloads | Where-Object { $_.Name -eq 'AdminRequests.cs' })[0]
[System.IO.File]::Copy($updatedSource.Path,$source,$true)
Write-Host ''
Write-Host 'ADMIN REQUEST MATCHING REPAIR APPLIED' -ForegroundColor Green
Write-Host "Updated source file: $source"
Write-Host "Previous source backed up: $localSourceBackup"
Write-Host 'Rebuild the application in Visual Studio. Select a Verified request to see eligible tutors.' -ForegroundColor Cyan
