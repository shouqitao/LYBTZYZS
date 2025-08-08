@echo off
echo ===================================================
echo LYBTZYZS 测试覆盖率报告生成器
echo ===================================================

REM 设置变量
set ROOT_DIR=%~dp0..
set TEST_RESULTS=%ROOT_DIR%\TestResults
set COVERAGE_DIR=%TEST_RESULTS%\Coverage
set REPORT_DIR=%TEST_RESULTS%\Reports

REM 创建必要的目录
echo 创建目录结构...
if not exist "%TEST_RESULTS%" mkdir "%TEST_RESULTS%"
if not exist "%COVERAGE_DIR%" mkdir "%COVERAGE_DIR%"
if not exist "%REPORT_DIR%" mkdir "%REPORT_DIR%"

REM 清理旧的测试结果
echo 清理旧的测试结果...
if exist "%TEST_RESULTS%\*.trx" del /q "%TEST_RESULTS%\*.trx"
if exist "%COVERAGE_DIR%\*.xml" del /q "%COVERAGE_DIR%\*.xml"
if exist "%COVERAGE_DIR%\*.json" del /q "%COVERAGE_DIR%\*.json"

REM 安装必要的工具
echo.
echo 检查并安装必要的工具...
dotnet tool list -g | findstr "reportgenerator" > nul
if errorlevel 1 (
    echo 安装 ReportGenerator...
    dotnet tool install -g dotnet-reportgenerator-globaltool
) else (
    echo ReportGenerator 已安装
)

REM 运行测试
echo.
echo 运行测试并收集覆盖率数据...
echo ===================================================

REM 用户模块测试
echo.
echo [1/3] 测试 Users 模块...
dotnet test "%ROOT_DIR%\tests\Backend\LYBT.Module.Users.Tests\LYBT.Module.Users.Tests.csproj" ^
    --configuration Release ^
    --logger "trx;LogFileName=Users.trx" ^
    --logger "console;verbosity=minimal" ^
    --results-directory "%TEST_RESULTS%" ^
    --settings "%ROOT_DIR%\coverlet.runsettings" ^
    --collect:"XPlat Code Coverage" ^
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

REM 患者模块测试
echo.
echo [2/3] 测试 Patients 模块...
dotnet test "%ROOT_DIR%\tests\Backend\LYBT.Module.Patients.Tests\LYBT.Module.Patients.Tests.csproj" ^
    --configuration Release ^
    --logger "trx;LogFileName=Patients.trx" ^
    --logger "console;verbosity=minimal" ^
    --results-directory "%TEST_RESULTS%" ^
    --settings "%ROOT_DIR%\coverlet.runsettings" ^
    --collect:"XPlat Code Coverage" ^
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

REM 药材模块测试
echo.
echo [3/3] 测试 Herbs 模块...
dotnet test "%ROOT_DIR%\tests\Backend\LYBT.Module.Herbs.Tests\LYBT.Module.Herbs.Tests.csproj" ^
    --configuration Release ^
    --logger "trx;LogFileName=Herbs.trx" ^
    --logger "console;verbosity=minimal" ^
    --results-directory "%TEST_RESULTS%" ^
    --settings "%ROOT_DIR%\coverlet.runsettings" ^
    --collect:"XPlat Code Coverage" ^
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

REM 查找生成的覆盖率文件
echo.
echo 查找覆盖率文件...
set COVERAGE_FILES=
for /r "%TEST_RESULTS%" %%f in (coverage.cobertura.xml) do (
    echo 找到: %%f
    if defined COVERAGE_FILES (
        set COVERAGE_FILES=!COVERAGE_FILES!;%%f
    ) else (
        set COVERAGE_FILES=%%f
    )
    REM 复制到统一目录
    copy "%%f" "%COVERAGE_DIR%\" > nul
)

REM 生成HTML报告
echo.
echo 生成覆盖率报告...
echo ===================================================

cd /d "%ROOT_DIR%"
reportgenerator ^
    "-reports:%COVERAGE_DIR%\*.xml" ^
    "-targetdir:%REPORT_DIR%" ^
    "-reporttypes:Html;Badges;JsonSummary;MarkdownSummary" ^
    "-title:LYBTZYZS 测试覆盖率报告" ^
    "-verbosity:Info"

REM 显示摘要
echo.
echo ===================================================
echo 测试完成！
echo.
echo 测试结果位置: %TEST_RESULTS%
echo 覆盖率报告位置: %REPORT_DIR%\index.html
echo ===================================================

REM 询问是否打开报告
echo.
set /p OPEN_REPORT=是否在浏览器中打开报告? (Y/N): 
if /i "%OPEN_REPORT%"=="Y" (
    start "" "%REPORT_DIR%\index.html"
)

pause