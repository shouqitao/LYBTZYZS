# Test Code Organization Script
# Purpose: Organize and standardize test project structure
param(
    [switch]$DryRun = $true,
    [string]$BackupBranch = "backup/test-cleanup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
)

Write-Host "=== Test Code Organization ===" -ForegroundColor Cyan
Write-Host "Dry Run: $DryRun" -ForegroundColor Gray
Write-Host ""

# Load audit results
$auditFile = "_reports/2025-09/backend/test-cleanup/audit-results.json"
if (-not (Test-Path $auditFile)) {
    Write-Host "ERROR: Audit results not found. Please run audit-tests.ps1 first." -ForegroundColor Red
    exit 1
}

$auditData = Get-Content $auditFile | ConvertFrom-Json

Write-Host "Loaded audit data:" -ForegroundColor Green
Write-Host "  Test Projects: $($auditData.TestProjects.Count)" -ForegroundColor Blue
Write-Host "  Test Files: $($auditData.TestFiles.Count)" -ForegroundColor Blue

# Create backup branch if not dry run
if (-not $DryRun) {
    Write-Host ""
    Write-Host "Creating backup branch: $BackupBranch" -ForegroundColor Yellow
    git checkout -b $BackupBranch
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to create backup branch" -ForegroundColor Red
        exit 1
    }
    git checkout -
}

# Define new test structure
$newStructure = @{
    "UnitTests" = @{
        "Core" = @()
        "Modules" = @()
        "Infrastructure" = @()
        "Shared" = @()
    }
    "IntegrationTests" = @{
        "API" = @()
        "Workflow" = @()
    }
    "ArchitectureTests" = @()
    "TestUtilities" = @{
        "Builders" = @()
        "Helpers" = @()
    }
    "Archive" = @()  # For deprecated tests
}

# Categorize projects
Write-Host ""
Write-Host "Categorizing test projects..." -ForegroundColor Yellow

$migrations = @()

foreach ($project in $auditData.TestProjects) {
    $currentPath = $project.Path
    $projectName = $project.Name -replace "\.csproj$", ""
    
    # Skip archived tests for now
    if ($currentPath -like "tests-archive*") {
        Write-Host "  SKIP (archived): $currentPath" -ForegroundColor Gray
        continue
    }
    
    # Determine new location based on project type
    $newPath = $null
    $category = "Unknown"
    
    switch -Regex ($projectName) {
        "ArchTests|Architecture" {
            $newPath = "tests/Architecture/$projectName"
            $category = "Architecture"
        }
        "Module\..*\.Tests|LYBT\.Module\." {
            $moduleName = if ($projectName -match "Module\.(.+?)\.Tests") { $matches[1] } else { $projectName }
            $newPath = "tests/UnitTests/Modules/$moduleName.UnitTests"
            $category = "Module Unit Tests"
        }
        "WebAPI.*Tests|.*Integration" {
            $newPath = "tests/IntegrationTests/API/$projectName"
            $category = "Integration Tests"
        }
        "Infrastructure" {
            $newPath = "tests/UnitTests/Infrastructure/$projectName"
            $category = "Infrastructure Tests"
        }
        "TestBase|TestUtilities|TestDataFactory" {
            $newPath = "tests/TestUtilities/$projectName"
            $category = "Test Utilities"
        }
        "UltraThink|TestInfrastructure" {
            $newPath = "tests/TestUtilities/Infrastructure/$projectName"
            $category = "Test Infrastructure"
        }
        "Shared\.Models|Core" {
            $newPath = "tests/UnitTests/Core/$projectName"
            $category = "Core Tests"
        }
        default {
            $newPath = "tests/UnitTests/Other/$projectName"
            $category = "Other"
        }
    }
    
    $migrations += @{
        From = $currentPath
        To = $newPath
        Category = $category
        ProjectName = $projectName
    }
    
    Write-Host "  $category`: $currentPath -> $newPath" -ForegroundColor Green
}

# Show migration plan
Write-Host ""
Write-Host "=== Migration Plan ===" -ForegroundColor Cyan
$migrations | Group-Object Category | ForEach-Object {
    Write-Host "$($_.Name) ($($_.Count) projects):" -ForegroundColor Yellow
    $_.Group | ForEach-Object {
        Write-Host "  $($_.From) -> $($_.To)" -ForegroundColor White
    }
    Write-Host ""
}

if ($DryRun) {
    Write-Host "DRY RUN MODE - No changes made" -ForegroundColor Yellow
    Write-Host "Run with -DryRun:`$false to execute migration" -ForegroundColor Blue
    return
}

# Execute migration
Write-Host "=== Executing Migration ===" -ForegroundColor Cyan

foreach ($migration in $migrations) {
    $fromPath = $migration.From
    $toPath = $migration.To
    
    Write-Host "Moving: $fromPath -> $toPath" -ForegroundColor Blue
    
    # Create target directory
    $targetDir = Split-Path $toPath -Parent
    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        Write-Host "  Created directory: $targetDir" -ForegroundColor Green
    }
    
    # Move with git mv to preserve history
    $sourceDir = Split-Path $fromPath -Parent
    try {
        git mv $sourceDir $toPath
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  SUCCESS: git mv completed" -ForegroundColor Green
        } else {
            Write-Host "  WARNING: git mv failed, using regular move" -ForegroundColor Yellow
            Move-Item -Path $sourceDir -Destination $toPath -Force
        }
    }
    catch {
        Write-Host "  ERROR: Failed to move $fromPath" -ForegroundColor Red
        Write-Host "    $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Clean up empty directories
Write-Host ""
Write-Host "Cleaning up empty directories..." -ForegroundColor Yellow
$emptyDirs = Get-ChildItem -Path "tests" -Directory -Recurse | Where-Object {
    (Get-ChildItem $_.FullName -Force | Measure-Object).Count -eq 0
}

foreach ($dir in $emptyDirs) {
    Write-Host "  Removing empty directory: $($dir.FullName)" -ForegroundColor Gray
    Remove-Item $dir.FullName -Force
}

Write-Host ""
Write-Host "✅ Migration completed!" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Blue
Write-Host "  1. Update solution files (.sln)" -ForegroundColor White
Write-Host "  2. Create .runsettings configuration" -ForegroundColor White
Write-Host "  3. Update CI/CD configuration" -ForegroundColor White
Write-Host "  4. Test the new structure" -ForegroundColor White