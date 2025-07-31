@echo off
chcp 65001 >nul
echo ====================================
echo   LYBT WebAPI 远程服务测试
echo   Remote Server: 192.168.190.243:5000
echo ====================================
echo.

set SERVER_URL=http://192.168.190.243:5000

echo [INFO] 测试 Health Check...
curl -s %SERVER_URL%/health
if %ERRORLEVEL% EQU 0 (
    echo [OK] Health Check 成功
) else (
    echo [ERROR] Health Check 失败
)

echo.
echo [INFO] 测试 Swagger 文档...
curl -s -I %SERVER_URL%/swagger/index.html
if %ERRORLEVEL% EQU 0 (
    echo [OK] Swagger 文档可访问
) else (
    echo [ERROR] Swagger 文档无法访问
)

echo.
echo [INFO] 测试认证 API - 密码哈希...
curl -s "%SERVER_URL%/api/v1.0/auth/hashPassword?password=test123"
if %ERRORLEVEL% EQU 0 (
    echo [OK] 认证 API 响应正常
) else (
    echo [ERROR] 认证 API 无响应
)

echo.
echo [INFO] 测试登录 API...
curl -s -X POST %SERVER_URL%/api/v1.0/auth/login ^
-H "Content-Type: application/json" ^
-d "{\"username\":\"sysadmin\",\"password\":\"Admin@123456\",\"rememberMe\":true}"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [OK] 登录 API 响应正常
) else (
    echo.
    echo [ERROR] 登录 API 调用失败
)

echo.
echo [INFO] 测试用户 API...
curl -s -I %SERVER_URL%/api/v1.0/users
if %ERRORLEVEL% EQU 0 (
    echo [OK] 用户 API 端点存在
) else (
    echo [ERROR] 用户 API 端点不可访问
)

echo.
echo [INFO] 测试草药 API...
curl -s -I %SERVER_URL%/api/v1.0/herbs
if %ERRORLEVEL% EQU 0 (
    echo [OK] 草药 API 端点存在
) else (
    echo [ERROR] 草药 API 端点不可访问
)

echo.
echo ====================================
echo   完整 API 测试报告
echo ====================================
echo.

echo [INFO] 可访问的端点：
echo - Health: %SERVER_URL%/health
echo - Swagger: %SERVER_URL%/swagger
echo - 认证API: %SERVER_URL%/api/v1.0/auth/*
echo - 用户API: %SERVER_URL%/api/v1.0/users/*
echo - 草药API: %SERVER_URL%/api/v1.0/herbs/*

echo.
echo [INFO] 默认管理员账号：
echo - 用户名: sysadmin
echo - 密码: Admin@123456

echo.
echo 测试完成！请检查上述输出结果。
pause