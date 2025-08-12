@echo off
REM SSL证书生成脚本 - UltraThink重构安全配置
chcp 65001 >nul
title SSL证书生成器

echo.
echo ==========================================
echo   凌隐宝堂SSL证书生成器
echo   UltraThink重构安全配置生成
echo ==========================================
echo.

REM 检查OpenSSL是否存在
where openssl >nul 2>&1
if errorlevel 1 (
    echo ❌ OpenSSL未找到，请安装OpenSSL并添加到PATH环境变量
    echo 下载地址: https://slproweb.com/products/Win32OpenSSL.html
    pause
    exit /b 1
)

echo ✅ 检测到OpenSSL，开始生成证书...
echo.

REM 创建SSL目录
if not exist "Docker\nginx\ssl" mkdir Docker\nginx\ssl
cd Docker\nginx\ssl

echo 🔐 生成自签名SSL证书...
echo.

REM 生成私钥
echo 生成私钥 (lybt.key)...
openssl genrsa -out lybt.key 2048
if errorlevel 1 (
    echo ❌ 私钥生成失败
    pause
    exit /b 1
)
echo ✅ 私钥生成成功

echo.
echo 生成证书签名请求 (lybt.csr)...

REM 创建证书配置文件
echo [req] > cert.conf
echo distinguished_name = req_distinguished_name >> cert.conf
echo req_extensions = v3_req >> cert.conf
echo prompt = no >> cert.conf
echo. >> cert.conf
echo [req_distinguished_name] >> cert.conf
echo C = CN >> cert.conf
echo ST = Beijing >> cert.conf
echo L = Beijing >> cert.conf
echo O = LYBT Medical Clinic >> cert.conf
echo OU = IT Department >> cert.conf
echo CN = lybt.local >> cert.conf
echo. >> cert.conf
echo [v3_req] >> cert.conf
echo keyUsage = keyEncipherment, dataEncipherment >> cert.conf
echo extendedKeyUsage = serverAuth >> cert.conf
echo subjectAltName = @alt_names >> cert.conf
echo. >> cert.conf
echo [alt_names] >> cert.conf
echo DNS.1 = lybt.local >> cert.conf
echo DNS.2 = localhost >> cert.conf
echo DNS.3 = *.lybt.local >> cert.conf
echo IP.1 = 127.0.0.1 >> cert.conf
echo IP.2 = ::1 >> cert.conf

REM 生成CSR
openssl req -new -key lybt.key -out lybt.csr -config cert.conf
if errorlevel 1 (
    echo ❌ CSR生成失败
    pause
    exit /b 1
)
echo ✅ CSR生成成功

echo.
echo 生成自签名证书 (lybt.crt)...

REM 生成自签名证书（有效期1年）
openssl x509 -req -in lybt.csr -signkey lybt.key -out lybt.crt -days 365 -extensions v3_req -extfile cert.conf
if errorlevel 1 (
    echo ❌ 证书生成失败
    pause
    exit /b 1
)
echo ✅ 证书生成成功

echo.
echo 🔍 验证生成的证书...
openssl x509 -in lybt.crt -text -noout | findstr "Subject:\|Not Before:\|Not After:\|DNS:\|IP:"

echo.
echo 设置文件权限...
REM 在Windows上设置只读权限
attrib +R lybt.key
attrib +R lybt.crt

echo.
echo 清理临时文件...
del lybt.csr
del cert.conf

echo.
echo ==========================================
echo ✅ SSL证书生成完成！
echo ==========================================
echo.
echo 📁 证书文件位置:
echo   - 私钥: Docker\nginx\ssl\lybt.key
echo   - 证书: Docker\nginx\ssl\lybt.crt
echo.
echo 🌐 支持的域名:
echo   - lybt.local
echo   - localhost
echo   - 127.0.0.1
echo   - *.lybt.local
echo.
echo 💡 使用说明:
echo   1. 证书有效期为1年
echo   2. 这是自签名证书，浏览器会显示安全警告
echo   3. 生产环境建议使用CA签名的正式证书
echo   4. 可以将lybt.local添加到hosts文件指向127.0.0.1
echo.
echo 📝 添加到hosts文件 (需要管理员权限):
echo   127.0.0.1    lybt.local
echo   127.0.0.1    api.lybt.local
echo   127.0.0.1    admin.lybt.local
echo.

cd ..\..\..
pause