@echo off
setlocal enabledelayedexpansion

title LYBT 数据库性能管理器
color 0A

:main_menu
cls
echo.
echo ========================================
echo       LYBT 数据库性能管理器
echo ========================================
echo.
echo 1. 添加性能优化索引
echo 2. 运行性能基准测试
echo 3. 查看索引使用情况  
echo 4. 生成性能优化报告
echo 5. 删除未使用的索引
echo 6. 数据库统计信息更新
echo 7. 查询执行计划分析
echo 8. 退出
echo.
set /p choice=请选择操作 (1-8): 

if "%choice%"=="1" goto add_indexes
if "%choice%"=="2" goto run_benchmark  
if "%choice%"=="3" goto show_index_usage
if "%choice%"=="4" goto generate_report
if "%choice%"=="5" goto remove_unused_indexes
if "%choice%"=="6" goto update_statistics
if "%choice%"=="7" goto analyze_execution_plans
if "%choice%"=="8" goto exit
goto main_menu

:add_indexes
cls
echo.
echo ========================================
echo         添加性能优化索引
echo ========================================
echo.
echo 正在添加Entity Framework迁移...
echo.

cd /d "%~dp0.."

REM 添加新的迁移
dotnet ef migrations add AddPerformanceIndexes_20250811 --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] 添加迁移失败
    pause
    goto main_menu
)

echo.
echo 正在更新数据库...
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] 数据库更新失败
    pause
    goto main_menu
)

echo.
echo [成功] 性能优化索引已添加
echo.
echo 已添加的索引包括:
echo - IX_Users_UserName_Unique (用户名唯一索引)
echo - IX_Users_Role_IsActive_CreatedAt (用户角色复合索引)
echo - IX_Patients_Name_PhoneNumber_CreatedAt (患者搜索索引)
echo - IX_Herbs_Category_IsEnabled_CreatedAt (中药材分类索引)
echo - IX_Prescriptions_PatientId_CreatedAt (患者处方索引)
echo.
pause
goto main_menu

:run_benchmark
cls
echo.
echo ========================================
echo         运行性能基准测试
echo ========================================
echo.
echo 正在启动Web API进行性能测试...

REM 启动API（后台运行）
start "LYBT WebAPI" /min dotnet run --project src/Backend/Services/LYBT.WebAPI --no-build --urls "https://localhost:7001"

REM 等待API启动
echo 等待API启动...
timeout /t 10 /nobreak > nul

REM 运行性能测试脚本
echo 正在执行性能基准测试...
python scripts/performance-benchmark.py

echo.
echo 性能测试完成，请查看生成的报告
pause

REM 关闭API
taskkill /f /im dotnet.exe > nul 2>&1

goto main_menu

:show_index_usage  
cls
echo.
echo ========================================
echo         查看索引使用情况
echo ========================================
echo.

REM 创建临时SQL文件
set temp_sql=%TEMP%\index_usage_check.sql

echo SELECT > "%temp_sql%"
echo     t.name AS TableName, >> "%temp_sql%"
echo     i.name AS IndexName, >> "%temp_sql%"
echo     i.type_desc AS IndexType, >> "%temp_sql%"
echo     ISNULL(ius.user_seeks, 0) AS UserSeeks, >> "%temp_sql%"
echo     ISNULL(ius.user_scans, 0) AS UserScans, >> "%temp_sql%"
echo     ISNULL(ius.user_lookups, 0) AS UserLookups, >> "%temp_sql%"
echo     ISNULL(ius.user_updates, 0) AS UserUpdates >> "%temp_sql%"
echo FROM sys.indexes i >> "%temp_sql%"
echo INNER JOIN sys.tables t ON i.object_id = t.object_id >> "%temp_sql%"
echo LEFT JOIN sys.dm_db_index_usage_stats ius ON i.object_id = ius.object_id AND i.index_id = ius.index_id >> "%temp_sql%"
echo WHERE t.name IN ('Users', 'Patients', 'Herbs', 'Prescriptions', 'FormulaTemplates', 'Consultations', 'MedicalCases') >> "%temp_sql%"
echo ORDER BY t.name, i.name; >> "%temp_sql%"

echo 连接数据库查询索引使用情况...
sqlcmd -S "localhost" -d "LYBTDB" -E -i "%temp_sql%"

REM 清理临时文件
del "%temp_sql%" > nul 2>&1

echo.
pause
goto main_menu

:generate_report
cls  
echo.
echo ========================================
echo         生成性能优化报告
echo ========================================
echo.

echo 正在生成性能报告...

REM 创建报告目录
if not exist "docs\reports\performance" mkdir "docs\reports\performance"

REM 生成报告文件名
for /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set mydate=%%c-%%a-%%b)
for /f "tokens=1-2 delims=: " %%a in ('time /t') do (set mytime=%%a%%b)
set report_file=docs\reports\performance\database-performance-report-%mydate%.md

echo 开始生成性能分析报告...

REM 启动API进行报告生成
start "LYBT WebAPI" /min dotnet run --project src/Backend/Services/LYBT.WebAPI --no-build --urls "https://localhost:7001"

timeout /t 10 /nobreak > nul

REM 调用性能报告API
curl -k -s "https://localhost:7001/api/v1/performance/report" -o temp_report.json

if exist temp_report.json (
    echo [成功] 性能报告已生成: %report_file%
    move temp_report.json "%report_file%.json" > nul
) else (
    echo [错误] 生成性能报告失败
)

REM 关闭API
taskkill /f /im dotnet.exe > nul 2>&1

echo.
pause
goto main_menu

:remove_unused_indexes
cls
echo.
echo ========================================
echo         删除未使用的索引
echo ========================================
echo.
echo [警告] 此操作将删除未使用的索引，请确保已备份数据库
echo.
set /p confirm=确认删除未使用的索引? (y/N): 

if /i not "%confirm%"=="y" goto main_menu

echo 正在分析未使用的索引...

REM 这里应该调用专门的索引清理脚本
echo 索引清理功能正在开发中...
echo 建议手动review索引使用情况后再删除

pause
goto main_menu

:update_statistics
cls
echo.
echo ========================================
echo         更新数据库统计信息
echo ========================================
echo.

echo 正在更新数据库统计信息...

REM 创建统计信息更新脚本
set stats_sql=%TEMP%\update_statistics.sql

echo -- 更新所有表的统计信息 > "%stats_sql%"
echo UPDATE STATISTICS Users WITH FULLSCAN; >> "%stats_sql%"
echo UPDATE STATISTICS Patients WITH FULLSCAN; >> "%stats_sql%"
echo UPDATE STATISTICS Herbs WITH FULLSCAN; >> "%stats_sql%"
echo UPDATE STATISTICS Prescriptions WITH FULLSCAN; >> "%stats_sql%"
echo UPDATE STATISTICS FormulaTemplates WITH FULLSCAN; >> "%stats_sql%"
echo UPDATE STATISTICS Consultations WITH FULLSCAN; >> "%stats_sql%"
echo UPDATE STATISTICS MedicalCases WITH FULLSCAN; >> "%stats_sql%"
echo PRINT 'Statistics updated successfully'; >> "%stats_sql%"

sqlcmd -S "localhost" -d "LYBTDB" -E -i "%stats_sql%"

del "%stats_sql%" > nul 2>&1

echo.
echo [完成] 数据库统计信息更新完成
pause
goto main_menu

:analyze_execution_plans
cls
echo.
echo ========================================
echo         查询执行计划分析
echo ========================================
echo.

echo 正在分析常用查询的执行计划...

REM 创建执行计划分析脚本
set plan_sql=%TEMP%\execution_plans.sql

echo SET STATISTICS IO ON; > "%plan_sql%"
echo SET STATISTICS TIME ON; >> "%plan_sql%"
echo -- 分析用户查询执行计划 >> "%plan_sql%"
echo SELECT TOP 10 * FROM Users WHERE UserName LIKE 'sys%%' ORDER BY CreatedAt DESC; >> "%plan_sql%"
echo -- 分析患者搜索执行计划 >> "%plan_sql%"
echo SELECT TOP 10 * FROM Patients WHERE Name LIKE '%%张%%' ORDER BY CreatedAt DESC; >> "%plan_sql%"
echo SET STATISTICS IO OFF; >> "%plan_sql%"
echo SET STATISTICS TIME OFF; >> "%plan_sql%"

sqlcmd -S "localhost" -d "LYBTDB" -E -i "%plan_sql%"

del "%plan_sql%" > nul 2>&1

echo.
pause
goto main_menu

:exit
echo.
echo 谢谢使用 LYBT 数据库性能管理器！
pause
exit /b 0