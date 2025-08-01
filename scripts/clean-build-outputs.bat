@echo off
REM =====================================================================
REM LYBTZYZS 项目构建输出清理脚本
REM 用途：清理所有项目的bin/obj等构建输出文件，保持项目目录干净整洁
REM 版本：v1.0
REM 创建日期：2025-08-01
REM =====================================================================

echo.
echo =====================================
echo  LYBTZYZS 构建输出清理工具
echo =====================================
echo.

cd /d "%~dp0\.."

echo [1/4] 正在清理Backend解决方案输出...
cd "src\Backend"
dotnet clean --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo     错误：Backend清理失败
    goto :error
)
echo     ✓ Backend清理完成

cd "..\Frontend"
echo [2/4] 正在清理Frontend解决方案输出...
dotnet clean --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo     错误：Frontend清理失败
    goto :error
)
echo     ✓ Frontend清理完成

cd "..\..\"

echo [3/4] 正在清理临时构建目录...
if exist "BIN\temp" (
    rmdir /s /q "BIN\temp" 2>nul
    echo     ✓ 临时目录已清理
) else (
    echo     ✓ 无需清理临时目录
)

echo [4/4] 正在清理历史构建输出...
REM 清理可能存在的旧bin/obj目录
for /d /r . %%d in (bin obj) do (
    if exist "%%d" (
        rmdir /s /q "%%d" 2>nul
        echo     已删除: %%d
    )
)

echo.
echo =====================================
echo  清理完成！项目目录已经干净整洁
echo =====================================
echo.
echo 提示：下次构建时，所有输出将按新的目录结构生成：
echo   - WebAPI 输出到: BIN\LybtWebApi
echo   - WPF桌面端输出到: BIN\LybtDesktop
echo   - 其他项目输出到: BIN\temp
echo.
pause
goto :eof

:error
echo.
echo =====================================
echo  清理过程中发生错误！
echo =====================================
echo.
pause
exit /b 1