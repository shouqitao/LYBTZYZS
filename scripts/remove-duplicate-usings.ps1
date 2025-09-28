# PowerShell script: Remove duplicate using statements
# Issue #787: Code cleanup phase 1

Write-Host "Starting bulk removal of duplicate using statements..." -ForegroundColor Cyan

# Namespaces to process (already defined in GlobalUsings.cs)
$namespaces = @(
    'using System;',
    'using System.Collections.Generic;',
    'using System.Linq;',
    'using System.Threading.Tasks;'
)

# Get all CS files (excluding GlobalUsings.cs itself)
$files = Get-ChildItem -Path "D:\source\repos\LYBTZYZS" -Filter "*.cs" -Recurse |
    Where-Object { $_.Name -ne "GlobalUsings.cs" -and
                   $_.FullName -notlike "*\obj\*" -and
                   $_.FullName -notlike "*\bin\*" -and
                   $_.FullName -notlike "*\Migrations\*" }

$totalFiles = $files.Count
$processedFiles = 0
$modifiedFiles = 0

Write-Host "Found $totalFiles CS files" -ForegroundColor Yellow

foreach ($file in $files) {
    $processedFiles++
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $modified = $false

    # Check file path to determine which GlobalUsings to use
    $useGlobalUsings = $false
    if ($file.FullName -like "*\src\Server\*") {
        $useGlobalUsings = $true
    }
    elseif ($file.FullName -like "*\src\Client\Desktop\*") {
        $useGlobalUsings = $true
    }
    elseif ($file.FullName -like "*\src\Shared\*") {
        $useGlobalUsings = $true
    }

    if ($useGlobalUsings) {
        foreach ($ns in $namespaces) {
            if ($content -match [regex]::Escape($ns)) {
                # Comment out instead of delete, so it can be restored
                $replacement = "// $ns // Moved to GlobalUsings.cs"
                $content = $content -replace [regex]::Escape($ns), $replacement
                $modified = $true
            }
        }

        if ($modified) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            $modifiedFiles++
            Write-Progress -Activity "Processing files" -Status "$processedFiles / $totalFiles" -PercentComplete (($processedFiles / $totalFiles) * 100)
        }
    }
}

Write-Host "Processing complete!" -ForegroundColor Green
Write-Host "Total files: $totalFiles" -ForegroundColor Yellow
Write-Host "Modified files: $modifiedFiles" -ForegroundColor Yellow

# Generate report
$report = @"
Duplicate using statements removal report
==========================================
Execution time: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Total files: $totalFiles
Modified files: $modifiedFiles
"@

$reportPath = "D:\source\repos\LYBTZYZS\scripts\using-cleanup-report.txt"
$report | Out-File -FilePath $reportPath
Write-Host "Report saved to: $reportPath" -ForegroundColor Cyan