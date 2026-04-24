<#
.SYNOPSIS
    LYBT WebAPI 配置修复脚本 - Server 2012 R2 兼容
.DESCRIPTION
    修复 Kestrel 监听地址、SQL 连接字符串、HTTPS 端点
    在服务器上运行: powershell -ExecutionPolicy Bypass -File fix-config.ps1
#>

$ErrorActionPreference = "Continue"
$apiDir = "C:\Services\LYBT-API"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " LYBT WebAPI 配置修复" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# --- 1. 停止现有进程 ---
Write-Host "`n[1/5] 停止现有进程..." -ForegroundColor Yellow
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue
sc stop LYBT-API 2>$null
Start-Sleep -Seconds 2
Write-Host "已停止"

# --- 2. 修复 appsettings.json (基础配置) ---
Write-Host "`n[2/5] 修复基础配置..." -ForegroundColor Yellow

$baseConfigPath = "$apiDir\appsettings.json"
$baseJson = [System.IO.File]::ReadAllText($baseConfigPath) | ConvertFrom-Json

# 修复 JWT SecretKey
$baseJson.Jwt.SecretKey = "TFlCVF9TZWN1cmVfS2V5XzIwMjZfQUJDREVGMTIzNDU2Nzg="

# 修复连接字符串 - Trusted_Connection
$baseJson.ConnectionStrings.DefaultConnection = "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=50;Min Pool Size=5;Pooling=true;Encrypt=false;Application Name=LYBT.WebAPI"

# 删除 Https 端点
if ($baseJson.Kestrel.Endpoints.PSObject.Properties["Https"]) {
    $baseJson.Kestrel.Endpoints.PSObject.Properties.Remove("Https")
    Write-Host "  - 已删除 Https 端点"
}

# 修复默认密码
if ($baseJson.DefaultPasswords) {
    if (-not $baseJson.DefaultPasswords.SysAdminPassword) {
        $baseJson.DefaultPasswords | Add-Member -NotePropertyName "SysAdminPassword" -NotePropertyValue "Admin@Qt2026!"
    }
    if (-not $baseJson.DefaultPasswords.NewUserPassword) {
        $baseJson.DefaultPasswords | Add-Member -NotePropertyName "NewUserPassword" -NotePropertyValue "LYBT@2026!"
    }
}
if ($baseJson.SystemAdmin -and -not $baseJson.SystemAdmin.Email) {
    $baseJson.SystemAdmin | Add-Member -NotePropertyName "Email" -NotePropertyValue "admin@lybt.com"
}

# 保存
[System.IO.File]::WriteAllText($baseConfigPath, ($baseJson | ConvertTo-Json -Depth 20), [System.Text.Encoding]::UTF8)
Write-Host "  - appsettings.json 已修复"

# --- 3. 修复 appsettings.Production.json ---
Write-Host "`n[3/5] 修复生产配置..." -ForegroundColor Yellow

$prodConfigPath = "$apiDir\appsettings.Production.json"
$prodJson = [System.IO.File]::ReadAllText($prodConfigPath) | ConvertFrom-Json

# Kestrel 监听 0.0.0.0:5000
$prodJson.Kestrel.Endpoints.Http.Url = "http://0.0.0.0:5000"

# 删除 Https 端点
if ($prodJson.Kestrel.Endpoints.PSObject.Properties["Https"]) {
    $prodJson.Kestrel.Endpoints.PSObject.Properties.Remove("Https")
    Write-Host "  - 已删除 Https 端点"
}

# JWT
$prodJson.Jwt.SecretKey = "TFlCVF9TZWN1cmVfS2V5XzIwMjZfQUJDREVGMTIzNDU2Nzg="

# 连接字符串
$prodJson.ConnectionStrings.DefaultConnection = "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=50;Min Pool Size=5;Pooling=true;Encrypt=false;Application Name=LYBT.WebAPI"

# 保存
[System.IO.File]::WriteAllText($prodConfigPath, ($prodJson | ConvertTo-Json -Depth 20), [System.Text.Encoding]::UTF8)
Write-Host "  - appsettings.Production.json 已修复"
Write-Host "  - 监听地址: http://0.0.0.0:5000"

# --- 4. 删除冲突的 .exe 文件 ---
Write-Host "`n[4/5] 清理冲突文件..." -ForegroundColor Yellow
if (Test-Path "$apiDir\LYBT.WebAPI.exe") {
    Remove-Item "$apiDir\LYBT.WebAPI.exe" -Force
    Write-Host "  - 已删除 LYBT.WebAPI.exe (避免与 .dll 冲突)"
} else {
    Write-Host "  - 无冲突文件"
}

# --- 5. 启动服务 ---
Write-Host "`n[5/5] 启动应用..." -ForegroundColor Yellow

# 删除旧服务
sc stop LYBT-API 2>$null
sc delete LYBT-API 2>$null
Start-Sleep -Seconds 2

# 创建新服务 (LocalSystem，避免账户密码问题)
sc create LYBT-API binPath= "$apiDir\start-api.bat" start= auto DisplayName= "LYBT WebAPI Service"
sc description LYBT-API "LYBT WebAPI - 凌隐宝堂中医诊所后端服务"

# 启动（会报 1053 但进程会起来，这是正常的）
Write-Host "  启动中 (SCM会超时，但进程会运行)..." -ForegroundColor Yellow
sc start LYBT-API 2>$null
Start-Sleep -Seconds 10

# 验证
$dotnetProc = Get-Process -Name dotnet -ErrorAction SilentlyContinue
if ($dotnetProc) {
    Write-Host "`n[OK] dotnet 进程运行中 (PID: $($dotnetProc.Id))" -ForegroundColor Green
} else {
    Write-Host "`n[WARN] 未检测到 dotnet 进程，尝试手动启动..." -ForegroundColor Yellow
    Start-Process -FilePath "C:\Program Files\dotnet\dotnet.exe" `
        -ArgumentList "$apiDir\LYBT.WebAPI.dll" `
        -WorkingDirectory $apiDir `
        -Environment @{"ASPNETCORE_ENVIRONMENT"="Production"} `
        -NoNewWindow
    Start-Sleep -Seconds 8
    $dotnetProc = Get-Process -Name dotnet -ErrorAction SilentlyContinue
    if ($dotnetProc) {
        Write-Host "[OK] 手动启动成功 (PID: $($dotnetProc.Id))" -ForegroundColor Green
    }
}

# 端口检查
$portCheck = netstat -ano | findstr ":5000"
if ($portCheck) {
    Write-Host "`n端口监听:" -ForegroundColor Green
    Write-Host $portCheck
} else {
    Write-Host "`n[WARN] 5000 端口未监听，检查日志:" -ForegroundColor Yellow
    Get-Content "$apiDir\logs\bootstrap-*.log" -Tail 10 -ErrorAction SilentlyContinue
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " 完成！访问: http://$(hostname):5000/health" -ForegroundColor Cyan
Write-Host " 本机测试: http://127.0.0.1:5000/health" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
