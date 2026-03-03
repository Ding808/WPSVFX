#Requires -Version 5.1
<#
.SYNOPSIS
    Compile PowerFx.Helper C# project and output to helper/bin/

.DESCRIPTION
    Uses dotnet publish to generate a Windows x64 self-contained single-file executable.
    Output: <repo_root>/helper/bin/PowerFx.Helper.exe

.EXAMPLE
    .\scripts\build-helper.ps1
    .\scripts\build-helper.ps1 -Configuration Debug
#>

param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [switch]$NoPause
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot  = Split-Path -Parent $PSScriptRoot
$projPath  = Join-Path $repoRoot "helper\src\PowerFx.Helper\PowerFx.Helper.csproj"
$outputDir = Join-Path $repoRoot "helper\bin"

Write-Host ""
Write-Host "=== wt-powerfx: Building Helper ===" -ForegroundColor Cyan
Write-Host "  Project    : $projPath"
Write-Host "  Output     : $outputDir"
Write-Host "  Config     : $Configuration"
Write-Host ""

# Check dotnet SDK
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

$dotnetVersion = & dotnet --version
Write-Host "  .NET SDK   : $dotnetVersion" -ForegroundColor Gray

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

# Kill any running helper instance so the output exe is not locked
$running = Get-Process -Name "PowerFx.Helper" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "  Stopping running PowerFx.Helper (PID $($running.Id))..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Milliseconds 800
}

# Run dotnet publish
$publishArgs = @(
    "publish"
    $projPath
    "--configuration", $Configuration
    "--runtime", "win-x64"
    "--self-contained", "true"
    "-p:PublishSingleFile=true"
    "-p:PublishReadyToRun=true"
    "--output", $outputDir
    "--nologo"
)

Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
Write-Host ""

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code: $LASTEXITCODE"
    exit $LASTEXITCODE
}

$exePath = Join-Path $outputDir "PowerFx.Helper.exe"
if (Test-Path $exePath) {
    $size = (Get-Item $exePath).Length / 1MB
    Write-Host ""
    Write-Host "Build succeeded!" -ForegroundColor Green
    Write-Host "  Output: $exePath" -ForegroundColor Green
    Write-Host "  Size  : $([math]::Round($size, 1)) MB" -ForegroundColor Gray
} else {
    Write-Error "Expected output not found: $exePath"
    exit 1
}

Write-Host ""
Write-Host "Tip: Run 'wt-powerfx start' to start the helper, or 'wt-powerfx install' for full install."
Write-Host ""

if (-not $NoPause) {
    Write-Host "Press any key to exit..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}