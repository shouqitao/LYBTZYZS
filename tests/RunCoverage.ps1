# RunCoverage.ps1 - LYBT Test Coverage Script
param(
    [bool]$OpenReport = $true,
    [string]$Configuration = "Release",
    [string]$ResultsDirectory = "BIN\TestResults"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "LYBT Test Coverage Analysis" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Clean previous results
if (Test-Path $ResultsDirectory) {
    Write-Host "Cleaning previous test results..." -ForegroundColor Yellow
    Remove-Item -Path $ResultsDirectory -Recurse -Force
}

# Create results directory
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null

# Set environment variable for test database
$env:TEST_SQLSERVER_CONNSTR = "Server=(localdb)\MSSQLLocalDB;Database=LYBT_Test_$([System.Guid]::NewGuid().ToString('N').Substring(0,8));Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;Encrypt=False"
Write-Host "Test database connection configured: $($env:TEST_SQLSERVER_CONNSTR)" -ForegroundColor Green

# Restore packages
Write-Host "`nRestoring packages..." -ForegroundColor Yellow
dotnet restore LYBT.Server.sln

# Build solution
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build LYBT.Server.sln -c $Configuration --no-restore

# Run tests with coverage
Write-Host "`nRunning tests with code coverage..." -ForegroundColor Yellow
$testProjects = @(
    "tests\UnitTests\Modules\Auth.UnitTests\LYBT.Module.Auth.Tests.csproj",
    "tests\UnitTests\Modules\Users.UnitTests\LYBT.Module.Users.Tests.csproj",
    "tests\UnitTests\Modules\Patients.UnitTests\LYBT.Module.Patients.Tests.csproj",
    "tests\UnitTests\Modules\Consultation.UnitTests\LYBT.Module.Consultation.Tests.csproj",
    "tests\UnitTests\Modules\Prescriptions.UnitTests\LYBT.Module.Prescriptions.Tests.csproj",
    "tests\UnitTests\Modules\Herbs.UnitTests\LYBT.Module.Herbs.Tests.csproj",
    "tests\UnitTests\Modules\Formula.UnitTests\LYBT.Module.Formula.Tests.csproj",
    "tests\UnitTests\Modules\MedicalCase.UnitTests\LYBT.Module.MedicalCase.Tests.csproj",
    "tests\Architecture\LYBT.ArchTests.csproj"
)

$coverageFiles = @()

foreach ($project in $testProjects) {
    if (Test-Path $project) {
        Write-Host "  Testing: $project" -ForegroundColor Cyan
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
        $outputPath = "$ResultsDirectory\$projectName"

        dotnet test $project `
            -c $Configuration `
            --no-build `
            --collect:"XPlat Code Coverage" `
            --results-directory $outputPath `
            --settings .runsettings `
            -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

        # Find coverage file
        $coverageFile = Get-ChildItem -Path $outputPath -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1
        if ($coverageFile) {
            $coverageFiles += $coverageFile.FullName
        }
    }
}

# Check if reportgenerator is installed
$reportGeneratorInstalled = $null -ne (dotnet tool list -g | Where-Object { $_ -match "reportgenerator" })
if (-not $reportGeneratorInstalled) {
    Write-Host "`nInstalling ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# Generate HTML report
if ($coverageFiles.Count -gt 0) {
    Write-Host "`nGenerating coverage report..." -ForegroundColor Yellow
    $reportPath = "$ResultsDirectory\coverage"
    $coverageFilesStr = $coverageFiles -join ";"

    reportgenerator `
        -reports:$coverageFilesStr `
        -targetdir:$reportPath `
        -reporttypes:"Html;Cobertura;Badges" `
        -verbosity:Info

    Write-Host "`n================================================" -ForegroundColor Green
    Write-Host "Coverage report generated successfully!" -ForegroundColor Green
    Write-Host "Report location: $reportPath\index.html" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green

    # Open report if requested
    if ($OpenReport) {
        $fullPath = Resolve-Path "$reportPath\index.html"
        Write-Host "`nOpening coverage report..." -ForegroundColor Cyan
        Start-Process $fullPath
    }

    # Display coverage summary
    $summaryFile = "$reportPath\Summary.txt"
    if (Test-Path $summaryFile) {
        Write-Host "`nCoverage Summary:" -ForegroundColor Cyan
        Get-Content $summaryFile | Write-Host
    }
}
else {
    Write-Host "`nNo coverage files found!" -ForegroundColor Red
    exit 1
}

Write-Host "`nTest coverage analysis completed!" -ForegroundColor Green