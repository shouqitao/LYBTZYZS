# scripts/apply-fix.ps1
# Apply fixes to .ai/rules.json based on error text and .ai/patterns.json
param(
  [Parameter(Mandatory = $true)][string]$ErrorText,
  [string]$RulesPath = "$PSScriptRoot\..\.\ai\rules.json",
  [string]$PatternsPath = "$PSScriptRoot\..\.\ai\patterns.json",
  [string]$LogPath = "$PSScriptRoot\..\.\ai\change-log.md"
)

if (-not (Test-Path $RulesPath)) { Write-Host "rules.json not found."; exit 1 }
if (-not (Test-Path $PatternsPath)) { Write-Host "patterns.json not found."; exit 1 }

$rules = (Get-Content $RulesPath -Raw) | ConvertFrom-Json
$patterns = (Get-Content $PatternsPath -Raw) | ConvertFrom-Json

$matched = $false

foreach ($p in $patterns) {
  if ($ErrorText -match $p.pattern) {
    $fix = $p.fix
    foreach ($k in $fix.PSObject.Properties.Name) {
      # Support dotted paths like a.b.c
      $pathParts = $k -split "\."
      $obj = $rules
      for ($i = 0; $i -lt $pathParts.Count - 1; $i++) {
        $obj = $obj.($pathParts[$i])
      }
      $obj.($pathParts[-1]) = $fix.$k
    }
    $matched = $true

    # log
    $time = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    Add-Content $LogPath "`n[$time] AUTO-FIX: $($p.note)`n```text`n$ErrorText`n```"
    break
  }
}

if ($matched) {
  $rules.lastUpdated = (Get-Date).ToUniversalTime().ToString("s") + "Z"
  ($rules | ConvertTo-Json -Depth 8) | Set-Content -Encoding UTF8 $RulesPath
  Write-Host "Rules updated."
} else {
  Write-Host "No pattern matched. Consider adding a new rule to patterns.json."
}
