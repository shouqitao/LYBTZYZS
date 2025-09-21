# Unify titles and terminology across module READMEs under src/Server and src/Client
# - Standardizes common headings
# - Normalizes project status blockquote line
# - Cleans historical/legacy wording and mojibake for key terms
# - Skips fenced code blocks

param(
  [string]$Root = "."
)

$ErrorActionPreference = 'Stop'

function Normalize-Heading($line) {
  # Force H2 for key sections
  if ($line -match '^(#){1,6}\s*(.*)$') {
    $text = $Matches[2]
    # Remove emojis at start for comparison
    $plain = $text -replace '^[\p{So}\p{Sc}\p{Sk}\p{Sm}\p{Sk}\s]+',''
    switch -Regex ($plain) {
      '^(项目概述|概述|简介)$'              { return '## 🎯 项目概述' }
      '^(项目结构|项目结构分析)$'           { return '## 📦 项目结构' }
      '^(技术栈|技术框架|技术栈与框架)$'     { return '## 🛠 技术栈' }
      '^(模块|业务模块|功能模块)$'           { return '## 🧩 模块' }
      '^(快速开始|开始使用|使用说明)$'       { return '## 🚀 快速开始' }
      '^(API接口|API 接口|接口说明)$'        { return '## 🔌 API 接口' }
      default { return $line }
    }
  }
  return $line
}

function Normalize-StatusLine($line) {
  # Match blockquote line that contains 项目状态 with optional formatting and colon
  if ($line -match '^>\s*.*项目状[态�?][*_\s：:]*[：:]\s*(.*)$') {
    $rest = $Matches[1]
    # Fix common mojibake inside the remainder
    $rest = $rest -replace '最后更\?', '最后更新'
    return '> 项目状态: ' + $rest
  }
  return $line
}

function Normalize-Terms($line) {
  # Skip obvious headings handled elsewhere
  $text = $line
  # Mojibake fixes
  $text = $text -replace '项目状\?', '项目状态'
  $text = $text -replace '项目结\?', '项目结构'

  # Key terms (outside code blocks)
  $map = @{
    '\bController\b'    = '控制器（Controller）'
    '\bService\b'       = '服务（Service）'
    '\bRepository\b'    = '仓储（Repository）'
    '\bInfrastructure\b'= '基础设施（Infrastructure）'
    '\bEntity\b'        = '实体（Entity）'
    '\bDTOs?\b'         = '数据传输对象（DTO）'
    '依赖注入(?!（)'       = '依赖注入（DI）'
  }
  foreach ($k in $map.Keys) { $text = [Regex]::Replace($text, $k, $map[$k]) }
  return $text
}

$targets = Get-ChildItem -Path $Root -Recurse -File -Filter README.md |
  Where-Object { $_.FullName -match '\\src\\(Server|Client)\\' -and $_.FullName -notmatch '\\.worktrees\\' }

$changed = @()
foreach ($f in $targets) {
  $orig = Get-Content -Raw -LiteralPath $f.FullName
  $lines = $orig -split "\r?\n"
  $inCode = $false
  for ($i=0; $i -lt $lines.Length; $i++) {
    $line = $lines[$i]
    if ($line -match '^\s*```') { $inCode = -not $inCode; $lines[$i] = $line; continue }
    if (-not $inCode) {
      $line = Normalize-Heading $line
      $line = Normalize-StatusLine $line
      $line = Normalize-Terms $line
      $lines[$i] = $line
    }
  }
  $new = ($lines -join "`r`n")
  if ($new -ne $orig) {
    [System.IO.File]::WriteAllText($f.FullName, $new, [System.Text.Encoding]::UTF8)
    $changed += $f.FullName
  }
}

if ($changed.Count -gt 0) {
  Write-Host ("Updated files:`n" + ($changed -join "`n"))
} else {
  Write-Host 'No module README changes needed.'
}
