<#
.SYNOPSIS
    Publishes Emergency Passport Tracker and builds the installer.

.DESCRIPTION
    Two steps:
      1. dotnet publish, self-contained win-x64, into bin\publish\win-x64
      2. Inno Setup compiles Installer\EmergencyPassportTracker.iss into Installer\Output

    The version comes from <Version> in the .csproj, so bump it there and everything
    downstream follows.

.PARAMETER SkipPublish
    Reuse whatever is already in bin\publish\win-x64 and only rebuild the installer.

.PARAMETER Version
    Override the version instead of reading it from the .csproj.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
#>

[CmdletBinding()]
param(
    [switch]$SkipPublish,
    [string]$Version
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root       = $PSScriptRoot
$project    = Join-Path $root 'Emergency Passport Tracker.csproj'
$issScript  = Join-Path $root 'Installer\EmergencyPassportTracker.iss'
$publishDir = Join-Path $root 'bin\publish\win-x64'
$outputDir  = Join-Path $root 'Installer\Output'
$exeName    = 'Emergency Passport Tracker.exe'

function Fail($message) {
    Write-Host ''
    Write-Host "  $message" -ForegroundColor Red
    Write-Host ''
    exit 1
}

if (-not (Test-Path $project)) {
    Fail "Project not found: $project"
}

# ---------------------------------------------------------------- version

if (-not $Version) {
    # Read <Version> straight out of the project file. Deliberately not via the XML DOM:
    # PropertyGroup is a collection, and member access across it trips Set-StrictMode.
    $csprojText = Get-Content $project -Raw
    $match = [regex]::Match($csprojText, '<Version>\s*([0-9]+(?:\.[0-9]+){1,3})\s*</Version>')

    if (-not $match.Success) {
        Fail "No <Version> found in the .csproj. Add one, or pass -Version 1.2.3."
    }

    $Version = $match.Groups[1].Value
}

$Version = $Version.Trim()

if ($Version -notmatch '^\d+(\.\d+){1,3}$') {
    Fail "Version '$Version' is not in the form 1.2.3."
}

Write-Host ""
Write-Host "Emergency Passport Tracker - installer build" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host ""

# ---------------------------------------------------------------- publish

if ($SkipPublish) {
    Write-Host "Skipping publish (-SkipPublish)." -ForegroundColor Yellow
}
else {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Fail "The .NET SDK ('dotnet') is not on PATH. Install the .NET 10 SDK."
    }

    if (Test-Path $publishDir) {
        # A stale publish folder is the classic way to ship a file that should be gone.
        Remove-Item $publishDir -Recurse -Force
    }

    Write-Host "Publishing self-contained win-x64..." -ForegroundColor Cyan

    & dotnet publish $project `
        -p:PublishProfile=Installer-win-x64 `
        -p:Version=$Version `
        --nologo

    if ($LASTEXITCODE -ne 0) {
        Fail "dotnet publish failed."
    }
}

$publishedExe = Join-Path $publishDir $exeName

if (-not (Test-Path $publishedExe)) {
    Fail "Publish output is missing $exeName. Looked in: $publishDir"
}

# Sanity check: a self-contained build must carry the runtime and its config alongside
# the exe. If these are absent the installer would produce an app that cannot start -
# which is exactly how the old EPT_Installer package failed.
foreach ($required in @(
    'Emergency Passport Tracker.dll',
    'Emergency Passport Tracker.runtimeconfig.json',
    'hostfxr.dll',
    'System.Windows.Forms.dll',
    'itext.kernel.dll')) {

    if (-not (Test-Path (Join-Path $publishDir $required))) {
        Fail "Publish output looks incomplete - '$required' is missing from $publishDir"
    }
}

$fileCount = (Get-ChildItem $publishDir -Recurse -File).Count
$sizeMb    = [math]::Round(((Get-ChildItem $publishDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 1)

Write-Host "Published $fileCount files, $sizeMb MB." -ForegroundColor Green

# ------------------------------------------------------------- inno setup

$iscc = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source

if (-not $iscc) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe"
    )

    $iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $iscc) {
    Fail @"
Inno Setup was not found.

Install Inno Setup 6.3 or later from https://jrsoftware.org/isdl.php
then run this script again.
"@
}

Write-Host "Compiling installer with $iscc" -ForegroundColor Cyan

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

& $iscc "/DMyAppVersion=$Version" $issScript

if ($LASTEXITCODE -ne 0) {
    Fail "Inno Setup failed."
}

$setup = Join-Path $outputDir "EmergencyPassportTracker-Setup-$Version.exe"

if (-not (Test-Path $setup)) {
    Fail "Inno Setup reported success but $setup is missing."
}

$setupMb = [math]::Round(((Get-Item $setup).Length / 1MB), 1)

Write-Host ""
Write-Host "Installer built: $setup ($setupMb MB)" -ForegroundColor Green
Write-Host ""
Write-Host "It installs per-user and needs no administrator rights." -ForegroundColor Gray
Write-Host "The .NET 10 runtime is included, so the target PC needs nothing installed first." -ForegroundColor Gray
Write-Host ""
