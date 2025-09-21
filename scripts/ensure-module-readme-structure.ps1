# Ensure module README structure for src/Server and src/Client modules
# Adds missing sections with placeholders in a consistent order:
#   1) 项目概述  2) 项目结构  3) 技术栈  4) 快速开始

param(
  [string]$Root = "."
)

$ErrorActionPreference = 'Stop'

function Test-HasSection([string]$content, [string]$keyword) {
  return [Regex]::IsMatch($content, "(?m)^##\s*.*$([Regex]::Escape($keyword))")
}

function Add-Section([string]$content, [string]$heading, [string]$body) {
  $nl = "`r`n"
  if ($content -notmatch "${nl}$") { $content += $nl }
  return $content + $nl + $heading + $nl + $body + $nl
}

function Get-ModuleName([string]$path) {
  $dir = Split-Path -Parent $path
  return Split-Path -Leaf $dir
}

$targets = Get-ChildItem -Path $Root -Recurse -File -Filter README.md |
  Where-Object { $_.FullName -match "\\src\\(Server|Client)\\" -and $_.FullName -notmatch "\\.worktrees\\" }

$changed = @()
foreach ($f in $targets) {
  $text = Get-Content -Raw -LiteralPath $f.FullName
  $module = Get-ModuleName $f.FullName
  $updated = $false

  # Mandatory sections and placeholders
  $sections = @(
    @{ key = '项目概述'; heading = '## 🎯 项目概述'; body = "- [待补充] 简要描述 ${module} 的职责、边界及与其他模块关系。" },
    @{ key = '项目结构'; heading = '## 📦 项目结构'; body = "- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。" },
    @{ key = '技术栈';   heading = '## 🛠 技术栈'; body = "- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。" },
    @{ key = '快速开始'; heading = '## 🚀 快速开始'; body = "- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。" }
  )

  foreach ($s in $sections) {
    if (-not (Test-HasSection $text $s.key)) {
      $text = Add-Section $text $s.heading $s.body
      $updated = $true
    }
  }

  if ($updated) {
    [System.IO.File]::WriteAllText($f.FullName, $text, [System.Text.Encoding]::UTF8)
    $changed += $f.FullName
  }
}

if ($changed.Count -gt 0) {
  Write-Host ("Updated files:`n" + ($changed -join "`n"))
} else {
  Write-Host 'All module README files already contain required sections.'
}

