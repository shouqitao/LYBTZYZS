# PowerShell script to run WebAPI unit tests and generate coverage report
# Usage: .\RunTests.ps1

param(
    [string]$Configuration = "Debug",
    [switch]$GenerateCoverage = $true,
    [switch]$OpenReport = $true
)

Write-Host "开始运行 WebAPI 单元测试..." -ForegroundColor Green

# 获取项目根目录
$ProjectRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$TestProject = $PSScriptRoot
$OutputDir = Join-Path $TestProject "TestResults"

# 确保输出目录存在
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

Write-Host "项目根目录: $ProjectRoot" -ForegroundColor Yellow
Write-Host "测试项目目录: $TestProject" -ForegroundColor Yellow
Write-Host "输出目录: $OutputDir" -ForegroundColor Yellow

try {
    # 运行测试
    if ($GenerateCoverage) {
        Write-Host "运行测试并生成覆盖率报告..." -ForegroundColor Cyan

        $CoverageFile = Join-Path $OutputDir "coverage.cobertura.xml"
        $TestResultsFile = Join-Path $OutputDir "test-results.trx"

        # 使用 dotnet test 运行测试并收集覆盖率
        & dotnet test "$TestProject" `
            --configuration $Configuration `
            --logger "trx;LogFileName=$TestResultsFile" `
            --collect:"XPlat Code Coverage" `
            --results-directory $OutputDir `
            --verbosity normal

        if ($LASTEXITCODE -ne 0) {
            Write-Host "测试执行失败!" -ForegroundColor Red
            exit $LASTEXITCODE
        }

        # 查找生成的覆盖率文件
        $CoverageFiles = Get-ChildItem -Path $OutputDir -Recurse -Filter "coverage.cobertura.xml"
        if ($CoverageFiles.Count -gt 0) {
            $LatestCoverageFile = $CoverageFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            Write-Host "找到覆盖率文件: $($LatestCoverageFile.FullName)" -ForegroundColor Green

            # 检查是否安装了 reportgenerator
            $ReportGeneratorPath = where.exe reportgenerator 2>$null
            if ($ReportGeneratorPath) {
                Write-Host "生成 HTML 覆盖率报告..." -ForegroundColor Cyan
                $ReportDir = Join-Path $OutputDir "CoverageReport"

                & reportgenerator `
                    "-reports:$($LatestCoverageFile.FullName)" `
                    "-targetdir:$ReportDir" `
                    "-reporttypes:Html;HtmlSummary;JsonSummary" `
                    "-title:LYBT WebAPI Controller Tests Coverage"

                if ($LASTEXITCODE -eq 0) {
                    Write-Host "覆盖率报告生成成功: $ReportDir" -ForegroundColor Green

                    if ($OpenReport) {
                        $IndexFile = Join-Path $ReportDir "index.html"
                        if (Test-Path $IndexFile) {
                            Write-Host "打开覆盖率报告..." -ForegroundColor Cyan
                            Start-Process $IndexFile
                        }
                    }
                } else {
                    Write-Host "覆盖率报告生成失败!" -ForegroundColor Red
                }
            } else {
                Write-Host "未安装 reportgenerator 工具。请运行: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
            }
        } else {
            Write-Host "未找到覆盖率文件!" -ForegroundColor Yellow
        }
    } else {
        Write-Host "运行测试..." -ForegroundColor Cyan
        & dotnet test "$TestProject" --configuration $Configuration --verbosity normal

        if ($LASTEXITCODE -ne 0) {
            Write-Host "测试执行失败!" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }

    Write-Host "测试执行完成!" -ForegroundColor Green

    # 显示测试结果摘要
    if (Test-Path (Join-Path $OutputDir "test-results.trx")) {
        Write-Host "`n测试结果摘要:" -ForegroundColor Cyan
        $TrxContent = Get-Content (Join-Path $OutputDir "test-results.trx") -Raw
        if ($TrxContent -match 'total="(\d+)".*executed="(\d+)".*passed="(\d+)".*failed="(\d+)"') {
            Write-Host "总计: $($Matches[1]) | 执行: $($Matches[2]) | 通过: $($Matches[3]) | 失败: $($Matches[4])" -ForegroundColor White
        }
    }

} catch {
    Write-Host "执行过程中发生错误: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n测试完成!" -ForegroundColor Green