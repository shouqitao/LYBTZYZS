# Test Runner Script with .runsettings
# Purpose: Run tests with unified configuration
param(
    [string]$Filter = "",
    [bool]$Coverage = $true,
    [bool]$Parallel = $true,
    [string]$Output = "TestResults",
    [ValidateSet("Unit", "Integration", "All")]
    [string]$TestType = "Unit"
)

Write-Host "=== LYBT Test Runner ===" -ForegroundColor Cyan
Write-Host "Test Type: $TestType" -ForegroundColor Gray
Write-Host "Coverage: $Coverage" -ForegroundColor Gray
Write-Host "Parallel: $Parallel" -ForegroundColor Gray
Write-Host ""

# Clean previous results
if (Test-Path $Output) {
    Remove-Item -Recurse -Force $Output
    Write-Host "Cleaned previous test results" -ForegroundColor Yellow
}

# Build solution first
Write-Host "Building solution..." -ForegroundColor Blue
dotnet build LYBT.All.sln --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Construct test command
$testCommand = @("test")

# Add settings file
$testCommand += @("--settings", ".runsettings")

# Add configuration
$testCommand += @("--configuration", "Release")

# Add no-build since we just built
$testCommand += "--no-build"

# Add results directory
$testCommand += @("--results-directory", $Output)

# Add logger
$testCommand += @("--logger", "trx", "--logger", "console;verbosity=normal")

# Add test type filter
switch ($TestType) {
    "Unit" {
        $testCommand += @("--filter", "Category!=Integration&Category!=E2E")
    }
    "Integration" {
        $testCommand += @("--filter", "Category=Integration|Category=E2E")
    }
    "All" {
        # No filter, run all tests
    }
}

# Add custom filter if provided
if ($Filter) {
    if ($TestType -ne "All") {
        # Combine with test type filter
        $combinedFilter = "($Filter) & (Category!=Integration&Category!=E2E)"
        if ($TestType -eq "Integration") {
            $combinedFilter = "($Filter) & (Category=Integration|Category=E2E)"
        }
        $testCommand[-1] = $combinedFilter
    } else {
        $testCommand += @("--filter", $Filter)
    }
}

# Add coverage collection
if ($Coverage) {
    $testCommand += @("--collect", "XPlat Code Coverage")
}

# Set parallel execution
if (-not $Parallel) {
    $testCommand += "--parallel", "off"
}

# Add specific test projects path
$testCommand += "tests/"

Write-Host "Executing: dotnet $($testCommand -join ' ')" -ForegroundColor Blue
Write-Host ""

# Run tests
& dotnet $testCommand

$exitCode = $LASTEXITCODE
Write-Host ""

if ($exitCode -eq 0) {
    Write-Host "✅ Tests completed successfully!" -ForegroundColor Green
    
    # Show coverage results if available
    if ($Coverage) {
        $coverageFiles = Get-ChildItem -Path $Output -Filter "coverage.cobertura.xml" -Recurse
        if ($coverageFiles.Count -gt 0) {
            Write-Host ""
            Write-Host "📊 Coverage reports generated:" -ForegroundColor Blue
            $coverageFiles | ForEach-Object {
                Write-Host "  $($_.FullName)" -ForegroundColor White
            }
        }
    }
    
    # Show test results
    $trxFiles = Get-ChildItem -Path $Output -Filter "*.trx" -Recurse
    if ($trxFiles.Count -gt 0) {
        Write-Host ""
        Write-Host "📋 Test result files:" -ForegroundColor Blue
        $trxFiles | ForEach-Object {
            Write-Host "  $($_.FullName)" -ForegroundColor White
        }
    }
} else {
    Write-Host "❌ Tests failed with exit code: $exitCode" -ForegroundColor Red
}

Write-Host ""
Write-Host "Test results saved to: $Output" -ForegroundColor Blue

exit $exitCode