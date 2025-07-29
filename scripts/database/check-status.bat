@echo off
echo =========================================
echo        LYBT WebAPI 进程状态检查
echo =========================================

echo 1. 检查dotnet进程...
set "dotnet_count=0"
for /f "tokens=2,8" %%a in ('tasklist /fi "imagename eq dotnet.exe" /fo csv /nh 2^>nul ^| findstr /v "PID"') do (
    if not "%%a"=="" (
        set /a dotnet_count+=1
        echo    进程ID: %%a, 内存: %%b
    )
)

if "%dotnet_count%"=="0" (
    echo ✅ 未发现dotnet进程
) else (
    echo ⚠️  发现 %dotnet_count% 个dotnet进程
)

echo.
echo 2. 检查端口占用...
set "port_count=0"
for /f "tokens=2,5" %%a in ('netstat -aon ^| findstr :50 ^| findstr LISTENING') do (
    set /a port_count+=1
    echo    端口: %%a, 进程ID: %%b
)

if "%port_count%"=="0" (
    echo ✅ 未发现5xxx端口占用
) else (
    echo ⚠️  发现 %port_count% 个端口被占用
)

echo.
echo 3. 检查LYBT相关文件锁定...
if exist "BIN\Debug\net8.0\*.dll" (
    echo    检查DLL文件是否被锁定...
    handle.exe "BIN\Debug\net8.0" 2>nul | findstr /i "lybt\|dotnet" >nul
    if not errorlevel 1 (
        echo ⚠️  发现文件锁定
    ) else (
        echo ✅ 未发现文件锁定
    )
) else (
    echo ✅ 构建输出目录正常
)

echo.
echo 4. 系统建议...
if "%dotnet_count%" gtr "0" (
    echo 💡 建议执行: stop-webapi.bat 清理进程
)
if "%port_count%" gtr "0" (
    echo 💡 建议检查哪些程序占用了端口
)
if "%dotnet_count%"=="0" if "%port_count%"=="0" (
    echo ✅ 系统状态良好，可以启动WebAPI
)

echo.
echo =========================================
echo           检查完成
echo =========================================
pause