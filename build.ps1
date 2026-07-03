# LYBT 本地构建脚本
# 使用方式: .\build.ps1 [-Clean] [-Run]

param(
    [switch]$Clean,
    [switch]$Run
)

Write-Host "凌隐宝堂 - 本地构建" -ForegroundColor Cyan

if ($Clean) {
    Write-Host "清理旧构建..." -ForegroundColor Yellow
    dotnet clean LYBTZYZS.sln -v quiet
}

Write-Host "构建解决方案..." -ForegroundColor Yellow
dotnet build LYBTZYZS.sln -c Debug -v quiet
if ($LASTEXITCODE -ne 0) { throw "构建失败" }
Write-Host "构建成功 (0 错误)" -ForegroundColor Green

if ($Run) {
    Write-Host "启动 WebAPI..." -ForegroundColor Yellow
    Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Server/Services/LYBT.WebAPI --urls http://localhost:5000" -WindowStyle Hidden
    Write-Host "WebAPI 启动中: http://localhost:5000" -ForegroundColor Green
}
