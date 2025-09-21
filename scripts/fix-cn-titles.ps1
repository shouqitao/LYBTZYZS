# Fix common Chinese title patterns in Markdown headers to improve readability
# - Inserts separators between concatenated section names
# - Fixes common mojibake (e.g., 项目状? -> 项目状态, 项目结? -> 项目结构)

param(
  [string]$Root = "."
)

$ErrorActionPreference = 'Stop'

function Fix-Line($line) {
  # Only process Markdown headers
  if ($line -notmatch '^(#){1,6}\s') { return $line }

  $replacements = @{
    '项目状\?' = '项目状态'
    '项目结\?' = '项目结构'
    '文档目\?' = '文档目录'
  }

  foreach ($k in $replacements.Keys) { $line = [Regex]::Replace($line, $k, $replacements[$k]) }

  # Concatenated titles → add separators
  $line = $line -replace '项目概述项目状态项目结构', '项目概述 · 项目状态 · 项目结构'
  $line = $line -replace '项目概述项目状态', '项目概述 · 项目状态'
  $line = $line -replace '项目概述项目结构', '项目概述 · 项目结构'
  $line = $line -replace '项目状态项目结构', '项目状态 · 项目结构'
  $line = $line -replace '文档目录开发指南架构设计', '文档目录 · 开发指南 · 架构设计'
  $line = $line -replace '代码风格命名约定', '代码风格 · 命名约定'
  $line = $line -replace '构建与运行测试指南', '构建与运行 · 测试指南'

  return $line
}

$files = Get-ChildItem -Path $Root -Recurse -File -Include *.md |
  Where-Object { $_.FullName -notmatch "\\BIN\\|\\bin\\|\\.git\\" }

$changed = @()
foreach ($f in $files) {
  $orig = Get-Content -Raw -LiteralPath $f.FullName
  $lines = $orig -split "\r?\n"
  $new = ($lines | ForEach-Object { Fix-Line $_ }) -join "`r`n"
  if ($new -ne $orig) {
    [System.IO.File]::WriteAllText($f.FullName, $new, [System.Text.Encoding]::UTF8)
    $changed += $f.FullName
  }
}

if ($changed.Count -gt 0) {
  Write-Host ("Updated files:`n" + ($changed -join "`n"))
} else {
  Write-Host "No header changes needed."
}
