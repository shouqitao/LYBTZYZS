# PowerShell脚本 - 验证端口配置

Write-Host "=== 端口配置验证 ===" -ForegroundColor Green

# 检查Program.cs配置
Write-Host "`n1. 检查Program.cs默认端口配置:" -ForegroundColor Yellow
$programCs = Get-Content "src/Server/Services/LYBT.WebAPI/Program.cs" | Select-String "localhost:"
Write-Host "   $programCs" -ForegroundColor Cyan

# 检查launchSettings.json
Write-Host "`n2. 检查launchSettings.json配置:" -ForegroundColor Yellow
$launchSettings = Get-Content "src/Server/Services/LYBT.WebAPI/Properties/launchSettings.json" | Select-String '"applicationUrl"'
foreach ($line in $launchSettings) {
    Write-Host "   $line" -ForegroundColor Cyan
}

# 检查Client配置
Write-Host "`n3. 检查Client配置:" -ForegroundColor Yellow
$clientConfig1 = Get-Content "src/Client/Desktop/Shell/appsettings.json" | Select-String "BaseUrl"
Write-Host "   Shell/appsettings.json: $clientConfig1" -ForegroundColor Cyan

$clientConfig2 = Get-Content "src/Client/Desktop/Core/Configuration/AppConfiguration.cs" | Select-String "localhost:5001"
Write-Host "   AppConfiguration.cs中找到 $($clientConfig2.Count) 处5001端口配置" -ForegroundColor Cyan

# 检查README
Write-Host "`n4. 检查README.md端口说明:" -ForegroundColor Yellow
$readme = Get-Content "README.md" | Select-String "localhost:5001"
Write-Host "   找到 $($readme.Count) 处5001端口引用" -ForegroundColor Cyan

Write-Host "`n=== 验证完成 ===" -ForegroundColor Green