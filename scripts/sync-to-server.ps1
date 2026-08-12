# =============================================================================
# 发布包同步到 WebAPI 服务器（US-SHELL-010: 更新源挂载）
# 用法: pwsh scripts/sync-to-server.ps1 -Server <host> -RemotePath "C:\Services\LYBT-releases"
# 依赖: ssh/scp（OpenSSH——Windows 10+ 内置）或 SMB 网络路径
# =============================================================================
param(
    [Parameter(Mandatory = $true)][string]$Server,          # 如 webadmin@192.168.1.10 或 \\server\share
    [Parameter(Mandatory = $true)][string]$RemotePath,      # 服务器 releases 目录（= DesktopUpdate:ReleasesPath）
    [string]$ReleaseDir = "releases",
    [string]$SshPort = "22"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$local = Join-Path $root $ReleaseDir

if (-not (Test-Path $local)) { throw "本地 releases 目录不存在: $local（先运行 velopack-pack.ps1）" }

Write-Host "==> 同步 $local -> $Server`:$RemotePath"

# SMB 路径（\\server\share）——直接 Copy-Item
if ($Server -match '^\\\\') {
    New-Item -ItemType Directory -Force -Path $RemotePath | Out-Null
    Copy-Item (Join-Path $local "*") $RemotePath -Force -Recurse
    Write-Host "==> SMB 同步完成: $RemotePath"
    exit 0
}

# SSH/SCP——创建远端目录后逐个上传（Setup.exe 大文件走 scp）
$remoteDir = $RemotePath -replace '\\', '/'
ssh -p $SshPort $Server "mkdir -p '$remoteDir'" 2>$null
if ($LASTEXITCODE -ne 0) { throw "SSH 连接失败: $Server" }

$files = Get-ChildItem $local
foreach ($f in $files) {
    Write-Host "    upload $($f.Name)..."
    scp -P $SshPort $f.FullName "$Server`:$remoteDir/$($f.Name)"
    if ($LASTEXITCODE -ne 0) { throw "上传失败: $($f.Name)" }
}

Write-Host "==> 同步完成（$($files.Count) 个文件）——服务器 /releases/ 静态服务 + GET / 下载页即时生效"
