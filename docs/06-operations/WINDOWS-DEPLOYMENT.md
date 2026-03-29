# Windows Server 部署指南

## 概述

本文档描述如何在 Windows Server 2019/2022 上部署 LYBT WebAPI，不使用 Docker。

## 部署方式

提供两种部署方式：

1. **Windows Service**（推荐）：独立运行，自动启动，适合后台服务
2. **IIS**：适合已有 IIS 环境，需要图形化管理界面

---

## 前置要求

### 系统要求

| 项目 | 要求 |
|------|------|
| 操作系统 | Windows Server 2019/2022 |
| CPU | 2 核及以上 |
| 内存 | 4GB 及以上 |
| 磁盘 | 20GB 可用空间 |

### 软件依赖

| 软件 | 版本 | 下载地址 |
|------|------|----------|
| .NET 8.0 Runtime | 8.0.x | https://dotnet.microsoft.com/download/dotnet/8.0 |
| SQL Server | 2019/2022 | https://www.microsoft.com/sql-server |
| IIS（可选） | 10.0+ | Windows Server 功能 |

### 安装 .NET 8.0 Hosting Bundle

```powershell
# 下载并安装 ASP.NET Core Runtime + IIS Module
# https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-aspnetcore-8.0.x-windows-hosting-bundle-installer

# 验证安装
dotnet --version
```

---

## 方式一：Windows Service 部署（推荐）

### 1. 自动部署（推荐）

```powershell
# 以管理员身份运行 PowerShell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# 运行部署脚本
.\deploy\windows\deploy.ps1 -DeployPath "C:\Services\LYBT-API" -Port 5000
```

### 2. 手动部署

#### 2.1 发布应用

```powershell
# 发布到指定目录
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj `
  -c Release `
  -o C:\Services\LYBT-API `
  --self-contained false
```

#### 2.2 创建 Windows Service

```powershell
# 创建服务
sc.exe create LYBT-API `
  binPath= "C:\Services\LYBT-API\LYBT.WebAPI.exe" `
  start= auto `
  displayName= "凌隐宝堂 WebAPI 服务"

# 设置服务描述
sc.exe description LYBT-API "凌隐宝堂中医诊所管理系统 WebAPI 服务"

# 启动服务
sc.exe start LYBT-API
```

#### 2.3 配置防火墙

```powershell
# 允许端口通过防火墙
New-NetFirewallRule `
  -DisplayName "LYBT-API-5000" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 5000 `
  -Action Allow
```

---

## 方式二：IIS 部署

### 1. 安装 IIS 和 ASP.NET Core Module

```powershell
# 安装 IIS 和所需功能
Install-WindowsFeature -Name Web-Server, Web-ASP-Net45, Web-ISAPI-Ext, Web-ISAPI-Filter, Web-HTTP-Redirect

# 安装 URL Rewrite Module（可选）
# https://www.iis.net/downloads/microsoft/url-rewrite
```

### 2. 发布应用

```powershell
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj `
  -c Release `
  -o C:\inetpub\lybt-api
```

### 3. 配置 IIS

```powershell
Import-Module WebAdministration

# 创建应用程序池
New-Item -Path IIS:\AppPools\LYBT-API -ItemType AppPool
Set-ItemProperty -Path IIS:\AppPools\LYBT-API -Name "managedRuntimeVersion" -Value ""

# 创建网站
New-Item -Path IIS:\Sites\LYBT-API `
  -Bindings @{protocol="http";bindingInformation=":8080:"} `
  -PhysicalPath "C:\inetpub\lybt-api"

Set-ItemProperty -Path IIS:\Sites\LYBT-API -Name "applicationPool" -Value "LYBT-API"
```

---

## 生产环境配置

### 1. 环境变量

```powershell
# 设置 JWT 密钥（必须，最少32字符）
[Environment]::SetEnvironmentVariable("JWT_SECRET", "your-secure-jwt-secret-key-min-32-chars-here", "Machine")

# 设置数据库连接字符串（如不使用 Windows 认证）
[Environment]::SetEnvironmentVariable("LYBT_CONNECTIONSTRING", "Server=localhost;Database=LYBTDB;User Id=sa;Password=YourStrongPassword;TrustServerCertificate=true", "Machine")

# 验证设置
[Environment]::GetEnvironmentVariable("JWT_SECRET", "Machine")
```

### 2. appsettings.Production.json

已创建 `src/Server/Services/LYBT.WebAPI/appsettings.Production.json`，包含生产环境优化配置：

- 连接池配置（Max Pool Size: 50）
- 日志轮转（保留30天）
- 速率限制优化
- CORS 配置

### 3. SQL Server 配置

```sql
-- 创建数据库
CREATE DATABASE LYBTDB;
GO

-- 创建登录用户（如不使用 Windows 认证）
CREATE LOGIN lybt_user WITH PASSWORD = 'YourStrongPassword';
GO

USE LYBTDB;
CREATE USER lybt_user FOR LOGIN lybt_user;
ALTER ROLE db_owner ADD MEMBER lybt_user;
GO
```

### 4. 数据库迁移

```powershell
# 执行数据库迁移
dotnet ef database update `
  --project src/Server/Core/LYBT.Infrastructure `
  --startup-project src/Server/Services/LYBT.WebAPI
```

---

## SSL/HTTPS 配置

### 方式1：使用自签名证书（测试环境）

```powershell
# 生成自签名证书
$cert = New-SelfSignedCertificate `
  -DnsName "lybt.local" `
  -CertStoreLocation "cert:\LocalMachine\My" `
  -KeyAlgorithm RSA `
  -KeyLength 2048

# 导出证书
$thumbprint = $cert.Thumbprint
Export-PfxCertificate `
  -Cert "cert:\LocalMachine\My\$thumbprint" `
  -FilePath "C:\Services\LYBT-API\lybt.pfx" `
  -Password (ConvertTo-SecureString -String "YourPassword" -Force -AsPlainText)
```

### 方式2：使用正式证书

```powershell
# 在 appsettings.Production.json 中配置证书路径
"Kestrel": {
  "Endpoints": {
    "Https": {
      "Url": "https://0.0.0.0:5001",
      "Certificate": {
        "Path": "C:\\Services\\LYBT-API\\cert.pfx",
        "Password": "${CERT_PASSWORD}"
      }
    }
  }
}
```

---

## 服务管理

### 常用命令

```powershell
# 查看服务状态
Get-Service -Name "LYBT-API"

# 启动服务
Start-Service -Name "LYBT-API"

# 停止服务
Stop-Service -Name "LYBT-API"

# 重启服务
Restart-Service -Name "LYBT-API"

# 查看服务日志
Get-EventLog -LogName Application -Source "LYBT-API" -Newest 20
```

### 日志查看

```powershell
# 查看最新日志
Get-Content 'C:\Services\LYBT-API\logs\lybt-web-api-$(Get-Date -Format "yyyyMMdd").log' -Tail 50

# 实时监控日志
Get-Content 'C:\Services\LYBT-API\logs\lybt-web-api-$(Get-Date -Format "yyyyMMdd").log' -Wait
```

---

## 健康检查

```powershell
# 测试服务是否正常运行
Invoke-RestMethod -Uri "http://localhost:5000/health" -Method GET

# 测试认证端点
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/auth/validate" -Method GET
```

---

## 故障排查

### 服务无法启动

```powershell
# 1. 检查事件日志
Get-EventLog -LogName Application -Newest 10 | Where-Object { $_.Source -like "*LYBT*" }

# 2. 检查端口占用
netstat -ano | findstr :5000

# 3. 检查文件权限
icacls "C:\Services\LYBT-API"

# 4. 直接运行查看错误
cd C:\Services\LYBT-API
.\LYBT.WebAPI.exe
```

### 数据库连接失败

```powershell
# 测试数据库连接
dotnet tool install -g dotnet-sqltest
sqltest -c "Server=localhost;Database=LYBTDB;Trusted_Connection=True"

# 检查 SQL Server 服务状态
Get-Service -Name "MSSQLSERVER"
```

### 日志不写入

```powershell
# 检查日志目录权限
icacls "C:\Services\LYBT-API\logs"

# 修复权限
icacls "C:\Services\LYBT-API\logs" /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F"
icacls "C:\Services\LYBT-API\logs" /grant "Administrators:(OI)(CI)F"
```

---

## 卸载

```powershell
# 使用卸载脚本
.\deploy\windows\uninstall.ps1

# 或手动卸载
sc.exe stop LYBT-API
sc.exe delete LYBT-API
Remove-Item -Path "C:\Services\LYBT-API" -Recurse -Force
```

---

## 生产环境检查清单

### 部署前检查

- [ ] Windows Server 2019/2022 已安装
- [ ] .NET 8.0 Hosting Bundle 已安装
- [ ] SQL Server 2019/2022 已安装并运行
- [ ] 防火墙已配置
- [ ] JWT_SECRET 环境变量已设置（≥32字符）

### 部署后检查

- [ ] 服务状态为 Running
- [ ] 健康检查端点可访问（/health）
- [ ] 数据库连接正常
- [ ] 日志文件正在写入
- [ ] 端口监听正常（netstat -an | findstr :5000）

### 安全加固（生产必需）

- [ ] JWT 密钥已更换为随机强密钥
- [ ] 默认密码已修改
- [ ] CORS 已配置为特定域名（非 *）
- [ ] SSL/TLS 证书已配置
- [ ] 数据库使用 SQL 认证而非 sa 账户
- [ ] 日志保留策略已配置
- [ ] 定期备份已配置

---

## 参考

- [.NET 8.0 Windows 托管文档](https://docs.microsoft.com/aspnet/core/host-and-deploy/windows-service)
- [IIS 上的 ASP.NET Core](https://docs.microsoft.com/aspnet/core/host-and-deploy/iis/)
- [SQL Server 连接字符串参考](https://docs.microsoft.com/sql/connect/ado-net/connection-string-syntax)
