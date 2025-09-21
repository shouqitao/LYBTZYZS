# Clean residual wording and historical terms in module READMEs (src/Server, src/Client)
# - Remove/neutralize branding (UltraThink) and marketing phrases
# - Normalize status labels and layered-architecture terms
# - Skip fenced code blocks

param(
  [string]$Root = "."
)

$ErrorActionPreference = 'Stop'

function In-CodeBlock([string]$line) {
  return ($line -match '^\s*```')
}

function Normalize-Line([string]$line) {
  $text = $line
  # Status labels anywhere (with optional formatting), e.g., **项目状?**: ... or > **模块状?**: ...
  if ($text -match '项目状[态�?][*_\s：:]*[：:]\s*(.*)$') {
    $rest = $Matches[1]
    $rest = $rest -replace '最后更\?', '最后更新'
    $text = ($text -replace '^\s*>\s*', '> ') # ensure blockquote form if it already was
    $text = '> 项目状态: ' + $rest
  }
  if ($text -match '模块状[态�?][*_\s：:]*[：:]\s*(.*)$') {
    $rest = $Matches[1]
    $rest = $rest -replace '最后更\?', '最后更新'
    $text = ($text -replace '^\s*>\s*', '> ')
    $text = '> 模块状态: ' + $rest
  }

  # Branding and marketing phrases
  $text = $text -replace 'UltraThink[\S ]*架构', '分层架构'
  $text = $text -replace 'UltraThink', ''
  $text = $text -replace '企业级', ''
  $text = $text -replace 'A\+[^\s，。]*', '高质量'
  $text = $text -replace '零编译错[误]*', '编译通过'
  $text = $text -replace '([双三])层架构', '分层架构'
  # Collapse duplicate spaces introduced by removals
  $text = $text -replace ' {2,}', ' '
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
    if (In-CodeBlock $line) { $inCode = -not $inCode; continue }
    if (-not $inCode) { $lines[$i] = Normalize-Line $line }
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
  Write-Host 'No module README wording changes needed.'
}

