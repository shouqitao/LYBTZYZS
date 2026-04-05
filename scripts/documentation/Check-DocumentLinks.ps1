<#
.SYNOPSIS
    Document link checker for LYBTZYZS documentation.

.DESCRIPTION
    Checks markdown files in docs/ for broken internal links.
    Called by the pre-commit hook when markdown files in docs/ are staged.

    When -StagedOnly is specified (or when running via pre-commit hook), only
    the staged markdown files are validated. This ensures the hook never blocks
    a commit due to pre-existing broken links in unrelated files.

    To check ALL docs files (CI/manual audit), omit -StagedOnly.

.PARAMETER FailOnError
    When specified, exits with code 1 if any broken links are found.

.PARAMETER StagedOnly
    When specified, only checks the files currently staged in git.
    The pre-commit hook always passes this flag to avoid penalising commits
    for pre-existing issues in unrelated archived docs.

.EXAMPLE
    # Pre-commit hook usage (only validate staged files):
    pwsh -File scripts/documentation/Check-DocumentLinks.ps1 -FailOnError -StagedOnly

.EXAMPLE
    # Full audit (validate every markdown file under docs/):
    pwsh -File scripts/documentation/Check-DocumentLinks.ps1
#>
param(
    [switch]$FailOnError,
    [switch]$StagedOnly
)

$ErrorActionPreference = 'Stop'
$RepoRoot = git rev-parse --show-toplevel
$DocsRoot = Join-Path $RepoRoot "docs"

# Determine which files to check
if ($StagedOnly) {
    $stagedFiles = git diff --cached --name-only --diff-filter=ACM | Where-Object { $_ -match '\.md$' }
    if (-not $stagedFiles) {
        Write-Host "[OK] No staged markdown files to check." -ForegroundColor Green
        exit 0
    }

    $mdFilesToCheck = $stagedFiles | ForEach-Object {
        $absPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($RepoRoot, $_))
        if (Test-Path $absPath) { Get-Item $absPath } else { $null }
    } | Where-Object { $_ -ne $null }

    Write-Host "Checking links in $($mdFilesToCheck.Count) staged file(s)..." -ForegroundColor Cyan
} else {
    $mdFilesToCheck = Get-ChildItem -Path $DocsRoot -Filter "*.md" -Recurse -File
    Write-Host "Checking document links in $DocsRoot..." -ForegroundColor Cyan
}

$brokenLinks = @()

foreach ($file in $mdFilesToCheck) {
    $content = Get-Content $file.FullName -Raw
    if (-not $content) { continue }

    # Find all markdown links: [text](path)
    $linkMatches = [regex]::Matches($content, '\[([^\]]+)\]\(([^)]+)\)')
    foreach ($match in $linkMatches) {
        $linkTarget = $match.Groups[2].Value

        # Skip external URLs and anchors-only
        if ($linkTarget -match '^https?://' -or $linkTarget -match '^#') {
            continue
        }

        # Strip anchor from path
        $pathPart = $linkTarget -replace '#.*$', ''
        if ([string]::IsNullOrWhiteSpace($pathPart)) { continue }

        # Resolve relative to file's directory
        $fileDir = Split-Path $file.FullName -Parent
        $resolved = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($fileDir, $pathPart))

        if (-not (Test-Path $resolved)) {
            $brokenLinks += [PSCustomObject]@{
                File   = $file.FullName.Replace($RepoRoot, '').TrimStart('\', '/')
                Link   = $linkTarget
                Target = $resolved.Replace($RepoRoot, '').TrimStart('\', '/')
            }
        }
    }
}

if ($brokenLinks.Count -eq 0) {
    Write-Host "[OK] All document links are valid." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "[WARN] Found $($brokenLinks.Count) broken link(s):" -ForegroundColor Yellow
foreach ($broken in $brokenLinks) {
    Write-Host "  $($broken.File): [$($broken.Link)] -> $($broken.Target)" -ForegroundColor Red
}

# Ensure reports directory exists
$reportsDir = Join-Path $RepoRoot "docs\reports"
if (-not (Test-Path $reportsDir)) {
    New-Item -ItemType Directory -Path $reportsDir | Out-Null
}

$reportPath = Join-Path $reportsDir "link-check-report.md"
$report = "# Link Check Report`n`nGenerated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n`n## Broken Links`n`n"
foreach ($broken in $brokenLinks) {
    $report += "- **$($broken.File)**: ``$($broken.Link)`` -> ``$($broken.Target)```n"
}
$report | Set-Content $reportPath

if ($FailOnError) {
    exit 1
}

exit 0
