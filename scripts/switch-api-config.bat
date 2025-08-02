@echo off
chcp 65001 >nul
title LYBT 前端API配置切换工具

:MAIN_MENU
cls
echo.
echo =====================================
echo   LYBT 前端API配置切换工具
echo =====================================
echo.
echo 当前配置目录:
if exist "src\Frontend\BIN\LYBT.Desktop\appsettings.json" (
    echo - 开发版本: src\Frontend\BIN\LYBT.Desktop\
    for /f "tokens=2 delims=:" %%a in ('findstr "BaseUrl" "src\Frontend\BIN\LYBT.Desktop\appsettings.json"') do (
        set "current_dev=%%a"
        setlocal EnableDelayedExpansion
    )
    echo   当前API地址: %current_dev:"=%
)
echo.
if exist "BIN\LYBT.Desktop.Configurable\appsettings.json" (
    echo - 生产版本: BIN\LYBT.Desktop.Configurable\
    for /f "tokens=2 delims=:" %%a in ('findstr "BaseUrl" "BIN\LYBT.Desktop.Configurable\appsettings.json"') do (
        set "current_prod=%%a"
    )
    echo   当前API地址: %current_prod:"=%
)
echo.
echo 请选择操作:
echo [1] 切换开发版本为本地服务器 (localhost:5927)
echo [2] 切换开发版本为生产服务器 (192.168.190.243:5000)
echo [3] 切换生产版本为本地服务器 (localhost:5927)
echo [4] 切换生产版本为生产服务器 (192.168.190.243:5000)
echo [5] 自定义API地址
echo [6] 查看当前配置
echo [7] 测试API连接
echo [0] 退出
echo.
set /p choice="请输入选择 (0-7): "

if "%choice%"=="1" goto DEV_LOCAL
if "%choice%"=="2" goto DEV_PROD
if "%choice%"=="3" goto PROD_LOCAL
if "%choice%"=="4" goto PROD_PROD
if "%choice%"=="5" goto CUSTOM
if "%choice%"=="6" goto VIEW_CONFIG
if "%choice%"=="7" goto TEST_API
if "%choice%"=="0" goto EXIT
goto MAIN_MENU

:DEV_LOCAL
echo.
echo 正在将开发版本切换为本地服务器...
call :UPDATE_CONFIG "src\Frontend\BIN\LYBT.Desktop\appsettings.json" "http://localhost:5927/"
echo 开发版本已切换为本地服务器 (http://localhost:5927/)
pause
goto MAIN_MENU

:DEV_PROD
echo.
echo 正在将开发版本切换为生产服务器...
call :UPDATE_CONFIG "src\Frontend\BIN\LYBT.Desktop\appsettings.json" "http://192.168.190.243:5000/"
echo 开发版本已切换为生产服务器 (http://192.168.190.243:5000/)
pause
goto MAIN_MENU

:PROD_LOCAL
echo.
echo 正在将生产版本切换为本地服务器...
call :UPDATE_CONFIG "BIN\LYBT.Desktop.Configurable\appsettings.json" "http://localhost:5927/"
echo 生产版本已切换为本地服务器 (http://localhost:5927/)
pause
goto MAIN_MENU

:PROD_PROD
echo.
echo 正在将生产版本切换为生产服务器...
call :UPDATE_CONFIG "BIN\LYBT.Desktop.Configurable\appsettings.json" "http://192.168.190.243:5000/"
echo 生产版本已切换为生产服务器 (http://192.168.190.243:5000/)
pause
goto MAIN_MENU

:CUSTOM
echo.
echo 自定义API地址配置
echo.
set /p custom_url="请输入完整的API地址 (例: http://127.0.0.1:8080/): "
if "%custom_url%"=="" goto MAIN_MENU

echo.
echo 请选择要更新的版本:
echo [1] 开发版本
echo [2] 生产版本
echo [3] 两个版本都更新
set /p version_choice="请选择 (1-3): "

if "%version_choice%"=="1" (
    call :UPDATE_CONFIG "src\Frontend\BIN\LYBT.Desktop\appsettings.json" "%custom_url%"
    echo 开发版本已更新为: %custom_url%
)
if "%version_choice%"=="2" (
    call :UPDATE_CONFIG "BIN\LYBT.Desktop.Configurable\appsettings.json" "%custom_url%"
    echo 生产版本已更新为: %custom_url%
)
if "%version_choice%"=="3" (
    call :UPDATE_CONFIG "src\Frontend\BIN\LYBT.Desktop\appsettings.json" "%custom_url%"
    call :UPDATE_CONFIG "BIN\LYBT.Desktop.Configurable\appsettings.json" "%custom_url%"
    echo 两个版本都已更新为: %custom_url%
)
pause
goto MAIN_MENU

:VIEW_CONFIG
cls
echo.
echo =====================================
echo   当前配置详情
echo =====================================
echo.
if exist "src\Frontend\BIN\LYBT.Desktop\appsettings.json" (
    echo 开发版本配置:
    echo 文件路径: src\Frontend\BIN\LYBT.Desktop\appsettings.json
    type "src\Frontend\BIN\LYBT.Desktop\appsettings.json"
    echo.
) else (
    echo 开发版本配置文件不存在
    echo.
)

if exist "BIN\LYBT.Desktop.Configurable\appsettings.json" (
    echo 生产版本配置:
    echo 文件路径: BIN\LYBT.Desktop.Configurable\appsettings.json
    type "BIN\LYBT.Desktop.Configurable\appsettings.json"
    echo.
) else (
    echo 生产版本配置文件不存在
    echo.
)
pause
goto MAIN_MENU

:TEST_API
echo.
echo 测试API连接...
echo.
if exist "src\Frontend\BIN\LYBT.Desktop\appsettings.json" (
    for /f "tokens=2 delims=:" %%a in ('findstr "BaseUrl" "src\Frontend\BIN\LYBT.Desktop\appsettings.json"') do (
        set "test_url=%%a"
    )
    setlocal EnableDelayedExpansion
    set "test_url=!test_url:"=!"
    set "test_url=!test_url: =!"
    echo 正在测试开发版本API: !test_url!
    curl -s -o nul -w "HTTP状态码: %%{http_code} - 响应时间: %%{time_total}秒\n" "!test_url!swagger/v1/swagger.json" 2>nul
    if errorlevel 1 (
        echo 连接失败 - 请检查API服务器是否运行
    )
    echo.
)

if exist "BIN\LYBT.Desktop.Configurable\appsettings.json" (
    for /f "tokens=2 delims=:" %%a in ('findstr "BaseUrl" "BIN\LYBT.Desktop.Configurable\appsettings.json"') do (
        set "test_url2=%%a"
    )
    setlocal EnableDelayedExpansion
    set "test_url2=!test_url2:"=!"
    set "test_url2=!test_url2: =!"
    echo 正在测试生产版本API: !test_url2!
    curl -s -o nul -w "HTTP状态码: %%{http_code} - 响应时间: %%{time_total}秒\n" "!test_url2!swagger/v1/swagger.json" 2>nul
    if errorlevel 1 (
        echo 连接失败 - 请检查API服务器是否运行
    )
)
echo.
pause
goto MAIN_MENU

:UPDATE_CONFIG
setlocal EnableDelayedExpansion
set "config_file=%~1"
set "new_url=%~2"

if not exist "%config_file%" (
    echo 错误: 配置文件 %config_file% 不存在
    endlocal
    exit /b 1
)

rem 创建临时文件
set "temp_file=%config_file%.tmp"

rem 读取并更新配置文件
for /f "usebackq delims=" %%i in ("%config_file%") do (
    set "line=%%i"
    if "!line:BaseUrl=!" neq "!line!" (
        echo     "BaseUrl": "%new_url%",>>"%temp_file%"
    ) else (
        echo !line!>>"%temp_file%"
    )
)

rem 替换原文件
move "%temp_file%" "%config_file%" >nul
endlocal
exit /b 0

:EXIT
echo.
echo 感谢使用 LYBT 前端API配置切换工具！
pause
exit /b 0