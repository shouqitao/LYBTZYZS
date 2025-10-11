# 文档自动化维护指南

- **维护人**：Claude Code
- **最后更新**：2025-10-11
- **版本**：v1.0

本文档说明文档自动化维护机制的设计、实现和使用方法。

---

## 📋 概述

文档自动化维护包含三个层次：
1. **CI集成检查**：PR提交时自动运行
2. **维护脚本**：按需或定期执行
3. **监控报告**：定期生成文档健康度报告

---

## 🔄 1. CI集成检查

### 当前CI检查（已实现）

| 检查项 | 工作流 | 状态 |
|-------|--------|------|
| Claude Code自动审查 | `.github/workflows/pr-review.yml` | ✅ 已实现 |
| 架构合规测试 | `.github/workflows/pr-review.yml` | ✅ 已实现 |
| 根目录文件检查 | `.github/workflows/root-directory-guard.yml` | ✅ 已实现 |

### 建议新增CI检查

#### 1.1 Markdown格式检查

**工作流**：`.github/workflows/doc-quality-check.yml`

```yaml
name: 文档质量检查

on:
  pull_request:
    paths:
      - 'docs/**/*.md'
      - '*.md'

jobs:
  markdown-lint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Markdown Lint
        uses: DavidAnson/markdownlint-cli2-action@v11
        with:
          globs: |
            docs/**/*.md
            *.md
          config: .markdownlint.json

  link-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: 链接有效性检查
        uses: gaurav-nelson/github-action-markdown-link-check@v1
        with:
          use-quiet-mode: 'yes'
          config-file: '.markdown-link-check.json'
```

**配置文件**：`.markdownlint.json`
```json
{
  "default": true,
  "MD013": false,
  "MD033": false,
  "MD041": false
}
```

**配置文件**：`.markdown-link-check.json`
```json
{
  "ignorePatterns": [
    {
      "pattern": "^http://localhost"
    }
  ],
  "timeout": "20s",
  "retryOn429": true,
  "retryCount": 3
}
```

#### 1.2 索引完整性检查

**脚本**：`scripts/check-index-completeness.ps1`

```powershell
# 检查文档是否都被索引
$docsPath = "docs"
$indexFiles = @(
    "docs/index.md",
    "docs/architecture/README.md",
    "docs/development/README.md",
    "docs/reports/INDEX.md"
)

$allDocs = Get-ChildItem -Path $docsPath -Recurse -Filter "*.md" |
    Where-Object { $_.Name -ne "README.md" -and $_.Name -ne "INDEX.md" }

$indexedDocs = @()
foreach ($indexFile in $indexFiles) {
    $content = Get-Content $indexFile -Raw
    $indexedDocs += $content
}

$unindexedDocs = $allDocs | Where-Object {
    $relativePath = $_.FullName.Replace((Get-Location).Path + "\", "").Replace("\", "/")
    $indexedDocs -notcontains $relativePath
}

if ($unindexedDocs.Count -gt 0) {
    Write-Error "发现 $($unindexedDocs.Count) 个未被索引的文档"
    $unindexedDocs | ForEach-Object { Write-Host "  - $($_.FullName)" }
    exit 1
} else {
    Write-Host "✅ 所有文档已被索引"
    exit 0
}
```

**工作流集成**：
```yaml
  index-check:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: 索引完整性检查
        run: .\scripts\check-index-completeness.ps1
```

#### 1.3 文件编码检查

**脚本**：`scripts/check-file-encoding.ps1`

```powershell
# 检查文件编码是否为UTF-8 with BOM
$files = Get-ChildItem -Path "docs" -Recurse -Filter "*.md"
$invalidFiles = @()

foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)

    # UTF-8 with BOM: EF BB BF
    if ($bytes.Length -lt 3 -or
        $bytes[0] -ne 0xEF -or
        $bytes[1] -ne 0xBB -or
        $bytes[2] -ne 0xBF) {
        $invalidFiles += $file
    }
}

if ($invalidFiles.Count -gt 0) {
    Write-Error "发现 $($invalidFiles.Count) 个编码不正确的文件"
    $invalidFiles | ForEach-Object { Write-Host "  - $($_.FullName)" }
    exit 1
} else {
    Write-Host "✅ 所有文件编码正确"
    exit 0
}
```

---

## 🛠️ 2. 维护脚本

### 2.1 死链检测脚本

**脚本**：`scripts/check-dead-links.ps1`

```powershell
<#
.SYNOPSIS
    检测Markdown文档中的死链

.DESCRIPTION
    扫描指定目录下所有Markdown文件，提取链接并验证有效性

.PARAMETER Path
    要扫描的目录路径

.PARAMETER OutputReport
    输出报告文件路径（可选）

.EXAMPLE
    .\scripts\check-dead-links.ps1 -Path docs/

.EXAMPLE
    .\scripts\check-dead-links.ps1 -Path docs/ -OutputReport dead-links-report.txt
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Path,

    [Parameter(Mandatory=$false)]
    [string]$OutputReport
)

function Test-LocalLink {
    param([string]$link, [string]$basePath)

    # 处理相对路径
    $fullPath = Join-Path $basePath $link
    return Test-Path $fullPath
}

function Test-WebLink {
    param([string]$url)

    try {
        $response = Invoke-WebRequest -Uri $url -Method Head -TimeoutSec 5
        return $response.StatusCode -eq 200
    } catch {
        return $false
    }
}

# 扫描Markdown文件
$files = Get-ChildItem -Path $Path -Recurse -Filter "*.md"
$deadLinks = @()

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # 提取Markdown链接 [text](url)
    $linkPattern = '\[([^\]]+)\]\(([^)]+)\)'
    $matches = [regex]::Matches($content, $linkPattern)

    foreach ($match in $matches) {
        $linkText = $match.Groups[1].Value
        $linkUrl = $match.Groups[2].Value

        # 跳过锚点和代码片段
        if ($linkUrl.StartsWith('#') -or $linkUrl.StartsWith('http://localhost')) {
            continue
        }

        $isValid = $false

        if ($linkUrl.StartsWith('http://') -or $linkUrl.StartsWith('https://')) {
            # Web链接
            $isValid = Test-WebLink $linkUrl
        } else {
            # 本地文件链接
            $basePath = Split-Path $file.FullName -Parent
            $isValid = Test-LocalLink $linkUrl $basePath
        }

        if (-not $isValid) {
            $deadLinks += [PSCustomObject]@{
                File = $file.FullName.Replace((Get-Location).Path + "\", "")
                LinkText = $linkText
                LinkUrl = $linkUrl
            }
        }
    }
}

# 输出结果
if ($deadLinks.Count -eq 0) {
    Write-Host "✅ 未发现死链" -ForegroundColor Green
} else {
    Write-Host "❌ 发现 $($deadLinks.Count) 个死链：" -ForegroundColor Red
    $deadLinks | Format-Table -AutoSize

    if ($OutputReport) {
        $deadLinks | Export-Csv -Path $OutputReport -NoTypeInformation -Encoding UTF8
        Write-Host "报告已保存到: $OutputReport"
    }

    exit 1
}
```

### 2.2 文档统计脚本

**脚本**：`scripts/doc-stats.ps1`

```powershell
<#
.SYNOPSIS
    生成文档统计报告

.DESCRIPTION
    统计文档数量、行数、更新时间等信息

.PARAMETER Output
    输出CSV文件路径

.EXAMPLE
    .\scripts\doc-stats.ps1 -Output docs/reports/doc-stats.csv
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Output
)

$docsPath = "docs"
$stats = @()

Get-ChildItem -Path $docsPath -Recurse -Filter "*.md" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $lines = (Get-Content $_.FullName).Count

    # 提取维护人
    $maintainerMatch = [regex]::Match($content, '\*\*维护人\*\*[：:]\s*([^\r\n]+)')
    $maintainer = if ($maintainerMatch.Success) { $maintainerMatch.Groups[1].Value.Trim() } else { "未知" }

    # 提取最后更新日期
    $updateMatch = [regex]::Match($content, '\*\*最后更新\*\*[：:]\s*(\d{4}-\d{2}-\d{2})')
    $lastUpdate = if ($updateMatch.Success) { $updateMatch.Groups[1].Value } else { "未知" }

    # 提取版本号
    $versionMatch = [regex]::Match($content, '\*\*版本\*\*[：:]\s*([^\r\n]+)')
    $version = if ($versionMatch.Success) { $versionMatch.Groups[1].Value.Trim() } else { "未知" }

    $stats += [PSCustomObject]@{
        文件路径 = $_.FullName.Replace((Get-Location).Path + "\", "")
        文件大小KB = [math]::Round($_.Length / 1KB, 2)
        行数 = $lines
        维护人 = $maintainer
        最后更新 = $lastUpdate
        版本 = $version
        文件修改时间 = $_.LastWriteTime.ToString("yyyy-MM-dd")
    }
}

# 导出统计
$stats | Export-Csv -Path $Output -NoTypeInformation -Encoding UTF8

# 生成摘要
$summary = @"
# 文档统计摘要

- 总文档数: $($stats.Count)
- 总行数: $($stats | Measure-Object -Property 行数 -Sum | Select-Object -ExpandProperty Sum)
- 总大小: $([math]::Round(($stats | Measure-Object -Property 文件大小KB -Sum | Select-Object -ExpandProperty Sum), 2)) KB
- 生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## 维护人分布
$($stats | Group-Object -Property 维护人 | Select-Object Name, Count | Format-Table -AutoSize | Out-String)

## 最近更新
$($stats | Sort-Object -Property 最后更新 -Descending | Select-Object -First 10 | Format-Table 文件路径, 最后更新, 维护人 -AutoSize | Out-String)
"@

Write-Host $summary
Write-Host "✅ 详细统计已保存到: $Output" -ForegroundColor Green
```

### 2.3 索引同步脚本

**脚本**：`scripts/sync-index.ps1`

```powershell
<#
.SYNOPSIS
    同步文档索引

.DESCRIPTION
    扫描指定目录，自动更新索引文件

.PARAMETER Scan
    要扫描的目录

.PARAMETER Index
    索引文件路径

.EXAMPLE
    .\scripts\sync-index.ps1 -Scan docs/development/ -Index docs/development/README.md
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Scan,

    [Parameter(Mandatory=$true)]
    [string]$Index
)

Write-Host "扫描目录: $Scan"
Write-Host "索引文件: $Index"

# 扫描Markdown文件
$files = Get-ChildItem -Path $Scan -Filter "*.md" |
    Where-Object { $_.Name -ne "README.md" -and $_.Name -ne "INDEX.md" }

# 生成索引条目
$entries = @()
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # 提取标题
    $titleMatch = [regex]::Match($content, '^#\s+(.+)$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $title = if ($titleMatch.Success) { $titleMatch.Groups[1].Value.Trim() } else { $file.BaseName }

    # 提取描述（第一段非标题文本）
    $descMatch = [regex]::Match($content, '^(?!#)(.+)$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $description = if ($descMatch.Success) { $descMatch.Groups[1].Value.Trim().Substring(0, [Math]::Min(50, $descMatch.Groups[1].Value.Trim().Length)) + "..." } else { "无描述" }

    $entries += "| [$title]($($file.Name)) | $description |"
}

# 生成索引内容
$indexContent = @"
# 索引

| 文档 | 说明 |
|------|------|
$($entries -join "`n")
"@

Write-Host "✅ 已生成 $($entries.Count) 个索引条目"
Write-Host "`n预览:"
Write-Host $indexContent

# 询问是否更新
$confirm = Read-Host "`n是否更新索引文件？(Y/N)"
if ($confirm -eq 'Y' -or $confirm -eq 'y') {
    Set-Content -Path $Index -Value $indexContent -Encoding UTF8
    Write-Host "✅ 索引已更新: $Index" -ForegroundColor Green
} else {
    Write-Host "⏭️  已取消" -ForegroundColor Yellow
}
```

---

## 📊 3. 监控报告

### 3.1 文档健康度报告（季度）

**脚本**：`scripts/generate-health-report.ps1`

```powershell
<#
.SYNOPSIS
    生成文档健康度报告

.DESCRIPTION
    分析文档质量、更新频率、索引完整性等指标

.PARAMETER OutputDir
    输出目录

.EXAMPLE
    .\scripts\generate-health-report.ps1 -OutputDir docs/reports/
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$OutputDir
)

$reportFile = Join-Path $OutputDir "doc-health-report-$(Get-Date -Format 'yyyy-MM-dd').md"

# 1. 文档数量统计
$totalDocs = (Get-ChildItem -Path "docs" -Recurse -Filter "*.md").Count

# 2. 更新时效性分析
$outdatedDocs = Get-ChildItem -Path "docs" -Recurse -Filter "*.md" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddMonths(-3) }

# 3. 索引完整性检查
# （调用check-index-completeness.ps1）

# 4. 链接有效性检查
# （调用check-dead-links.ps1）

# 5. 生成报告
$report = @"
# 文档健康度报告

**生成时间**：$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## 📊 总体统计

- 总文档数：$totalDocs
- 过期文档数（>3个月未更新）：$($outdatedDocs.Count)
- 健康度评分：$([math]::Round((1 - ($outdatedDocs.Count / $totalDocs)) * 100, 2))%

## 🔍 详细分析

### 更新时效性
$($outdatedDocs | Select-Object -First 10 | ForEach-Object { "- $($_.FullName.Replace((Get-Location).Path + "\", "")) (最后更新: $($_.LastWriteTime.ToString("yyyy-MM-dd")))" } | Out-String)

### 建议改进

1. 归档或更新超过3个月未更新的文档
2. 定期运行链接检查，修复死链
3. 确保新增文档及时更新索引

## 📋 下一步行动

- [ ] 归档过期文档
- [ ] 修复死链
- [ ] 更新索引
"@

Set-Content -Path $reportFile -Value $report -Encoding UTF8
Write-Host "✅ 报告已生成: $reportFile" -ForegroundColor Green
```

---

## 📅 4. 使用场景和频率

| 场景 | 工具 | 频率 | 触发方式 |
|------|------|------|---------|
| PR提交 | CI自动检查 | 每次PR | 自动触发 |
| 文档新增 | 索引同步脚本 | 按需 | 手动运行 |
| 链接检查 | 死链检测脚本 | 每周 | 定时任务 |
| 文档统计 | 统计脚本 | 每月 | 手动运行 |
| 健康度报告 | 健康度脚本 | 每季度 | 手动运行 |

---

## 🚀 5. 实施计划

### Phase 1: CI基础检查（优先级：高）
- [ ] 实现Markdown lint检查
- [ ] 实现链接有效性检查
- [ ] 实现索引完整性检查
- [ ] 实现文件编码检查

### Phase 2: 维护脚本（优先级：中）
- [ ] 创建死链检测脚本
- [ ] 创建文档统计脚本
- [ ] 创建索引同步脚本

### Phase 3: 监控报告（优先级：低）
- [ ] 创建健康度报告脚本
- [ ] 建立定期报告机制

---

## 📚 参考资料

- [文档编写与维护指南](documentation-guidelines.md)
- [文档质量检查清单](documentation-quality-checklist.md)
- [GitHub Actions文档](https://docs.github.com/en/actions)
- [PowerShell文档](https://learn.microsoft.com/en-us/powershell/)

---

🤖 最后更新：Phase 3 - 文档治理规则建立
