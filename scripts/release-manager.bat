@echo off
REM ================================================
REM 凌隐宝堂中医诊所系统 - 发布管理器
REM UltraThink Stage 5.3.3 - 版本发布自动化
REM ================================================

setlocal enabledelayedexpansion

:menu
cls
echo ================================================
echo      凌隐宝堂中医诊所系统 - 发布管理器
echo ================================================
echo.
echo 1. 创建新版本发布
echo 2. 生成增量更新包
echo 3. 发布到测试环境
echo 4. 发布到生产环境
echo 5. 回滚到上一版本
echo 6. 查看发布历史
echo 7. 生成发布说明
echo 8. 验证发布包
echo 9. 清理旧版本
echo 0. 退出
echo.
set /p choice=请选择操作 [0-9]: 

if "%choice%"=="1" goto :create_release
if "%choice%"=="2" goto :create_delta
if "%choice%"=="3" goto :deploy_test
if "%choice%"=="4" goto :deploy_production
if "%choice%"=="5" goto :rollback
if "%choice%"=="6" goto :view_history
if "%choice%"=="7" goto :generate_notes
if "%choice%"=="8" goto :validate_package
if "%choice%"=="9" goto :cleanup
if "%choice%"=="0" goto :exit

echo 无效的选择，请重试
pause
goto :menu

REM ================================================
REM 创建新版本发布
REM ================================================
:create_release
cls
echo ================================================
echo            创建新版本发布
echo ================================================
echo.

REM 获取当前版本
set /p CURRENT_VERSION=<version.txt 2>nul
if "%CURRENT_VERSION%"=="" set CURRENT_VERSION=1.0.0
echo 当前版本: %CURRENT_VERSION%
echo.

REM 输入新版本号
set /p NEW_VERSION=请输入新版本号 (例如: 1.0.1): 
if "%NEW_VERSION%"=="" (
    echo 版本号不能为空
    pause
    goto :menu
)

REM 选择发布类型
echo.
echo 发布类型:
echo 1. 主要版本 (Major)
echo 2. 次要版本 (Minor)
echo 3. 补丁版本 (Patch)
echo 4. 预发布版本 (Pre-release)
set /p RELEASE_TYPE=选择发布类型 [1-4]: 

REM 设置构建配置
if "%RELEASE_TYPE%"=="4" (
    set BUILD_CONFIG=Debug
    set RELEASE_SUFFIX=-beta
) else (
    set BUILD_CONFIG=Release
    set RELEASE_SUFFIX=
)

echo.
echo 正在创建版本 %NEW_VERSION%%RELEASE_SUFFIX%...
echo.

REM 更新版本文件
echo %NEW_VERSION%> version.txt

REM 执行构建
call "%~dp0build-all.bat" %BUILD_CONFIG%
if %errorlevel% neq 0 (
    echo 构建失败
    pause
    goto :menu
)

REM 创建发布包
set TIMESTAMP=%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set TIMESTAMP=%TIMESTAMP: =0%
set RELEASE_DIR=%~dp0..\Releases\%NEW_VERSION%%RELEASE_SUFFIX%_%TIMESTAMP%

mkdir "%RELEASE_DIR%" 2>nul
xcopy /E /I /Y "%~dp0..\BIN\%BUILD_CONFIG%" "%RELEASE_DIR%\BIN"
xcopy /E /I /Y "%~dp0..\Config\Production" "%RELEASE_DIR%\Config"
copy "%~dp0..\README.md" "%RELEASE_DIR%\"
copy "%~dp0..\LICENSE" "%RELEASE_DIR%\"

REM 生成发布信息
(
    echo Version: %NEW_VERSION%%RELEASE_SUFFIX%
    echo Build: %TIMESTAMP%
    echo Type: %RELEASE_TYPE%
    echo Config: %BUILD_CONFIG%
    echo Date: %date% %time%
) > "%RELEASE_DIR%\RELEASE_INFO.txt"

REM 创建ZIP包
set PACKAGE_NAME=LYBT_%NEW_VERSION%%RELEASE_SUFFIX%_%TIMESTAMP%.zip
powershell -Command "Compress-Archive -Path '%RELEASE_DIR%\*' -DestinationPath '%~dp0..\Packages\%PACKAGE_NAME%'"

echo.
echo ================================================
echo 版本 %NEW_VERSION%%RELEASE_SUFFIX% 创建成功！
echo ================================================
echo 发布目录: %RELEASE_DIR%
echo 安装包: %PACKAGE_NAME%
echo ================================================
pause
goto :menu

REM ================================================
REM 生成增量更新包
REM ================================================
:create_delta
cls
echo ================================================
echo            生成增量更新包
echo ================================================
echo.

set /p FROM_VERSION=请输入起始版本号: 
set /p TO_VERSION=请输入目标版本号: 

if "%FROM_VERSION%"=="" goto :menu
if "%TO_VERSION%"=="" goto :menu

echo.
echo 正在生成从 %FROM_VERSION% 到 %TO_VERSION% 的增量包...
echo.

REM 查找版本目录
set FROM_DIR=
set TO_DIR=
for /d %%D in ("%~dp0..\Releases\%FROM_VERSION%*") do set FROM_DIR=%%D
for /d %%D in ("%~dp0..\Releases\%TO_VERSION%*") do set TO_DIR=%%D

if "%FROM_DIR%"=="" (
    echo 找不到版本 %FROM_VERSION%
    pause
    goto :menu
)
if "%TO_DIR%"=="" (
    echo 找不到版本 %TO_VERSION%
    pause
    goto :menu
)

REM 创建增量包目录
set DELTA_DIR=%~dp0..\Releases\Delta_%FROM_VERSION%_to_%TO_VERSION%
mkdir "%DELTA_DIR%" 2>nul

REM 比较并复制变更文件
powershell -Command "& {
    $from = '%FROM_DIR%\BIN'
    $to = '%TO_DIR%\BIN'
    $delta = '%DELTA_DIR%'
    
    Get-ChildItem $to -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($to.Length + 1)
        $fromFile = Join-Path $from $relativePath
        
        if (-not (Test-Path $fromFile) -or 
            (Get-FileHash $_.FullName).Hash -ne (Get-FileHash $fromFile).Hash) {
            $targetPath = Join-Path $delta $relativePath
            $targetDir = Split-Path $targetPath -Parent
            if (-not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }
            Copy-Item $_.FullName -Destination $targetPath -Force
            Write-Host "添加: $relativePath"
        }
    }
}"

REM 生成增量清单
(
    echo Delta Update Package
    echo From: %FROM_VERSION%
    echo To: %TO_VERSION%
    echo Created: %date% %time%
) > "%DELTA_DIR%\DELTA_MANIFEST.txt"

REM 创建增量包
set DELTA_PACKAGE=LYBT_Delta_%FROM_VERSION%_to_%TO_VERSION%.zip
powershell -Command "Compress-Archive -Path '%DELTA_DIR%\*' -DestinationPath '%~dp0..\Packages\%DELTA_PACKAGE%'"

echo.
echo ================================================
echo 增量包生成成功！
echo ================================================
echo 增量包: %DELTA_PACKAGE%
echo ================================================
pause
goto :menu

REM ================================================
REM 发布到测试环境
REM ================================================
:deploy_test
cls
echo ================================================
echo            发布到测试环境
echo ================================================
echo.

set /p VERSION=请输入要发布的版本号: 
if "%VERSION%"=="" goto :menu

echo.
echo 正在发布版本 %VERSION% 到测试环境...
echo.

REM 停止测试服务
echo 停止现有服务...
sc \\TEST-SERVER stop LYBTService 2>nul
timeout /t 5 /nobreak >nul

REM 备份当前版本
echo 备份当前版本...
set BACKUP_DIR=\\TEST-SERVER\Backups\%date:~0,4%%date:~5,2%%date:~8,2%
mkdir "%BACKUP_DIR%" 2>nul
xcopy /E /I /Y "\\TEST-SERVER\LYBT" "%BACKUP_DIR%"

REM 部署新版本
echo 部署新版本...
set RELEASE_DIR=
for /d %%D in ("%~dp0..\Releases\%VERSION%*") do set RELEASE_DIR=%%D

if "%RELEASE_DIR%"=="" (
    echo 找不到版本 %VERSION%
    pause
    goto :menu
)

xcopy /E /I /Y "%RELEASE_DIR%\BIN" "\\TEST-SERVER\LYBT"

REM 更新配置
echo 更新配置文件...
copy /Y "%~dp0..\Config\Test\appsettings.json" "\\TEST-SERVER\LYBT\Backend\"

REM 启动服务
echo 启动服务...
sc \\TEST-SERVER start LYBTService

echo.
echo ================================================
echo 版本 %VERSION% 已发布到测试环境
echo ================================================
pause
goto :menu

REM ================================================
REM 发布到生产环境
REM ================================================
:deploy_production
cls
echo ================================================
echo            发布到生产环境
echo ================================================
echo.
echo 警告: 这将更新生产环境！
echo.
set /p CONFIRM=确认要继续吗？(yes/no): 
if not "%CONFIRM%"=="yes" goto :menu

set /p VERSION=请输入要发布的版本号: 
if "%VERSION%"=="" goto :menu

echo.
echo 正在发布版本 %VERSION% 到生产环境...
echo.

REM 生产环境部署逻辑
call "%~dp0deploy-production.bat"

echo.
echo ================================================
echo 版本 %VERSION% 已发布到生产环境
echo ================================================
pause
goto :menu

REM ================================================
REM 回滚到上一版本
REM ================================================
:rollback
cls
echo ================================================
echo            回滚到上一版本
echo ================================================
echo.

echo 可用的备份:
dir /B /AD "%~dp0..\Backups"
echo.

set /p BACKUP_NAME=请输入要回滚的备份名称: 
if "%BACKUP_NAME%"=="" goto :menu

set BACKUP_DIR=%~dp0..\Backups\%BACKUP_NAME%
if not exist "%BACKUP_DIR%" (
    echo 备份不存在
    pause
    goto :menu
)

echo.
echo 正在回滚到备份 %BACKUP_NAME%...
echo.

REM 停止服务
sc stop LYBTService 2>nul
timeout /t 5 /nobreak >nul

REM 备份当前版本（以防回滚失败）
set TEMP_BACKUP=%~dp0..\Backups\Rollback_%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set TEMP_BACKUP=%TEMP_BACKUP: =0%
mkdir "%TEMP_BACKUP%" 2>nul
xcopy /E /I /Y "%ProgramFiles%\LYBT" "%TEMP_BACKUP%"

REM 执行回滚
xcopy /E /I /Y "%BACKUP_DIR%" "%ProgramFiles%\LYBT"

REM 重启服务
sc start LYBTService

echo.
echo ================================================
echo 已回滚到备份 %BACKUP_NAME%
echo ================================================
pause
goto :menu

REM ================================================
REM 查看发布历史
REM ================================================
:view_history
cls
echo ================================================
echo            发布历史
echo ================================================
echo.

echo 已发布版本:
echo ------------
for /d %%D in ("%~dp0..\Releases\*") do (
    if exist "%%D\RELEASE_INFO.txt" (
        echo.
        type "%%D\RELEASE_INFO.txt"
        echo ------------
    )
)

echo.
pause
goto :menu

REM ================================================
REM 生成发布说明
REM ================================================
:generate_notes
cls
echo ================================================
echo            生成发布说明
echo ================================================
echo.

set /p VERSION=请输入版本号: 
if "%VERSION%"=="" goto :menu

set OUTPUT_FILE=%~dp0..\Releases\RELEASE_NOTES_%VERSION%.md

(
    echo # 凌隐宝堂中医诊所系统 v%VERSION% 发布说明
    echo.
    echo 发布日期: %date%
    echo.
    echo ## 新功能
    echo.
    echo - 功能1
    echo - 功能2
    echo.
    echo ## 改进
    echo.
    echo - 改进1
    echo - 改进2
    echo.
    echo ## 修复
    echo.
    echo - 修复1
    echo - 修复2
    echo.
    echo ## 已知问题
    echo.
    echo - 问题1
    echo.
    echo ## 升级说明
    echo.
    echo 1. 备份当前数据库
    echo 2. 停止所有服务
    echo 3. 运行安装程序
    echo 4. 更新配置文件
    echo 5. 重启服务
    echo.
    echo ## 系统要求
    echo.
    echo - Windows 10/11 或 Windows Server 2019+
    echo - .NET 8.0 Runtime
    echo - SQL Server 2019+
    echo - 最低 4GB RAM
    echo - 10GB 可用磁盘空间
) > "%OUTPUT_FILE%"

echo.
echo 发布说明已生成: %OUTPUT_FILE%
echo.
notepad "%OUTPUT_FILE%"
pause
goto :menu

REM ================================================
REM 验证发布包
REM ================================================
:validate_package
cls
echo ================================================
echo            验证发布包
echo ================================================
echo.

set /p PACKAGE_FILE=请输入包文件名（不含路径）: 
if "%PACKAGE_FILE%"=="" goto :menu

set PACKAGE_PATH=%~dp0..\Packages\%PACKAGE_FILE%
if not exist "%PACKAGE_PATH%" (
    echo 包文件不存在
    pause
    goto :menu
)

echo.
echo 正在验证 %PACKAGE_FILE%...
echo.

REM 检查文件大小
for %%F in ("%PACKAGE_PATH%") do set SIZE=%%~zF
set /a SIZE_MB=%SIZE%/1048576
echo 文件大小: %SIZE_MB% MB

REM 验证ZIP完整性
powershell -Command "try { Add-Type -Assembly 'System.IO.Compression.FileSystem'; [IO.Compression.ZipFile]::OpenRead('%PACKAGE_PATH%').Dispose(); Write-Host 'ZIP文件完整性: 通过' -ForegroundColor Green } catch { Write-Host 'ZIP文件完整性: 失败' -ForegroundColor Red }"

REM 计算校验和
echo.
echo 计算SHA256校验和...
certutil -hashfile "%PACKAGE_PATH%" SHA256 | findstr /v ":" | findstr /v "CertUtil"

echo.
echo ================================================
echo 验证完成
echo ================================================
pause
goto :menu

REM ================================================
REM 清理旧版本
REM ================================================
:cleanup
cls
echo ================================================
echo            清理旧版本
echo ================================================
echo.

set /p KEEP_DAYS=保留最近几天的版本？(默认30): 
if "%KEEP_DAYS%"=="" set KEEP_DAYS=30

echo.
echo 将删除 %KEEP_DAYS% 天前的版本...
echo.

REM 清理发布目录
forfiles /P "%~dp0..\Releases" /D -%KEEP_DAYS% /C "cmd /c if @isdir==TRUE rd /s /q @path" 2>nul

REM 清理包文件
forfiles /P "%~dp0..\Packages" /M *.zip /D -%KEEP_DAYS% /C "cmd /c del @path" 2>nul

REM 清理备份
forfiles /P "%~dp0..\Backups" /D -%KEEP_DAYS% /C "cmd /c if @isdir==TRUE rd /s /q @path" 2>nul

REM 清理日志
forfiles /P "%~dp0..\DeploymentLogs" /M *.log /D -%KEEP_DAYS% /C "cmd /c del @path" 2>nul

echo.
echo 清理完成
echo.
pause
goto :menu

REM ================================================
REM 退出
REM ================================================
:exit
echo.
echo 感谢使用发布管理器！
echo.
endlocal
exit /b 0