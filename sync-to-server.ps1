# LYBT WebAPI 同步脚本 - 本地修改直接同步到服务器
# 使用方式: .\sync-to-server.ps1 [-Restart] [-Build] [-ConfigOnly]

param(
    [switch]$Restart,
    [switch]$Build,
    [switch]$ConfigOnly
)

$ErrorActionPreference = "Stop"
$Server = "60.190.215.86"
$Port = 5555
$User = "player"
$RemotePath = "/home/player/lybt-api"
$ProjectPath = "src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj"
$DotnetPath = "/home/player/.dotnet/dotnet"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  LYBT WebAPI 同步到服务器" -ForegroundColor Cyan
Write-Host "  目标: ${User}@${Server}:${Port}" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# Step 1: Build (if requested or not ConfigOnly)
if ($Build -or (-not $ConfigOnly)) {
    Write-Host "`n[1/4] 构建项目..." -ForegroundColor Yellow
    dotnet publish $ProjectPath -c Release -o "./publish-webapi" --self-contained false -v quiet
    if ($LASTEXITCODE -ne 0) { throw "构建失败" }
    Write-Host "  构建成功" -ForegroundColor Green
}

# Step 2: Sync files
Write-Host "`n[2/4] 同步文件到服务器..." -ForegroundColor Yellow

if ($ConfigOnly) {
    # Only upload config files
    Write-Host "  仅同步配置文件..." -ForegroundColor Gray
    @("appsettings.json", "appsettings.Production.json") | ForEach-Object {
        $localFile = "src/Server/Services/LYBT.WebAPI/$_"
        if (Test-Path $localFile) {
            Write-Host "  上传: $_" -ForegroundColor Gray
            scp -P $Port $localFile "${User}@${Server}:${RemotePath}/"
        }
    }
} else {
    # Upload all files
    Write-Host "  使用 scp 上传..." -ForegroundColor Gray
    $publishDir = "./publish-webapi"
    Get-ChildItem $publishDir -File | ForEach-Object {
        Write-Host "  上传: $($_.Name)" -ForegroundColor Gray
        scp -P $Port $_.FullName "${User}@${Server}:${RemotePath}/"
    }
}
Write-Host "  同步完成" -ForegroundColor Green

# Step 3: Restart (if requested)
if ($Restart) {
    Write-Host "`n[3/4] 重启服务..." -ForegroundColor Yellow
    $restartCmd = @"
# 查找并终止旧进程
pkill -9 -f 'dotnet.*LYBT' || true
sleep 2

# 启动新进程 (后台)
cd ${RemotePath}
nohup ${DotnetPath} LYBT.WebAPI.dll --environment Production > /dev/null 2>&1 &
sleep 5

# 验证启动
if pgrep -f 'dotnet.*LYBT' > /dev/null; then
    echo "服务已启动"
    curl -s http://localhost:5000/health
else
    echo "启动失败"
    exit 1
fi
"@
    ssh -p $Port "${User}@${Server}" $restartCmd
    Write-Host "  服务已重启" -ForegroundColor Green
} else {
    Write-Host "`n[3/4] 跳过重启 (使用 -Restart 参数重启)" -ForegroundColor Gray
}

# Step 4: Verify
Write-Host "`n[4/4] 验证..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
try {
    $health = Invoke-RestMethod -Uri "http://${Server}:5000/health" -TimeoutSec 5
    Write-Host "  健康检查: 通过" -ForegroundColor Green
} catch {
    Write-Host "  健康检查: 失败 ($_)" -ForegroundColor Red
}

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "  同步完成!" -ForegroundColor Green
Write-Host "  服务: http://${Server}:5000" -ForegroundColor Cyan
Write-Host "  Swagger: http://${Server}:5000/swagger" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
