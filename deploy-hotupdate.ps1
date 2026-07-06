# LYBT WebAPI 自更新脚本
# 开发机运行：构建 → 上传ZIP → 触发服务器自更新
#
# 前置: 目标服务器 WebAPI 已运行 (任何版本)
# 原理: POST ZIP 到 /api/deploy/upload → 服务器保存到临时目录 → POST /api/deploy/restart → 服务器自动替换并重启

param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [int]$Port = 5000,
    
    [string]$Password = "SysAdmin@2026!"
)

$ErrorActionPreference = "Stop"
$baseUrl = "http://${Server}:${Port}"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  LYBT WebAPI 热更新" -ForegroundColor Cyan
Write-Host "  目标: $baseUrl" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# ── Step 1: 本地构建 ──
Write-Host "`n[1/4] 构建 Release..." -ForegroundColor Yellow
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj -c Release -o "$env:TEMP\lybt-hotupdate" --self-contained false -v quiet
if ($LASTEXITCODE -ne 0) { throw "构建失败" }
Write-Host "  构建成功" -ForegroundColor Green

# ── Step 2: 打包 ──
Write-Host "`n[2/4] 打包..." -ForegroundColor Yellow
$zipPath = "$env:TEMP\lybt-hotupdate.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$env:TEMP\lybt-hotupdate\*" -DestinationPath $zipPath -Force
$size = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "  打包完成: ${size} MB" -ForegroundColor Green

# ── Step 3: 获取登录 Token ──
Write-Host "`n[3/4] 登录获取 Token..." -ForegroundColor Yellow
try {
    $loginBody = @{ username = "sysadmin"; password = $Password } | ConvertTo-Json
    $loginResp = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $loginBody
    $token = $loginResp.data.accessToken
    if (-not $token) { throw "登录失败: $($loginResp.message)" }
    Write-Host "  登录成功" -ForegroundColor Green
} catch {
    Write-Host "  登录失败: $_" -ForegroundColor Red
    exit 1
}

$headers = @{ Authorization = "Bearer $token" }

# ── Step 4: 上传并触发更新 ──
Write-Host "`n[4/4] 上传更新包..." -ForegroundColor Yellow
try {
    $fileBytes = [System.IO.File]::ReadAllBytes($zipPath)
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $bodyLines = @(
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"lybt-webapi.zip`"",
        "Content-Type: application/zip",
        "",
        [System.Text.Encoding]::GetEncoding("iso-8859-1").GetString($fileBytes),
        "--$boundary--"
    )
    $body = $bodyLines -join $LF
    
    $uploadResp = Invoke-RestMethod -Uri "$baseUrl/api/v1/deploy/upload" -Method POST -ContentType "multipart/form-data; boundary=$boundary" -Body $body -Headers $headers
    Write-Host "  上传成功: $($uploadResp.message)" -ForegroundColor Green
    
    # 触发重启
    Write-Host "  触发服务器重启..." -ForegroundColor Gray
    $restartResp = Invoke-RestMethod -Uri "$baseUrl/api/v1/deploy/restart" -Method POST -Headers $headers
    Write-Host "  $($restartResp.message)" -ForegroundColor Green
    
} catch {
    Write-Host "  上传失败: $_" -ForegroundColor Red
    exit 1
}

# ── 等待重启完成 ──
Write-Host "  等待服务器重启..." -ForegroundColor Gray
$retries = 0
$maxRetries = 30
do {
    Start-Sleep -Seconds 3
    $retries++
    try {
        $health = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 5
        if ($health) { break }
    } catch { }
} while ($retries -lt $maxRetries)

# 重新登录验证
try {
    $loginBody2 = @{ username = "sysadmin"; password = $Password } | ConvertTo-Json
    $loginResp2 = Invoke-RestMethod -Uri "$baseUrl/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $loginBody2
    if ($loginResp2.data.accessToken) {
        Write-Host "  新版本运行正常" -ForegroundColor Green
    }
} catch {
    Write-Host "  重启后验证失败: $_" -ForegroundColor Red
}

# 清理
Remove-Item "$env:TEMP\lybt-hotupdate" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "  热更新完成!" -ForegroundColor Green
Write-Host "  服务: $baseUrl" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
