#Requires -Version 5.1
<#
.SYNOPSIS
    打包 wt-powerfx npm 包（含 helper exe），准备 npm publish。

.DESCRIPTION
    1. 编译 helper（如果 helper/bin/PowerFx.Helper.exe 不存在）
    2. 编译 CLI TypeScript
    3. 检查 assets/audio/ 中的 WAV 文件（可选，发出警告）
    4. 显示打包摘要

.EXAMPLE
    .\scripts\publish-helper.ps1 -DryRun
    .\scripts\publish-helper.ps1
#>

param(
    [switch]$DryRun,
    [switch]$SkipBuild,
    [switch]$NoPause
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot   = Split-Path -Parent $PSScriptRoot
$helperExe  = Join-Path $repoRoot "helper\bin\PowerFx.Helper.exe"
$cliDistDir = Join-Path $repoRoot "cli\dist"
$audioDir   = Join-Path $repoRoot "assets\audio"

Write-Host ""
Write-Host "=== wt-powerfx: Publish Preparation ===" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: 编译 helper ──────────────────────────────────────────────────────
if (-not $SkipBuild) {
    if (-not (Test-Path $helperExe)) {
        Write-Host "[1/3] helper.exe 不存在，开始编译..." -ForegroundColor Yellow
        & "$PSScriptRoot\build-helper.ps1" -NoPause
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    } else {
        Write-Host "[1/3] helper.exe 已存在，跳过编译（使用 -SkipBuild:$false 强制重编）" -ForegroundColor Gray
    }
} else {
    Write-Host "[1/3] 已跳过 helper 编译（-SkipBuild）" -ForegroundColor Gray
}

# ── Step 2: 编译 CLI ──────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[2/3] 编译 CLI TypeScript..." -ForegroundColor Yellow

Push-Location (Join-Path $repoRoot "cli")
try {
    & npm install --silent
    & npm run build
    if ($LASTEXITCODE -ne 0) { throw "CLI 编译失败，退出码: $LASTEXITCODE" }
} finally {
    Pop-Location
}

Write-Host "  ✔ CLI 编译完成 → $cliDistDir" -ForegroundColor Green

# ── Step 3: 检查音频文件 ───────────────────────────────────────────────────────
Write-Host ""
Write-Host "[3/3] 检查 assets/audio/ ..." -ForegroundColor Yellow

$requiredAudio = @("key.wav", "backspace.wav", "delete.wav", "select.wav")
$missingAudio  = @()

foreach ($f in $requiredAudio) {
    $path = Join-Path $audioDir $f
    if (Test-Path $path) {
        Write-Host "  ✔ $f" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ $f 缺失（用户安装时将静音运行）" -ForegroundColor Yellow
        $missingAudio += $f
    }
}

# ── 摘要 ──────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host "打包摘要" -ForegroundColor Cyan

if (Test-Path $helperExe) {
    $sz = [math]::Round((Get-Item $helperExe).Length / 1MB, 1)
    Write-Host "  helper.exe : $sz MB" -ForegroundColor Gray
}

if ($missingAudio.Count -gt 0) {
    Write-Host ""
    Write-Host "  ⚠ 以下音频文件缺失，npm 包将不包含音效：" -ForegroundColor Yellow
    $missingAudio | ForEach-Object { Write-Host "    - $_" -ForegroundColor Yellow }
    Write-Host "  参考 assets/audio/README.md 获取免版权音效。" -ForegroundColor Yellow
}

Write-Host ""

if ($DryRun) {
    Write-Host "DryRun 模式：跳过 npm publish" -ForegroundColor Cyan
} else {
    Write-Host "执行 npm publish ..." -ForegroundColor Cyan
    Push-Location (Join-Path $repoRoot "cli")
    try {
        & npm publish --access public
        if ($LASTEXITCODE -ne 0) { throw "npm publish 失败" }
        Write-Host "✔ 发布成功！" -ForegroundColor Green
    } finally {
        Pop-Location
    }
}

Write-Host ""

if (-not $NoPause) {
    Write-Host "按任意键退出..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
