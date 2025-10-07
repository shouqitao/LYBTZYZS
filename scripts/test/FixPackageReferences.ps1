# 修复测试项目中重复的PackageReference
$testProjects = @(
    "tests\UnitTests\Modules\Auth.UnitTests\LYBT.Module.Auth.Tests.csproj",
    "tests\UnitTests\Modules\Herbs.UnitTests\LYBT.Module.Herbs.Tests.csproj",
    "tests\UnitTests\Modules\MedicalCase.UnitTests\LYBT.Module.MedicalCase.Tests.csproj",
    "tests\UnitTests\Modules\Patients.UnitTests\LYBT.Module.Patients.Tests.csproj",
    "tests\UnitTests\Modules\Users.UnitTests\LYBT.Module.Users.Tests.csproj",
    "tests\UnitTests\Shared.Models.UnitTests\LYBT.Shared.Models.Tests.csproj",
    "tests\UnitTests\Shared\LYBT.Shared.Utilities.Tests\LYBT.Shared.Utilities.Tests.csproj"
)

foreach ($projectFile in $testProjects) {
    if (Test-Path $projectFile) {
        Write-Host "Processing: $projectFile" -ForegroundColor Cyan

        # 读取文件内容
        $content = Get-Content $projectFile -Raw

        # 移除coverlet.collector引用（包括可能的多行格式）
        $pattern = '<PackageReference\s+Include="coverlet\.collector"[^>]*>(?:\s*<[^>]+>\s*</[^>]+>\s*)*</PackageReference>'
        $newContent = $content -replace $pattern, ''

        # 也移除单行格式
        $pattern2 = '<PackageReference\s+Include="coverlet\.collector"[^/>]*/?>'
        $newContent = $newContent -replace $pattern2, ''

        # 保存文件
        Set-Content $projectFile $newContent -NoNewline
        Write-Host "  - Removed coverlet.collector reference" -ForegroundColor Green
    }
}

Write-Host "`nCompleted fixing duplicate PackageReference warnings!" -ForegroundColor Yellow