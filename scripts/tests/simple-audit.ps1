# Simple Test Code Audit Script
param(
    [string]$RootPath = ".",
    [string]$ReportPath = "_reports/2025-09/backend/test-cleanup"
)

Write-Host "=== Test Code Audit ===" -ForegroundColor Cyan
Write-Host "Root Path: $RootPath" -ForegroundColor Gray
Write-Host ""

# Create report directory
New-Item -ItemType Directory -Path $ReportPath -Force | Out-Null

# 1. Find test projects
Write-Host "Step 1: Finding test projects..." -ForegroundColor Yellow
$testProjects = @()
Get-ChildItem -Path $RootPath -Filter "*.csproj" -Recurse | Where-Object {
    $_.Name -match "Test|Tests"
} | ForEach-Object {
    $relativePath = $_.FullName.Replace((Get-Location).Path, "").TrimStart("\")
    $testProjects += @{
        Name = $_.Name
        Path = $relativePath
        Directory = $_.Directory.Name
    }
    Write-Host "  Found: $relativePath" -ForegroundColor Green
}

# 2. Find test files
Write-Host "Step 2: Finding test files..." -ForegroundColor Yellow
$testFiles = @()
Get-ChildItem -Path $RootPath -Filter "*Test*.cs" -Recurse | Where-Object {
    $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*"
} | ForEach-Object {
    $relativePath = $_.FullName.Replace((Get-Location).Path, "").TrimStart("\")
    $testFiles += @{
        Name = $_.Name
        Path = $relativePath
    }
}

# 3. Find solution files
Write-Host "Step 3: Finding solution files..." -ForegroundColor Yellow
$solutionFiles = @()
Get-ChildItem -Path $RootPath -Filter "*.sln" | ForEach-Object {
    $solutionFiles += $_.Name
    Write-Host "  Solution: $($_.Name)" -ForegroundColor Magenta
}

# Generate migration plan
Write-Host "Step 4: Generating migration plan..." -ForegroundColor Yellow
$migrationPlan = @()
foreach ($project in $testProjects) {
    $projectName = $project.Name -replace "\.csproj$", ""
    $testType = "UnitTests"
    
    if ($projectName -match "Integration|Api") { 
        $testType = "IntegrationTests" 
    }
    
    $cleanName = $projectName -replace "\.Tests?$", "" -replace "Tests?$", ""
    $targetPath = "tests/$cleanName.$testType"
    
    $migrationPlan += @{
        From = $project.Path
        To = $targetPath
        Type = $testType
    }
}

# Output results
Write-Host ""
Write-Host "=== Audit Results ===" -ForegroundColor Cyan
Write-Host "Test Projects: $($testProjects.Count)" -ForegroundColor Green
Write-Host "Test Files: $($testFiles.Count)" -ForegroundColor Green  
Write-Host "Solution Files: $($solutionFiles.Count)" -ForegroundColor Blue

Write-Host ""
Write-Host "Migration Plan:" -ForegroundColor Cyan
foreach ($plan in $migrationPlan) {
    Write-Host "  $($plan.From) -> $($plan.To)" -ForegroundColor Yellow
}

# Save results to JSON
$results = @{
    TestProjects = $testProjects
    TestFiles = $testFiles
    SolutionFiles = $solutionFiles
    MigrationPlan = $migrationPlan
    Timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
}

$jsonFile = "$ReportPath/audit-results.json"
$results | ConvertTo-Json -Depth 5 | Out-File -FilePath $jsonFile -Encoding UTF8

Write-Host ""
Write-Host "Results saved to: $jsonFile" -ForegroundColor Blue
Write-Host "Ready for migration!" -ForegroundColor Green