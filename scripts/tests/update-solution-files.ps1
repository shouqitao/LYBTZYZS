# Update Solution Files Script
# Purpose: Add new test project references to solution files after test reorganization
param(
    [switch]$DryRun = $false
)

Write-Host "=== Solution Files Update ===" -ForegroundColor Cyan
Write-Host "Dry Run: $DryRun" -ForegroundColor Gray
Write-Host ""

# Get all test project files in new structure
$testProjects = @()
Get-ChildItem -Path "tests" -Filter "*.csproj" -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Replace((Get-Location).Path, "").TrimStart("\").Replace("\", "/")
    $testProjects += @{
        Name = $_.BaseName
        Path = $relativePath
        Directory = $_.Directory.Name
        FullPath = $_.FullName
    }
}

Write-Host "Found $($testProjects.Count) test projects:" -ForegroundColor Green
$testProjects | ForEach-Object {
    Write-Host "  $($_.Path)" -ForegroundColor White
}

Write-Host ""

# Solutions to update
$solutions = @("LYBT.All.sln", "LYBT.Server.sln")

foreach ($solutionFile in $solutions) {
    if (-not (Test-Path $solutionFile)) {
        Write-Host "Solution file not found: $solutionFile" -ForegroundColor Red
        continue
    }

    Write-Host "Processing solution: $solutionFile" -ForegroundColor Yellow
    
    # Read solution content
    $solutionContent = Get-Content $solutionFile -Raw
    
    # Find existing test projects in solution
    $existingTestProjects = @()
    $solutionContent -split "`n" | ForEach-Object {
        if ($_ -match 'Project\("{[^}]+}"\) = "([^"]*)", "([^"]*\.csproj)"') {
            $projectName = $matches[1]
            $projectPath = $matches[2]
            if ($projectPath -like "*Test*" -or $projectPath -like "*tests/*") {
                $existingTestProjects += @{
                    Name = $projectName
                    Path = $projectPath
                }
            }
        }
    }
    
    Write-Host "  Existing test projects: $($existingTestProjects.Count)" -ForegroundColor Blue
    
    # Find projects to add (not already in solution)
    $projectsToAdd = @()
    foreach ($testProject in $testProjects) {
        $found = $false
        foreach ($existing in $existingTestProjects) {
            if ($existing.Name -eq $testProject.Name -or $existing.Path -eq $testProject.Path) {
                $found = $true
                break
            }
        }
        if (-not $found) {
            $projectsToAdd += $testProject
        }
    }
    
    Write-Host "  Projects to add: $($projectsToAdd.Count)" -ForegroundColor Green
    $projectsToAdd | ForEach-Object {
        Write-Host "    $($_.Name) -> $($_.Path)" -ForegroundColor White
    }
    
    if ($projectsToAdd.Count -eq 0) {
        Write-Host "  No new projects to add to $solutionFile" -ForegroundColor Gray
        continue
    }
    
    if ($DryRun) {
        Write-Host "  [DRY RUN] Would add $($projectsToAdd.Count) projects to $solutionFile" -ForegroundColor Yellow
        continue
    }
    
    # Add projects to solution using dotnet CLI
    foreach ($project in $projectsToAdd) {
        Write-Host "  Adding: $($project.Name)" -ForegroundColor Green
        try {
            $result = & dotnet sln $solutionFile add $project.FullPath 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "    SUCCESS: Added $($project.Name)" -ForegroundColor Green
            } else {
                Write-Host "    WARNING: Failed to add $($project.Name): $result" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "    ERROR: Exception adding $($project.Name): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "✅ Solution files update completed!" -ForegroundColor Green

if ($DryRun) {
    Write-Host "Run with -DryRun:`$false to execute changes" -ForegroundColor Blue
}