# Sets repository hooks path to use the versioned .githooks directory

param(
  [string]$HooksPath = '.githooks'
)

$ErrorActionPreference = 'Stop'

Write-Host "Configuring git hooks path to: $HooksPath"
git config core.hooksPath $HooksPath

Write-Host "Done. A pre-commit hook will normalize docs titles/terms."

