# P4-Server 发布就绪 - 临时禁用测试项目可空性警告
# 目标：快速降低编译警告数量至≤100，专注Server核心项目质量

Write-Host "P4-Server 编译警告治理 - 禁用测试项目可空性检查" -ForegroundColor Green

$testProjects = @(
    "tests/Architecture/LYBT.ArchTests.csproj",
    "tests/TestUtilities/TestBase/TestBase.csproj",
    "tests/TestUtilities/TestDataFactory.UnitTests/TestDataFactory.csproj",
    "tests/TestUtilities/TestUtilities/TestUtilities.csproj",
    "tests/UltraThink/TestInfrastructure/LYBT.Tests.UltraThink.TestInfrastructure.csproj",
    "tests/UnitTests/Core/Core/LYBT.Tests.Core.csproj",
    "tests/UnitTests/Core/Core/Services/LYBT.Tests.Simplified.csproj",
    "tests/UnitTests/Modules/Auth.UnitTests/LYBT.Module.Auth.Tests.csproj",
    "tests/UnitTests/Modules/Consultation.UnitTests/LYBT.Module.Consultation.Tests.csproj",
    "tests/UnitTests/Modules/Formula.UnitTests/LYBT.Module.Formula.Tests.csproj",
    "tests/UnitTests/Modules/Herbs.UnitTests/LYBT.Module.Herbs.Tests.csproj",
    "tests/UnitTests/Modules/MedicalCase.UnitTests/LYBT.Module.MedicalCase.Tests.csproj",
    "tests/UnitTests/Modules/Patients.UnitTests/LYBT.Module.Patients.Tests.csproj",
    "tests/UnitTests/Modules/Prescriptions.UnitTests/LYBT.Module.Prescriptions.Tests.csproj",
    "tests/UnitTests/Modules/Users.UnitTests/LYBT.Module.Users.Tests.csproj",
    "tests/UnitTests/Shared.Models.UnitTests/LYBT.Shared.Models.Tests.csproj"
)

$processedCount = 0
$skippedCount = 0

foreach ($project in $testProjects) {
    if (Test-Path $project) {
        Write-Host "处理项目: $project" -ForegroundColor Yellow
        
        # 读取项目文件内容
        $content = Get-Content $project -Raw
        
        # 检查是否已经有Nullable设置
        if ($content -match '<Nullable>enable</Nullable>') {
            # 替换为disable
            $content = $content -replace '<Nullable>enable</Nullable>', '<Nullable>disable</Nullable>'
            Set-Content $project -Value $content -NoNewline
            Write-Host "  ✅ 已禁用可空性检查" -ForegroundColor Green
            $processedCount++
        }
        elseif ($content -match '<Nullable>.*</Nullable>') {
            Write-Host "  ⚠️ 项目已有其他Nullable设置，跳过" -ForegroundColor Yellow
            $skippedCount++
        }
        else {
            # 添加Nullable disable到PropertyGroup
            if ($content -match '(<PropertyGroup[^>]*>)') {
                $content = $content -replace '(<PropertyGroup[^>]*>)', "`$1`n    <Nullable>disable</Nullable>"
                Set-Content $project -Value $content -NoNewline
                Write-Host "  ✅ 已添加Nullable disable配置" -ForegroundColor Green
                $processedCount++
            }
            else {
                Write-Host "  ❌ 无法找到PropertyGroup，跳过" -ForegroundColor Red
                $skippedCount++
            }
        }
    }
    else {
        Write-Host "  ❌ 项目文件不存在: $project" -ForegroundColor Red
        $skippedCount++
    }
}

Write-Host "`n📊 处理结果统计:" -ForegroundColor Green
Write-Host "  已处理项目: $processedCount" -ForegroundColor Green
Write-Host "  跳过项目: $skippedCount" -ForegroundColor Yellow
Write-Host "  总项目数: $($testProjects.Count)" -ForegroundColor Cyan

Write-Host "`n🎯 下一步: 运行 'dotnet build LYBT.Server.sln --configuration Release' 验证警告数量" -ForegroundColor Green