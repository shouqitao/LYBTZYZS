@echo off
:: 设置代码页为UTF-8以支持中文显示
chcp 65001 >nul 2>&1
:: 启用延迟变量扩展
setlocal enabledelayedexpansion

echo ====================================
echo     LYBT 一键部署测试脚本
echo ====================================
echo.

echo 🧪 开始完整部署流程测试...
echo.

:: 第一步：运行系统测试
echo [步骤 1] 运行部署系统测试...
call "%~dp0test-deploy-system.bat"
if !errorlevel! neq 0 (
    echo ❌ 系统测试失败，终止部署测试
    goto :end
)

echo.
echo [步骤 2] 询问是否继续实际部署测试...
set /p "continue=是否继续执行实际部署测试？(y/N): "
if /i not "!continue!"=="y" (
    echo 部署测试已取消
    goto :end
)

echo.
echo [步骤 3] 备份当前配置...
if not exist "%~dp0backup" mkdir "%~dp0backup"
if exist "D:\source\repos\LYBTZYZS\Release\WebAPI" (
    echo 备份现有发布文件...
    xcopy "D:\source\repos\LYBTZYZS\Release\WebAPI\*" "%~dp0backup\WebAPI_!date:~0,4!!date:~5,2!!date:~8,2!_!time:~0,2!!time:~3,2!\" /E /I /Q >nul 2>&1
)

echo.
echo [步骤 4] 执行自动部署...
call "%~dp0auto-deploy.bat"
set "deploy_result=!errorlevel!"

echo.
echo [步骤 5] 等待服务启动...
timeout /t 15 /nobreak >nul

echo.
echo [步骤 6] 运行健康检查...
call "%~dp0health-check.bat"

echo.
echo ====================================
if !deploy_result! equ 0 (
    echo ✅ 完整部署测试成功！
    echo.
    echo 📊 测试结果摘要：
    echo ✅ 系统环境检查通过
    echo ✅ 自动部署流程成功
    echo ✅ 服务健康检查完成
    echo.
    echo 🌐 可以访问: http://192.168.190.243:5297
) else (
    echo ❌ 部署测试失败！
    echo.
    echo 🔧 故障排除建议：
    echo 1. 检查网络连接
    echo 2. 验证服务器配置
    echo 3. 查看错误日志
    echo 4. 手动运行各个步骤
)
echo ====================================
echo.

:end
pause