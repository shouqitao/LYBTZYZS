#requires -RunAsAdministrator
# LYBT WebAPI Windows Server 卸载脚本

param(
    [Parameter()]
    [string]$DeployPath = "C:\Services\LYBT-API",
    
    [Parameter()]
    [string]$ServiceName = "LYBT-API",
    
    [Parameter()]
    [switch]$RemoveData
)

$ErrorActionPreference = "Stop"

Write-Host "=== LYBT WebAPI 卸载脚本 ===" -ForegroundColor Cyan
Write-Host ""

# 停止并删除服务
Write-Host "[1/3] 停止并删除 Windows Service..." -ForegroundColor Yellow
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -eq "Running") {
        Write-Host "  停止服务..." -ForegroundColor Gray
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }
    
    Write-Host "  删除服务..." -ForegroundColor Gray
    sc.exe delete $ServiceName | Out-Null
    Write-Host "✓ 服务已删除" -ForegroundColor Green
} else {
    Write-Host "  服务不存在，跳过" -ForegroundColor Gray
}

# 删除防火墙规则
Write-Host ""
Write-Host "[2/3] 删除防火墙规则..." -ForegroundColor Yellow
$rules = Get-NetFirewallRule -DisplayName "LYBT-API-*" -ErrorAction SilentlyContinue
if ($rules) {
    $rules | Remove-NetFirewallRule
    Write-Host "✓ 防火墙规则已删除" -ForegroundColor Green
} else {
    Write-Host "  防火墙规则不存在，跳过" -ForegroundColor Gray
}

# 删除部署目录
Write-Host ""
Write-Host "[3/3] 删除部署文件..." -ForegroundColor Yellow
if (Test-Path $DeployPath) {
    if ($RemoveData) {
        Remove-Item -Path $DeployPath -Recurse -Force
        Write-Host "✓ 部署目录已删除: $DeployPath" -ForegroundColor Green
    } else {
        Write-Host "  保留数据模式，仅删除可执行文件" -ForegroundColor Yellow
        $keepPaths = @("logs", "data")
        Get-ChildItem -Path $DeployPath | Where-Object {
            $keepPaths -notcontains $_.Name
        } | Remove-Item -Recurse -Force
        Write-Host "✓ 可执行文件已删除，日志和数据已保留" -ForegroundColor Green
    }
} else {
    Write-Host "  部署目录不存在，跳过" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== 卸载完成 ===" -ForegroundColor Cyan
