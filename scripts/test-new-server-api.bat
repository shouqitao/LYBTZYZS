@echo off
chcp 65001 >nul
echo ====================================
echo   LYBT WebAPI 新服务器测试
echo   Remote Server: 60.190.215.86:5000
echo ====================================
echo.

set SERVER_URL=http://60.190.215.86:5000

echo [INFO] 测试服务器连通性...
ping -n 2 60.190.215.86 > nul
if %ERRORLEVEL% EQU 0 (
    echo [OK] 服务器网络连通
) else (
    echo [ERROR] 服务器网络不通
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
echo [INFO] 测试 Swagger JSON...
curl -s -I %SERVER_URL%/swagger/v1/swagger.json
if %ERRORLEVEL% EQU 0 (
    echo [OK] Swagger JSON 可访问
) else (
    echo [ERROR] Swagger JSON 无法访问
)

echo.
echo [INFO] 测试登录 API (v1 版本)...
curl -s -X POST %SERVER_URL%/api/v1/auth/login ^
-H "Content-Type: application/json" ^
-d "{\"username\":\"sysadmin\",\"password\":\"Admin@123456\",\"rememberMe\":false}"

echo.
echo [INFO] 测试不同的登录凭据...

echo.
echo [INFO] 测试 admin/admin...
curl -s -X POST %SERVER_URL%/api/v1/auth/login ^
-H "Content-Type: application/json" ^
-d "{\"username\":\"admin\",\"password\":\"admin\",\"rememberMe\":false}"

echo.
echo [INFO] 测试 admin/Admin@123456...
curl -s -X POST %SERVER_URL%/api/v1/auth/login ^
-H "Content-Type: application/json" ^
-d "{\"username\":\"admin\",\"password\":\"Admin@123456\",\"rememberMe\":false}"

echo.
echo [INFO] 测试 sysadmin/admin...
curl -s -X POST %SERVER_URL%/api/v1/auth/login ^
-H "Content-Type: application/json" ^
-d "{\"username\":\"sysadmin\",\"password\":\"admin\",\"rememberMe\":false}"

echo.
echo [INFO] 获取 API 版本信息...
curl -s %SERVER_URL%/swagger/v1/swagger.json | findstr "\"version\""

echo.
echo ====================================
echo   服务器状态总结
echo ====================================
echo.

echo [INFO] 服务器信息：
echo - 地址: %SERVER_URL%
echo - Swagger 文档: %SERVER_URL%/swagger/index.html
echo - API 端点: %SERVER_URL%/api/v1/*

echo.
echo [INFO] 测试的登录凭据：
echo - sysadmin / Admin@123456 (默认管理员)
echo - admin / admin (简单管理员)
echo - admin / Admin@123456 (管理员+复杂密码)
echo - sysadmin / admin (系统管理员+简单密码)

echo.
echo 测试完成！请检查上述输出结果。
echo.
pause