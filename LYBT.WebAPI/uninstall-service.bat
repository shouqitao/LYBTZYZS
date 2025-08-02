@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所诊疗系统 - 服务卸载器

echo.
echo ====================================================
echo    凌隐宝堂中医诊所诊疗系统 - Windows服务卸载
echo ====================================================
echo.

:: 检查管理员权限
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ❌ 错误: 请以管理员身份运行此脚本
    echo 💡 右键点击脚本 → 以管理员身份运行
    pause
    exit /b 1
)

:: 设置变量
set "SERVICE_NAME=LYBT.WebAPI"
set "NSSM_PATH=%~dp0nssm.exe"

:: 检查NSSM是否存在
if not exist "%NSSM_PATH%" (
    echo ❌ 错误: 找不到 nssm.exe
    echo 💡 请将 nssm.exe 复制到当前目录
    pause
    exit /b 1
)

:: 检查服务是否存在
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% neq 0 (
    echo ❌ 错误: 服务 '%SERVICE_NAME%' 不存在
    echo 💡 可能已经被卸载或从未安装
    pause
    exit /b 1
)

echo 📋 即将卸载服务: %SERVICE_NAME%
echo.
set /p "CONFIRM=确认卸载服务? (Y/N): "
if /i "%CONFIRM%" neq "Y" (
    echo 🚫 卸载已取消
    pause
    exit /b 0
)

echo.
echo 🛑 正在停止服务...
"%NSSM_PATH%" stop "%SERVICE_NAME%"
if %errorLevel% equ 0 (
    echo ✅ 服务已停止
) else (
    echo ⚠️  服务可能已经停止或停止失败
)

echo.
echo 🗑️  正在卸载服务...
"%NSSM_PATH%" remove "%SERVICE_NAME%" confirm
if %errorLevel% equ 0 (
    echo ✅ 服务卸载成功!
) else (
    echo ❌ 服务卸载失败
    pause
    exit /b 1
)

echo.
echo 🎯 卸载完成!
echo.
echo 📁 注意事项:
echo    - 应用程序文件仍保留在当前目录
echo    - 日志文件保留在 logs 目录中
echo    - 如需完全清理，请手动删除相关文件
echo.

set /p "DELETE_LOGS=是否删除日志文件? (Y/N): "
if /i "%DELETE_LOGS%"=="Y" (
    if exist "%~dp0logs" (
        rmdir /s /q "%~dp0logs" 2>nul
        if %errorLevel% equ 0 (
            echo ✅ 日志文件已删除
        ) else (
            echo ⚠️  部分日志文件可能仍在使用中，请手动删除
        )
    )
)

echo.
pause