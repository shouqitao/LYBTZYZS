# Claude Skills 符号链接设置脚本
# 用途：将项目Skills目录符号链接到Claude全局目录
# 使用：以管理员权限运行PowerShell，执行此脚本

param(
    [switch]$Force
)

# 设置错误处理
$ErrorActionPreference = "Stop"

# 定义路径
$projectSkills = "D:\source\repos\LYBTZYZS\.claude\skills"
$globalSkills = "$env:USERPROFILE\.claude\skills"

Write-Host "=== Claude Skills 符号链接设置 ===" -ForegroundColor Cyan
Write-Host ""

# 检查管理员权限
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "错误：需要管理员权限才能创建符号链接" -ForegroundColor Red
    Write-Host "请以管理员身份运行PowerShell，然后重新执行此脚本" -ForegroundColor Yellow
    exit 1
}

# 检查项目Skills目录是否存在
if (-not (Test-Path $projectSkills)) {
    Write-Host "错误：项目Skills目录不存在：$projectSkills" -ForegroundColor Red
    exit 1
}

Write-Host "项目Skills目录：$projectSkills" -ForegroundColor Green

# 确保全局目录存在
if (-not (Test-Path $globalSkills)) {
    Write-Host "创建全局Skills目录：$globalSkills" -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $globalSkills | Out-Null
}

Write-Host "全局Skills目录：$globalSkills" -ForegroundColor Green
Write-Host ""

# 获取所有Skill目录
$skillDirs = Get-ChildItem $projectSkills -Directory

if ($skillDirs.Count -eq 0) {
    Write-Host "警告：项目Skills目录中没有Skill" -ForegroundColor Yellow
    Write-Host "请先创建Skill，然后重新运行此脚本" -ForegroundColor Yellow
    exit 0
}

Write-Host "发现 $($skillDirs.Count) 个Skill：" -ForegroundColor Cyan
$skillDirs | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor White }
Write-Host ""

# 为每个Skill创建符号链接
$successCount = 0
$skipCount = 0
$errorCount = 0

foreach ($skillDir in $skillDirs) {
    $linkPath = Join-Path $globalSkills $skillDir.Name
    $targetPath = $skillDir.FullName

    # 检查链接是否已存在
    if (Test-Path $linkPath) {
        $existingTarget = (Get-Item $linkPath).Target

        if ($existingTarget -eq $targetPath) {
            Write-Host "[跳过] $($skillDir.Name) - 符号链接已存在且正确" -ForegroundColor Gray
            $skipCount++
            continue
        }

        if ($Force) {
            Write-Host "[删除] $($skillDir.Name) - 删除旧的符号链接" -ForegroundColor Yellow
            Remove-Item $linkPath -Force
        } else {
            Write-Host "[跳过] $($skillDir.Name) - 符号链接已存在但目标不同" -ForegroundColor Yellow
            Write-Host "        现有目标：$existingTarget" -ForegroundColor Gray
            Write-Host "        期望目标：$targetPath" -ForegroundColor Gray
            Write-Host "        使用 -Force 参数强制更新" -ForegroundColor Gray
            $skipCount++
            continue
        }
    }

    # 创建符号链接
    try {
        New-Item -ItemType SymbolicLink -Path $linkPath -Target $targetPath -Force | Out-Null
        Write-Host "[成功] $($skillDir.Name) - 符号链接已创建" -ForegroundColor Green
        $successCount++
    } catch {
        Write-Host "[失败] $($skillDir.Name) - 创建符号链接失败：$($_.Exception.Message)" -ForegroundColor Red
        $errorCount++
    }
}

# 显示结果统计
Write-Host ""
Write-Host "=== 执行结果 ===" -ForegroundColor Cyan
Write-Host "成功：$successCount" -ForegroundColor Green
Write-Host "跳过：$skipCount" -ForegroundColor Gray
Write-Host "失败：$errorCount" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Gray" })
Write-Host ""

if ($successCount -gt 0) {
    Write-Host "符号链接已创建，Claude Code将自动加载这些Skills" -ForegroundColor Green
}

if ($errorCount -gt 0) {
    Write-Host "警告：部分符号链接创建失败，请检查错误信息" -ForegroundColor Yellow
    exit 1
}

Write-Host "提示：如需更新Skills，直接编辑项目目录中的SKILL.md文件，修改会自动同步" -ForegroundColor Cyan
