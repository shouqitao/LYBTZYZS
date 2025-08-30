# scripts/detect-environment.ps1
param([string]$RulesPath="$PSScriptRoot\..\.\ai\rules.json")
$aiDir = Split-Path $RulesPath -Parent
if (-not (Test-Path $aiDir)) { New-Item -ItemType Directory -Force -Path $aiDir | Out-Null }
if (-not (Test-Path $RulesPath)) {
  $now=(Get-Date).ToUniversalTime().ToString("s")+"Z"
  $default=@{
    project="YOUR_PROJECT_NAME"
    environment=@{os="windows";shell="powershell";pathSeparator="\";lineEnding="CRLF";powershellVersion="7+"}
    conventions=@{scriptFormat="ps1";preferNSSM=$true;apiVersionFormat="v1";encoding="UTF-8-BOM"}
    lastUpdated=$now
  }
  ($default|ConvertTo-Json -Depth 8)|Set-Content -Encoding UTF8 $RulesPath
  Write-Host "[detect] Created default rules.json at $RulesPath"
}
$rules=(Get-Content $RulesPath -Raw)|ConvertFrom-Json
$rules.environment.os="windows"
$rules.environment.shell="powershell"
$rules.environment.pathSeparator="\"
$rules.environment.lineEnding="CRLF"
$rules.environment.powershellVersion=($PSVersionTable.PSVersion.Major.ToString()+"+")
$rules.conventions.scriptFormat="ps1"
$rules.conventions.encoding="UTF-8-BOM"
$rules.lastUpdated=(Get-Date).ToUniversalTime().ToString("s")+"Z"
($rules|ConvertTo-Json -Depth 8)|Set-Content -Encoding UTF8 $RulesPath
Write-Host "[detect] Environment detected and rules.json updated: $RulesPath"
