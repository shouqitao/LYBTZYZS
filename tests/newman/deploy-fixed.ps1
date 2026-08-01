# 部署脚本 - 排除配置文件，避免覆盖服务器配置
param(
    [string]$Server = "60.190.215.86",
    [string]$User = "player"
)

$ErrorActionPreference = "Stop"

Write-Host "=== 部署到 $Server ===" -ForegroundColor Cyan

# Step 1: 构建
Write-Host "[1/4] 构建..." -ForegroundColor Yellow
dotnet publish "D:\source\repos\LYBTZYZS\src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj" -c Release -o "$env:TEMP\lybt-publish" --self-contained false -v quiet

# Step 2: 打包（排除配置文件）
Write-Host "[2/4] 打包（排除配置文件）..." -ForegroundColor Yellow
$tempDir = "$env:TEMP\lybt-deploy"
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
Copy-Item "$env:TEMP\lybt-publish\*" $tempDir -Recurse

# 删除配置文件，保留其他所有文件
Remove-Item "$tempDir\appsettings*.json" -Force -ErrorAction SilentlyContinue

$zipPath = "$env:TEMP\lybt-webapi-update.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$tempDir\*" -DestinationPath $zipPath -Force
Write-Host "  打包完成: $([math]::Round((Get-Item $zipPath).Length / 1MB, 1)) MB" -ForegroundColor Green

# Step 3: 上传
Write-Host "[3/4] 上传..." -ForegroundColor Yellow
scp $zipPath "${User}@${Server}:/tmp/lybt-webapi-update.zip"

# Step 4: 部署
Write-Host "[4/4] 部署..." -ForegroundColor Yellow
ssh $User@$Server @"
killall dotnet 2>/dev/null
sleep 2
cd ~/lybt-api
unzip -o /tmp/lybt-webapi-update.zip -d . > /dev/null 2>&1
ASPNETCORE_ENVIRONMENT=Production nohup dotnet LYBT.WebAPI.dll > logs/webapi.log 2>&1 &
sleep 10
if pgrep -f 'dotnet LYBT.WebAPI.dll' > /dev/null; then
    echo 'Service started successfully'
else
    echo 'Service failed to start'
    tail -20 logs/webapi.log
fi
"@

Write-Host "`n=== 部署完成 ===" -ForegroundColor Green
Write-Host "服务地址: http://${Server}:5000" -ForegroundColor Cyan
