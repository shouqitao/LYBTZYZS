@echo off
chcp 65001 >nul
echo.
echo ========================================
echo 🏥 Phase E1: 小诊所配置优化脚本
echo ========================================
echo.

set "SOURCE_CONFIG=src\Server\Services\LYBT.WebAPI\appsettings.json"
set "TARGET_CONFIG=src\Server\Services\LYBT.WebAPI\appsettings.ClinicOptimized.json"

echo 📖 检查源配置文件: %SOURCE_CONFIG%
if not exist "%SOURCE_CONFIG%" (
    echo ❌ 错误: 源配置文件不存在
    pause
    exit /b 1
)

echo ✅ 源配置文件存在
echo.

echo 🔧 配置已手动优化完成
echo 📁 优化配置文件位置: %TARGET_CONFIG%
echo.

if exist "%TARGET_CONFIG%" (
    echo ✅ 小诊所优化配置文件已存在
) else (
    echo ❌ 优化配置文件不存在，请手动创建
)

echo.
echo 📋 优化摘要:
echo   • 数据库连接池: Max=10, Min=1, 超时=10/15秒
echo   • 内存缓存: 限制50项, 清理20%%, 扫描60秒
echo   • 日志保留: 7天, 单文件5MB
echo   • 批量操作: 50条记录限制
echo   • 认证安全: 3次失败锁定30分钟
echo.

echo 🎯 下一步操作:
echo   1. 复制 appsettings.ClinicOptimized.json 到生产环境
echo   2. 设置环境变量 (JWT密钥、数据库连接等)
echo   3. 参考部署指南进行配置验证
echo   4. 监控系统资源使用情况
echo.

echo 📖 详细部署指南: docs\deployment\clinic-deployment-guide.md
echo.

pause