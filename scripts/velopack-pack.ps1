# =============================================================================
# Velopack 打包脚本（US-SHELL-010: Setup.exe + Velopack 更新源）
# 用法: pwsh scripts/velopack-pack.ps1 -Version 1.2.0 [-Runtime win-x64]
# 前置: dotnet tool install -g vpk (Velopack CLI)
# 产物: releases/Setup.exe + releases/RELEASES + releases/*.nupkg（更新源）
# =============================================================================
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Runtime = "win-x64",
    [string]$ShellProject = "src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj",
    [string]$ReleaseDir = "releases"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# 1. 校验版本（SemVer——Velopack 需要）
if ($Version -notmatch '^\d+\.\d+\.\d+') {
    throw "版本号需为 SemVer 格式（如 1.2.0）: $Version"
}

# 2. dotnet publish（自包含单文件——免 .NET Runtime）
$publishDir = "artifacts/publish/$Runtime"
Write-Host "==> dotnet publish ($Runtime, self-contained single-file)..."
dotnet publish $ShellProject -c Release -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败" }

# 3. vpk pack（生成 Setup.exe + RELEASES + nupkg）
Write-Host "==> vpk pack (Velopack $Version)..."
New-Item -ItemType Directory -Force -Path $ReleaseDir | Out-Null
vpk pack --packId "lybt-desktop" --packVersion $Version `
    --packDir $publishDir --mainExe "LYBT.Desktop.Shell.exe" `
    --releaseDir $ReleaseDir --framework "net8.0" --runtime "$Runtime" `
    --channel "stable"
if ($LASTEXITCODE -ne 0) { throw "vpk pack 失败" }

# 4. 产物清单
Write-Host "==> 打包完成，产物:"
Get-ChildItem $ReleaseDir | ForEach-Object { Write-Host "    $($_.Name) ($([math]::Round($_.Length/1MB,1)) MB)" }
Write-Host "==> 下一步: ./scripts/sync-to-server.ps1 -Server <host> -RemotePath <releases-path>"
