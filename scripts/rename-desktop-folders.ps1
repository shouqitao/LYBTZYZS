# Desktop 架构优化 - 文件夹重命名脚本
# Issue #820: 统一文件夹命名规范
# 生成时间: 2025-09-30

$ErrorActionPreference = "Stop"
$rootPath = "D:\source\repos\LYBTZYZS"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Desktop 架构优化 - 文件夹重命名" -ForegroundColor Cyan
Write-Host "Issue #820" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查工作目录
Set-Location $rootPath
Write-Host "当前工作目录: $rootPath" -ForegroundColor Green
Write-Host ""

# 重命名映射表
$renameMappings = @(
    @{
        Old = "src\Client\Desktop\Core_New"
        New = "src\Client\Desktop\Core"
        Description = "核心架构文件夹"
    },
    @{
        Old = "src\Client\Desktop\Modules\Auth"
        New = "src\Client\Desktop\Modules\LYBT.Desktop.Auth"
        Description = "认证模块"
    },
    @{
        Old = "src\Client\Desktop\Modules\Consultation"
        New = "src\Client\Desktop\Modules\LYBT.Desktop.Consultation"
        Description = "就诊模块"
    },
    @{
        Old = "src\Client\Desktop\Modules\Formula"
        New = "src\Client\Desktop\Modules\LYBT.Desktop.Formula"
        Description = "方剂模块"
    },
    @{
        Old = "src\Client\Desktop\Modules\Herbs"
        New = "src\Client\Desktop\Modules\LYBT.Desktop.Herbs"
        Description = "药材模块"
    },
    @{
        Old = "src\Client\Desktop\Modules\MedicalCase"
        New = "src\Client\Desktop\Modules\LYBT.Desktop.MedicalCase"
        Description = "病历模块"
    },
    @{
        Old = "src\Client\Desktop\Modules\Patients"
        New = "src\Client\Desktop\Modules\LYBT.Desktop.Patients"
        Description = "患者模块"
    },
    @{
        Old = "src\Client\Desktop\Modules\Prescriptions"
        New = "src\Client\Desktop\Modules\LYBT.Desktop.Prescriptions"
        Description = "处方模块"
    },
    @{
        Old = "src\Client\Desktop\Modules\Users"
        New = "src\Client\Desktop\Modules\LYBT.Desktop.Users"
        Description = "用户模块"
    }
)

# 执行重命名
$successCount = 0
$failCount = 0
$skippedCount = 0

foreach ($mapping in $renameMappings) {
    $oldPath = Join-Path $rootPath $mapping.Old
    $newPath = Join-Path $rootPath $mapping.New

    Write-Host "[$($mapping.Description)]" -ForegroundColor Yellow
    Write-Host "  旧路径: $($mapping.Old)" -ForegroundColor Gray
    Write-Host "  新路径: $($mapping.New)" -ForegroundColor Gray

    if (-not (Test-Path $oldPath)) {
        Write-Host "  ⚠️  旧路径不存在，跳过" -ForegroundColor DarkYellow
        $skippedCount++
        Write-Host ""
        continue
    }

    if (Test-Path $newPath) {
        Write-Host "  ⚠️  新路径已存在，跳过" -ForegroundColor DarkYellow
        $skippedCount++
        Write-Host ""
        continue
    }

    try {
        # 使用 git mv 保留历史（如果是 git 仓库）
        $gitStatus = git status 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  🔄 使用 git mv 重命名..." -ForegroundColor Cyan
            git mv $mapping.Old $mapping.New 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✅ 成功" -ForegroundColor Green
                $successCount++
            } else {
                # git mv 失败，尝试普通重命名
                Write-Host "  ⚠️  git mv 失败，尝试普通重命名..." -ForegroundColor Yellow
                Move-Item -Path $oldPath -Destination $newPath -Force
                Write-Host "  ✅ 成功（普通重命名）" -ForegroundColor Green
                $successCount++
            }
        } else {
            # 不是 git 仓库，使用普通重命名
            Write-Host "  🔄 重命名中..." -ForegroundColor Cyan
            Move-Item -Path $oldPath -Destination $newPath -Force
            Write-Host "  ✅ 成功" -ForegroundColor Green
            $successCount++
        }
    }
    catch {
        Write-Host "  ❌ 失败: $_" -ForegroundColor Red
        $failCount++
    }

    Write-Host ""
}

# 总结
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "重命名完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ 成功: $successCount" -ForegroundColor Green
Write-Host "⚠️  跳过: $skippedCount" -ForegroundColor Yellow
Write-Host "❌ 失败: $failCount" -ForegroundColor Red
Write-Host ""

if ($failCount -gt 0) {
    Write-Host "⚠️  部分重命名失败，请检查错误信息" -ForegroundColor Red
    Write-Host "可能的原因:" -ForegroundColor Yellow
    Write-Host "  1. 文件正在被其他程序使用（Visual Studio, dotnet 进程等）" -ForegroundColor Gray
    Write-Host "  2. 权限不足" -ForegroundColor Gray
    Write-Host "  3. 文件夹被锁定" -ForegroundColor Gray
    Write-Host ""
    Write-Host "建议:" -ForegroundColor Yellow
    Write-Host "  1. 关闭 Visual Studio" -ForegroundColor Gray
    Write-Host "  2. 停止所有 dotnet 进程" -ForegroundColor Gray
    Write-Host "  3. 以管理员身份重新运行此脚本" -ForegroundColor Gray
    exit 1
}

Write-Host "🎉 所有文件夹重命名成功！" -ForegroundColor Green
Write-Host ""
Write-Host "下一步:" -ForegroundColor Cyan
Write-Host "  1. 更新项目文件 (.csproj)" -ForegroundColor Gray
Write-Host "  2. 更新解决方案文件 (.sln)" -ForegroundColor Gray
Write-Host "  3. 更新命名空间和 using 语句" -ForegroundColor Gray
Write-Host "  4. 编译验证" -ForegroundColor Gray
Write-Host ""

exit 0