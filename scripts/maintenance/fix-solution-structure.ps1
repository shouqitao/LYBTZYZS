# 修复Solution文件结构错误
# Issue: dotnet sln add 自动生成的错误文件夹层次导致VS启动配置失效

$slnFile = "LYBT.All.sln"
$backupFile = "LYBT.All.sln.backup-fix"

Write-Host "开始修复Solution文件结构..." -ForegroundColor Cyan

# 1. 创建备份
Write-Host "`n[1/6] 创建备份..." -ForegroundColor Yellow
Copy-Item $slnFile $backupFile -Force
Write-Host "  ✓ 已备份到 $backupFile" -ForegroundColor Green

# 2. 读取Solution文件
Write-Host "`n[2/6] 读取Solution文件..." -ForegroundColor Yellow
$content = Get-Content $slnFile -Raw -Encoding UTF8

# 3. 删除错误的项目和文件夹定义（行152-164）
Write-Host "`n[3/6] 删除错误的自动生成定义..." -ForegroundColor Yellow

# 删除错误的Server文件夹（测试层次）
$content = $content -replace 'Project\("\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}"\) = "Server", "Server", "\{0B58203F-9D59-4D9F-942E-CB960D19FC0A\}"[\r\n]+EndProject[\r\n]+', ''

# 删除错误的Core文件夹（测试层次）
$content = $content -replace 'Project\("\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}"\) = "Core", "Core", "\{583504E0-1CBF-49E8-8CAD-F0F621434B78\}"[\r\n]+EndProject[\r\n]+', ''

# 删除错误的src文件夹
$content = $content -replace 'Project\("\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}"\) = "src", "src", "\{2093028A-9064-4A7D-8580-7C3AED0B094E\}"[\r\n]+EndProject[\r\n]+', ''

# 删除错误的Server文件夹（src层次）
$content = $content -replace 'Project\("\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}"\) = "Server", "Server", "\{8A6B99D7-E5AE-4503-A29C-C3CFAFE671D6\}"[\r\n]+EndProject[\r\n]+', ''

# 删除错误的Core文件夹（src层次）
$content = $content -replace 'Project\("\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}"\) = "Core", "Core", "\{8D451E98-7835-4902-A4A3-9C9562ABA698\}"[\r\n]+EndProject[\r\n]+', ''

# 删除EventBus.Tests和Server.Interfaces的错误位置定义
$content = $content -replace 'Project\("\{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC\}"\) = "LYBT\.EventBus\.Tests", "tests\\UnitTests\\Server\\Core\\LYBT\.EventBus\.Tests\\LYBT\.EventBus\.Tests\.csproj", "\{8E16421C-52D1-4522-8987-D2A839E32B02\}"[\r\n]+EndProject[\r\n]+', ''

$content = $content -replace 'Project\("\{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC\}"\) = "LYBT\.Server\.Interfaces", "src\\Server\\Core\\LYBT\.Server\.Interfaces\\LYBT\.Server\.Interfaces\.csproj", "\{15C25E1A-6E41-4936-B5C5-968DFC3F1D19\}"[\r\n]+EndProject[\r\n]+', ''

Write-Host "  ✓ 已删除错误定义" -ForegroundColor Green

# 4. 在正确位置添加项目定义
Write-Host "`n[4/6] 在正确位置添加项目..." -ForegroundColor Yellow

# 在LYBT.EventBus后添加Server.Interfaces
$eventBusPattern = '(Project\("\{9A19103F-16F7-4668-BE54-9A1E7A4F7556\}"\) = "LYBT\.EventBus", "src\\Server\\Core\\LYBT\.EventBus\\LYBT\.EventBus\.csproj", "\{E4F5A6B7-C8D9-8E9F-2A3B-4C5D6E7F8A9B\}"[\r\n]+EndProject)'

$serverInterfacesInsert = @"
$1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LYBT.Server.Interfaces", "src\Server\Core\LYBT.Server.Interfaces\LYBT.Server.Interfaces.csproj", "{15C25E1A-6E41-4936-B5C5-968DFC3F1D19}"
EndProject
"@

$content = $content -replace $eventBusPattern, $serverInterfacesInsert

# 添加tests/UnitTests/Server/Core文件夹定义（在UnitTests.Server文件夹定义之后）
$testsServerPattern = '(Project\("\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}"\) = "UnitTests", "UnitTests", "\{394A81D9-3C45-9818-BB26-23F2B85056FF\}"[\r\n]+EndProject)'

$testsCoreInsert = @"
$1
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Server.Core", "Server.Core", "{E9FAEFA8-21CF-41A0-8A53-075BB100C47E}"
EndProject
"@

$content = $content -replace $testsServerPattern, $testsCoreInsert

# 在合适位置添加EventBus.Tests项目（在Shared.Components之后）
$sharedComponentsPattern = '(Project\("\{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC\}"\) = "LYBT\.Shared\.Components", "src\\Shared\\LYBT\.Shared\.Components\\LYBT\.Shared\.Components\.csproj", "\{AC561A4F-4F50-4737-838B-D9F2288A809A\}"[\r\n]+EndProject)'

$eventBusTestsInsert = @"
$1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LYBT.EventBus.Tests", "tests\UnitTests\Server\Core\LYBT.EventBus.Tests\LYBT.EventBus.Tests.csproj", "{8E16421C-52D1-4522-8987-D2A839E32B02}"
EndProject
"@

$content = $content -replace $sharedComponentsPattern, $eventBusTestsInsert

Write-Host "  ✓ 已添加正确定义" -ForegroundColor Green

# 5. 修复NestedProjects映射
Write-Host "`n[5/6] 修复NestedProjects映射..." -ForegroundColor Yellow

# 删除错误的映射行
$content = $content -replace '\s*\{0B58203F-9D59-4D9F-942E-CB960D19FC0A\} = \{394A81D9-3C45-9818-BB26-23F2B85056FF\}[\r\n]+', ''
$content = $content -replace '\s*\{583504E0-1CBF-49E8-8CAD-F0F621434B78\} = \{0B58203F-9D59-4D9F-942E-CB960D19FC0A\}[\r\n]+', ''
$content = $content -replace '\s*\{8E16421C-52D1-4522-8987-D2A839E32B02\} = \{583504E0-1CBF-49E8-8CAD-F0F621434B78\}[\r\n]+', ''
$content = $content -replace '\s*\{8A6B99D7-E5AE-4503-A29C-C3CFAFE671D6\} = \{2093028A-9064-4A7D-8580-7C3AED0B094E\}[\r\n]+', ''
$content = $content -replace '\s*\{8D451E98-7835-4902-A4A3-9C9562ABA698\} = \{8A6B99D7-E5AE-4503-A29C-C3CFAFE671D6\}[\r\n]+', ''
$content = $content -replace '\s*\{15C25E1A-6E41-4936-B5C5-968DFC3F1D19\} = \{8D451E98-7835-4902-A4A3-9C9562ABA698\}[\r\n]+', ''

# 在正确位置添加映射（在EventBus.Tests前添加）
$nestedPattern = '(\s*\{AC561A4F-4F50-4737-838B-D9F2288A809A\} = \{088967DC-D878-4BE2-9A6E-B9A9BF72FC98\})'

$nestedInsert = @"
$1
		{E9FAEFA8-21CF-41A0-8A53-075BB100C47E} = {394A81D9-3C45-9818-BB26-23F2B85056FF}
		{8E16421C-52D1-4522-8987-D2A839E32B02} = {E9FAEFA8-21CF-41A0-8A53-075BB100C47E}
		{15C25E1A-6E41-4936-B5C5-968DFC3F1D19} = {B2C3D4E5-F6A7-5B6C-9D0E-1F2A3B4C5D6E}
"@

$content = $content -replace $nestedPattern, $nestedInsert

Write-Host "  ✓ 已修复映射关系" -ForegroundColor Green

# 6. 保存修复后的文件
Write-Host "`n[6/6] 保存修复结果..." -ForegroundColor Yellow
$content | Set-Content $slnFile -Encoding UTF8 -NoNewline
Write-Host "  ✓ 已保存到 $slnFile" -ForegroundColor Green

Write-Host "`n✅ Solution文件结构修复完成！" -ForegroundColor Green
Write-Host "`n建议验证步骤：" -ForegroundColor Cyan
Write-Host "1. dotnet sln list - 验证项目列表" -ForegroundColor White
Write-Host "2. dotnet build LYBT.All.sln - 验证编译" -ForegroundColor White
Write-Host "3. 在VS中打开解决方案，检查文件夹层次和启动项配置" -ForegroundColor White
