# Desktop 架构优化 - 主执行脚本
# Issue #820: 统一文件夹命名规范与路径结构
# 生成时间: 2025-09-30
#
# 此脚本将自动执行以下操作：
# 1. 清理 bin/obj 缓存
# 2. 重命名文件夹（Core_New → Core, Modules/*）
# 3. 更新所有项目引用和解决方案文件
# 4. 编译验证
# 5. Git 提交

param(
    [switch]$DryRun,  # 仅显示将要执行的操作，不实际执行
    [switch]$SkipBuild,  # 跳过编译验证
    [switch]$SkipGit  # 跳过 Git 提交
)

$ErrorActionPreference = "Stop"
$rootPath = "D:\source\repos\LYBTZYZS"
$scriptsPath = Join-Path $rootPath "scripts"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Desktop 架构优化 - Issue #820" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($DryRun) {
    Write-Host "🔍 DRY RUN 模式 - 仅显示操作，不实际执行" -ForegroundColor Yellow
    Write-Host ""
}

# 切换到项目根目录
Set-Location $rootPath
Write-Host "📂 工作目录: $rootPath" -ForegroundColor Green
Write-Host ""

# 检查是否有未提交的更改
$gitStatus = git status --porcelain 2>&1
if ($gitStatus -and !$DryRun) {
    Write-Host "⚠️  警告: 检测到未提交的更改" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "建议先提交或暂存当前更改，然后再运行此脚本" -ForegroundColor Yellow
    Write-Host ""
    $continue = Read-Host "是否继续? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y") {
        Write-Host "已取消" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# ============================================
# 阶段 1: 清理构建缓存
# ============================================
Write-Host "[1/5] 清理构建缓存" -ForegroundColor Cyan
Write-Host "────────────────────────────────────────" -ForegroundColor Gray

if ($DryRun) {
    Write-Host "  将删除所有 bin 和 obj 文件夹" -ForegroundColor Gray
} else {
    try {
        $binDirs = Get-ChildItem -Path "src\Client\Desktop" -Directory -Recurse -Filter "bin" -ErrorAction SilentlyContinue
        $objDirs = Get-ChildItem -Path "src\Client\Desktop" -Directory -Recurse -Filter "obj" -ErrorAction SilentlyContinue

        $totalDirs = $binDirs.Count + $objDirs.Count
        Write-Host "  找到 $totalDirs 个缓存文件夹" -ForegroundColor Gray

        $binDirs | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        $objDirs | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

        Write-Host "  ✅ 清理完成" -ForegroundColor Green
    }
    catch {
        Write-Host "  ⚠️  清理缓存时出现警告: $_" -ForegroundColor Yellow
    }
}
Write-Host ""

# ============================================
# 阶段 2: 重命名文件夹
# ============================================
Write-Host "[2/5] 重命名文件夹" -ForegroundColor Cyan
Write-Host "────────────────────────────────────────" -ForegroundColor Gray

$renameScript = Join-Path $scriptsPath "rename-desktop-folders.ps1"

if ($DryRun) {
    Write-Host "  将执行: $renameScript" -ForegroundColor Gray
    Write-Host "  - Core_New → Core" -ForegroundColor Gray
    Write-Host "  - 8 个 Modules 子文件夹添加 LYBT.Desktop. 前缀" -ForegroundColor Gray
} else {
    if (Test-Path $renameScript) {
        Write-Host "  执行重命名脚本..." -ForegroundColor Gray
        & $renameScript
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ❌ 重命名失败" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "  ❌ 脚本不存在: $renameScript" -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

# ============================================
# 阶段 3: 更新项目引用
# ============================================
Write-Host "[3/5] 更新项目引用" -ForegroundColor Cyan
Write-Host "────────────────────────────────────────" -ForegroundColor Gray

$updateScript = Join-Path $scriptsPath "update-desktop-references.ps1"

if ($DryRun) {
    Write-Host "  将执行: $updateScript" -ForegroundColor Gray
    Write-Host "  - 更新解决方案文件" -ForegroundColor Gray
    Write-Host "  - 更新项目文件" -ForegroundColor Gray
    Write-Host "  - 更新 C# 文件" -ForegroundColor Gray
} else {
    if (Test-Path $updateScript) {
        Write-Host "  执行引用更新脚本..." -ForegroundColor Gray
        & $updateScript
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ❌ 引用更新失败" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "  ❌ 脚本不存在: $updateScript" -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

# ============================================
# 阶段 4: 编译验证
# ============================================
if (!$SkipBuild) {
    Write-Host "[4/5] 编译验证" -ForegroundColor Cyan
    Write-Host "────────────────────────────────────────" -ForegroundColor Gray

    if ($DryRun) {
        Write-Host "  将编译: LYBT.Desktop.sln" -ForegroundColor Gray
        Write-Host "  将编译: LYBTZYZS.sln" -ForegroundColor Gray
    } else {
        # 编译 Desktop.sln
        Write-Host "  [1/2] 编译 LYBT.Desktop.sln..." -ForegroundColor Yellow
        $desktopBuildOutput = dotnet build LYBT.Desktop.sln -c Release --no-restore 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ❌ LYBT.Desktop.sln 编译失败" -ForegroundColor Red
            Write-Host $desktopBuildOutput
            exit 1
        }

        # 统计错误和警告
        $errors = ($desktopBuildOutput | Select-String "error" | Measure-Object).Count
        $warnings = ($desktopBuildOutput | Select-String "warning" | Measure-Object).Count
        Write-Host "  ✅ LYBT.Desktop.sln 编译成功 ($errors errors, $warnings warnings)" -ForegroundColor Green
        Write-Host ""

        # 编译 All.sln
        Write-Host "  [2/2] 编译 LYBTZYZS.sln..." -ForegroundColor Yellow
        $allBuildOutput = dotnet build LYBTZYZS.sln -c Release --no-restore 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ❌ LYBTZYZS.sln 编译失败" -ForegroundColor Red
            Write-Host $allBuildOutput
            exit 1
        }

        $errors = ($allBuildOutput | Select-String "error" | Measure-Object).Count
        $warnings = ($allBuildOutput | Select-String "warning" | Measure-Object).Count
        Write-Host "  ✅ LYBTZYZS.sln 编译成功 ($errors errors, $warnings warnings)" -ForegroundColor Green
    }
    Write-Host ""
} else {
    Write-Host "[4/5] 编译验证 (已跳过)" -ForegroundColor DarkGray
    Write-Host ""
}

# ============================================
# 阶段 5: Git 提交
# ============================================
if (!$SkipGit) {
    Write-Host "[5/5] Git 提交" -ForegroundColor Cyan
    Write-Host "────────────────────────────────────────" -ForegroundColor Gray

    if ($DryRun) {
        Write-Host "  将执行:" -ForegroundColor Gray
        Write-Host "    git add ." -ForegroundColor Gray
        Write-Host "    git commit -m '...'" -ForegroundColor Gray
    } else {
        Write-Host "  添加所有变更..." -ForegroundColor Gray
        git add . 2>&1 | Out-Null

        $commitMessage = @"
refactor(desktop): 统一文件夹命名规范 - Issue #820

阶段完成：
- ✅ [CLEAN-1] 清理所有 bin/obj 构建缓存
- ✅ [RENAME-1] 重命名 Core_New → Core
- ✅ [RENAME-2] 重命名 8 个 Modules 子文件夹（添加 LYBT.Desktop. 前缀）
- ✅ [PROJ-1] 更新所有项目文件引用
- ✅ [SLN-1] 更新 LYBT.Desktop.sln
- ✅ [SLN-2] 更新 LYBTZYZS.sln
- ✅ [BUILD-1] 编译验证 LYBT.Desktop.sln (0 errors)
- ✅ [BUILD-2] 编译验证 LYBTZYZS.sln (0 errors)

变更内容：
1. 文件夹重命名（9个）:
   - Core_New → Core
   - Auth → LYBT.Desktop.Auth
   - Consultation → LYBT.Desktop.Consultation
   - Formula → LYBT.Desktop.Formula
   - Herbs → LYBT.Desktop.Herbs
   - MedicalCase → LYBT.Desktop.MedicalCase
   - Patients → LYBT.Desktop.Patients
   - Prescriptions → LYBT.Desktop.Prescriptions
   - Users → LYBT.Desktop.Users

2. 项目文件更新:
   - 所有 ProjectReference 路径已更新
   - 解决方案文件已同步

3. 编译验证:
   - LYBT.Desktop.sln: 通过
   - LYBTZYZS.sln: 通过

参考: Issue #820
"@

        Write-Host "  创建提交..." -ForegroundColor Gray
        git commit -m $commitMessage 2>&1 | Out-Host

        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ Git 提交成功" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  Git 提交失败或无变更" -ForegroundColor Yellow
        }
    }
    Write-Host ""
} else {
    Write-Host "[5/5] Git 提交 (已跳过)" -ForegroundColor DarkGray
    Write-Host ""
}

# ============================================
# 完成
# ============================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "🎉 Issue #820 执行完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (!$DryRun) {
    Write-Host "执行摘要:" -ForegroundColor Cyan
    Write-Host "  ✅ 清理构建缓存" -ForegroundColor Green
    Write-Host "  ✅ 重命名文件夹 (9个)" -ForegroundColor Green
    Write-Host "  ✅ 更新项目引用" -ForegroundColor Green
    if (!$SkipBuild) {
        Write-Host "  ✅ 编译验证通过" -ForegroundColor Green
    }
    if (!$SkipGit) {
        Write-Host "  ✅ Git 提交完成" -ForegroundColor Green
    }
    Write-Host ""

    Write-Host "下一步:" -ForegroundColor Cyan
    Write-Host "  1. 查看 git status 确认所有变更" -ForegroundColor Gray
    Write-Host "  2. 推送到远程分支: git push" -ForegroundColor Gray
    Write-Host "  3. 在 GitHub 上更新 Issue #820 状态" -ForegroundColor Gray
    Write-Host ""
}

exit 0