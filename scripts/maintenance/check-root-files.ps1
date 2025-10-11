#!/usr/bin/env pwsh
<#
.SYNOPSIS
    根目录文件检查脚本 - 确保根目录符合白名单规范

.DESCRIPTION
    扫描项目根目录，对比白名单配置，识别违规文件并生成审计报告。

    返回码：
    - 0: 所有文件符合规范
    - 1: 发现违规文件

.PARAMETER GenerateReport
    是否生成审计报告文件（默认：$true）

.PARAMETER ReportPath
    审计报告保存路径（默认：docs/reports/root-files-audit-{date}.md）

.EXAMPLE
    .\check-root-files.ps1

.EXAMPLE
    .\check-root-files.ps1 -GenerateReport:$false
#>

param(
    [switch]$GenerateReport = $true,
    [string]$ReportPath = ""
)

# 设置错误处理
$ErrorActionPreference = "Stop"

# 颜色输出函数
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# 主逻辑
try {
    # 1. 确定根目录路径
    $rootPath = Resolve-Path (Join-Path $PSScriptRoot "..\..") | Select-Object -ExpandProperty Path

    # 2. 加载白名单配置
    $whitelistPath = Join-Path $rootPath ".config\root-files-whitelist.json"
    if (-not (Test-Path $whitelistPath)) {
        Write-ColorOutput "❌ 错误: 白名单配置文件不存在: $whitelistPath" "Red"
        exit 1
    }

    $whitelist = Get-Content $whitelistPath -Raw -Encoding UTF8 | ConvertFrom-Json
    Write-ColorOutput "✓ 成功加载白名单配置" "Green"

    # 3. 扫描根目录（排除隐藏目录）
    $rootFiles = Get-ChildItem -Path $rootPath -File -Depth 0
    $rootDirs = Get-ChildItem -Path $rootPath -Directory -Depth 0

    Write-ColorOutput "`n📊 根目录扫描结果：" "Cyan"
    Write-ColorOutput "  - 文件总数: $($rootFiles.Count)" "White"
    Write-ColorOutput "  - 目录总数: $($rootDirs.Count)" "White"

    # 4. 检查文件是否符合白名单
    $violations = @()
    $compliant = @()

    foreach ($file in $rootFiles) {
        $fileName = $file.Name
        $isAllowed = $false

        # 检查精确匹配（最高优先级）
        if ($whitelist.allowed_files -contains $fileName) {
            $isAllowed = $true
            $compliant += $fileName
            continue  # 精确白名单，直接跳过后续检查
        }

        # 检查模式匹配
        foreach ($pattern in $whitelist.allowed_patterns) {
            if ($fileName -like $pattern) {
                $isAllowed = $true
                break
            }
        }

        # 检查禁止模式（仅当不在精确白名单时）
        if (-not $isAllowed) {
            foreach ($blockedPattern in $whitelist.blocked_patterns) {
                $cleanPattern = $blockedPattern.TrimStart('/')
                if ($fileName -like $cleanPattern) {
                    $isAllowed = $false
                    break
                }
            }
        }

        if ($isAllowed) {
            $compliant += $fileName
        } else {
            $violations += $fileName
        }
    }

    # 5. 检查目录是否符合白名单
    $dirViolations = @()
    foreach ($dir in $rootDirs) {
        if ($whitelist.allowed_dirs -notcontains $dir.Name) {
            $dirViolations += $dir.Name
        }
    }

    # 6. 输出检查结果
    Write-ColorOutput "`n📋 检查结果：" "Cyan"

    if ($violations.Count -eq 0 -and $dirViolations.Count -eq 0) {
        Write-ColorOutput "  ✅ 所有文件和目录均符合白名单规范" "Green"
    } else {
        Write-ColorOutput "  ❌ 发现违规项" "Red"

        if ($violations.Count -gt 0) {
            Write-ColorOutput "`n  违规文件 ($($violations.Count)个)：" "Yellow"
            foreach ($v in $violations) {
                Write-ColorOutput "    - $v" "Red"
            }
        }

        if ($dirViolations.Count -gt 0) {
            Write-ColorOutput "`n  违规目录 ($($dirViolations.Count)个)：" "Yellow"
            foreach ($d in $dirViolations) {
                Write-ColorOutput "    - $d/" "Red"
            }
        }

        Write-ColorOutput "`n  建议操作：" "Cyan"
        Write-ColorOutput "    1. 文档 → docs/ 对应子目录" "White"
        Write-ColorOutput "    2. 脚本 → scripts/ 对应子目录" "White"
        Write-ColorOutput "    3. 报告 → docs/reports/" "White"
        Write-ColorOutput "    4. 截图 → docs/assets/screenshots/" "White"
        Write-ColorOutput "    5. 配置 → .config/ 或 config/" "White"
    }

    # 7. 生成审计报告
    if ($GenerateReport) {
        $timestamp = Get-Date -Format "yyyy-MM-dd"
        if ([string]::IsNullOrEmpty($ReportPath)) {
            $reportsDir = Join-Path $rootPath "docs\reports"
            if (-not (Test-Path $reportsDir)) {
                New-Item -ItemType Directory -Path $reportsDir -Force | Out-Null
            }
            $ReportPath = Join-Path $reportsDir "root-files-audit-$timestamp.md"
        }

        $statusEmoji = if ($violations.Count -eq 0 -and $dirViolations.Count -eq 0) { "✅" } else { "❌" }
        $statusText = if ($violations.Count -eq 0 -and $dirViolations.Count -eq 0) { "正常" } else { "发现异常" }

        $reportContent = @"
# 根目录文件审计报告

**日期**: $timestamp
**扫描结果**: $statusEmoji $statusText

## 统计信息

- 根目录文件总数: $($rootFiles.Count)个
- 根目录目录总数: $($rootDirs.Count)个
- 白名单文件数: $($whitelist.allowed_files.Count)个
- 白名单模式数: $($whitelist.allowed_patterns.Count)个
- 允许目录数: $($whitelist.allowed_dirs.Count)个
- 违规文件数: $($violations.Count)个
- 违规目录数: $($dirViolations.Count)个

## 文件列表

### ✅ 符合白名单的文件

$(if ($compliant.Count -gt 0) {
    $compliant | ForEach-Object { "- $_" } | Out-String
} else {
    "- 无"
})

### ❌ 违规文件

$(if ($violations.Count -gt 0) {
    $violations | ForEach-Object { "- **$_** ⚠️" } | Out-String
} else {
    "- 无"
})

### ❌ 违规目录

$(if ($dirViolations.Count -gt 0) {
    $dirViolations | ForEach-Object { "- **$_/** ⚠️" } | Out-String
} else {
    "- 无"
})

## 建议操作

$(if ($violations.Count -eq 0 -and $dirViolations.Count -eq 0) {
    "✅ 无需清理，根目录整洁"
} else {
    @"
⚠️ 需要清理以下项：

### 违规文件处理建议
$(if ($violations.Count -gt 0) {
    $violations | ForEach-Object {
        $suggestion = switch -Wildcard ($_) {
            "*.md" { "→ 移至 docs/ 对应子目录" }
            "*.txt" { "→ 移至 docs/reports/ 或删除" }
            "*.ps1" { "→ 移至 scripts/ 对应子目录" }
            "*.sh" { "→ 移至 scripts/ 对应子目录" }
            "*.png" { "→ 移至 docs/assets/screenshots/" }
            "*.jpg" { "→ 移至 docs/assets/screenshots/" }
            "*.json" { "→ 移至 .config/ 或删除" }
            default { "→ 根据类型移至正确位置或删除" }
        }
        "- **$_** $suggestion"
    } | Out-String
} else { "- 无" })

### 违规目录处理建议
$(if ($dirViolations.Count -gt 0) {
    $dirViolations | ForEach-Object { "- **$_/** → 检查是否应该存在，或移至正确位置" } | Out-String
} else { "- 无" })
"@
})

## 白名单配置

**配置文件**: `.config/root-files-whitelist.json`

### 允许的文件
$(($whitelist.allowed_files | ForEach-Object { "- ``$_``" }) -join "`n")

### 允许的模式
$(($whitelist.allowed_patterns | ForEach-Object { "- ``$_``" }) -join "`n")

### 允许的目录
$(($whitelist.allowed_dirs | ForEach-Object { "- ``$_/``" }) -join "`n")

---

🤖 自动生成于 $(Get-Date -Format "yyyy-MM-dd HH:mm:ss") UTC+8
🔧 检查脚本: ``scripts/maintenance/check-root-files.ps1``
"@

        $reportContent | Out-File -FilePath $ReportPath -Encoding UTF8 -Force
        Write-ColorOutput "`n✓ 审计报告已生成: $ReportPath" "Green"
    }

    # 8. 返回码
    if ($violations.Count -gt 0 -or $dirViolations.Count -gt 0) {
        Write-ColorOutput "`n❌ 检查失败：发现违规项" "Red"
        exit 1
    } else {
        Write-ColorOutput "`n✅ 检查通过：根目录整洁" "Green"
        exit 0
    }

} catch {
    Write-ColorOutput "`n❌ 脚本执行错误: $($_.Exception.Message)" "Red"
    Write-ColorOutput $_.ScriptStackTrace "Gray"
    exit 1
}
