@echo off
:: 设置代码页为UTF-8以支持中文显示
chcp 65001 >nul 2>&1
:: 启用延迟变量扩展
setlocal enabledelayedexpansion
echo ====================================
echo     LYBT 服务器环境初始化脚本
echo ====================================
echo.

:: 检查管理员权限
net session >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ 请以管理员身份运行此脚本！
    pause
    exit /b 1
)

echo [步骤 1] 创建目录结构...
if not exist "C:\LYBT" mkdir "C:\LYBT"
if not exist "C:\LYBT\WebAPI" mkdir "C:\LYBT\WebAPI"
if not exist "C:\LYBT\Backup" mkdir "C:\LYBT\Backup"
if not exist "C:\LYBT\Logs" mkdir "C:\LYBT\Logs"
if not exist "C:\LYBT\Scripts" mkdir "C:\LYBT\Scripts"
if not exist "C:\temp" mkdir "C:\temp"

echo [步骤 2] 复制脚本文件...
copy "!~dp0!server-deploy.bat" "C:\LYBT\Scripts\" >nul 2>&1
copy "!~dp0!file-monitor.bat" "C:\LYBT\Scripts\" >nul 2>&1

echo [步骤 3] 设置权限...
icacls "C:\LYBT" /grant "Everyone:(OI)(CI)F" /T >nul 2>&1
icacls "C:\temp" /grant "Everyone:(OI)(CI)F" /T >nul 2>&1

echo [步骤 4] 配置开机自启动监控...
set "STARTUP_PATH=!APPDATA!\Microsoft\Windows\Start Menu\Programs\Startup"
(
echo @echo off
echo cd /d "C:\LYBT\Scripts"
echo start "LYBT Deploy Monitor" /min file-monitor.bat
) > "!STARTUP_PATH!\LYBT-Monitor.bat"

echo [步骤 5] 创建健康检查端点...
(
echo using Microsoft.AspNetCore.Mvc;
echo.
echo namespace LYBT.WebAPI.Controllers
echo {
echo     [ApiController]
echo     [Route("[controller]")]
echo     public class HealthController : ControllerBase
echo     {
echo         [HttpGet]
echo         public IActionResult Get()
echo         {
echo             return Ok(new { status = "healthy", timestamp = DateTime.Now });
echo         }
echo     }
echo }
) > "C:\LYBT\HealthController.cs"

echo.
echo ✅ 服务器环境初始化完成！
echo 📂 主目录: C:\LYBT\
echo 📁 WebAPI: C:\LYBT\WebAPI\
echo 📁 备份: C:\LYBT\Backup\
echo 📁 日志: C:\LYBT\Logs\
echo 📁 脚本: C:\LYBT\Scripts\
echo.
echo 🔄 下次重启后将自动开始监控部署信号
echo.
pause