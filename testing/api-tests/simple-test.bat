@echo off
chcp 65001 >nul
title LYBT测试数据生成器

echo.
echo 🚀 LYBT医疗系统测试数据生成器
echo ====================================

echo.
echo 🔍 测试API连接...
curl -s -o nul -w "Status: %%{http_code}" http://localhost:5297/api/health
echo.

if %errorlevel% neq 0 (
    echo ❌ API连接失败！请确保WebAPI服务正在运行
    pause
    exit /b 1
)

echo ✅ API连接正常

echo.
echo 🔐 正在登录系统...
curl -X POST "http://localhost:5297/api/v1/Auth/login" ^
     -H "Content-Type: application/json" ^
     -d "{\"username\":\"sysadmin\",\"password\":\"Admin@123456\",\"rememberMe\":true,\"loginType\":\"Password\"}" ^
     -o login_response.json -s

if %errorlevel% neq 0 (
    echo ❌ 登录失败！
    pause
    exit /b 1
)

echo ✅ 登录请求发送成功

echo.
echo 📋 登录响应:
type login_response.json
echo.

echo.
echo 👥 创建测试用户（需要手动提取token）...
echo.
echo 💡 接下来的步骤:
echo   1. 从上面的响应中复制token值
echo   2. 使用Postman或curl手动创建用户
echo   3. 或者使用Node.js版本的脚本

echo.
echo 📝 示例用户创建命令:
echo curl -X POST "http://localhost:5297/api/v1/Users/add" \
echo      -H "Content-Type: application/json" \
echo      -H "Authorization: Bearer YOUR_TOKEN_HERE" \
echo      -d "{\"userName\":\"doctor01\",\"realName\":\"张医生\",\"role\":1,\"roles\":[1],\"isActive\":true,\"email\":\"doctor01@lybt.com\",\"phoneNumber\":\"13800138001\"}"

echo.
echo 🎉 基础测试完成！
echo.

rem 清理临时文件
del login_response.json 2>nul

pause