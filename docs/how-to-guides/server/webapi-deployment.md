# WebAPI 部署指南 - LYBT 中医诊所诊疗系统

## 📋 文档信息

- **文档版本**: v1.0
- **最后更新**: 2025-10-30
- **适用范围**: LYBT.WebAPI 服务端部署
- **技术栈**: .NET 8, ASP.NET Core, Kestrel/IIS, Windows Service
- **读者对象**: 运维工程师、系统管理员、DevOps工程师

---

## 1. 概述

### 1.1 部署模式对比

| 模式 | 适用场景 | 优势 | 劣势 |
|------|---------|------|------|
| **Kestrel控制台** | 开发/测试环境 | 快速启动、实时日志、易调试 | 不适合生产、进程管理缺失 |
| **Windows Service** | 生产环境（单机） | 自动启动、后台运行、系统集成 | Windows专属、配置复杂 |
| **IIS托管** | 企业环境 | 成熟稳定、集中管理、负载均衡 | 配置繁琐、资源占用高 |
| **Docker容器** | 云环境/微服务 | 隔离性好、可移植、快速部署 | MVP阶段暂不支持 |

**MVP阶段推荐方案**：
- 开发环境 → Kestrel控制台模式（快速调试）
- 生产环境 → Windows Service模式（稳定可靠）
- IIS托管 → 作为备选方案（企业需求）

### 1.2 核心技术栈

```
运行时: .NET 8.0
Web框架: ASP.NET Core 8.0
Web服务器: Kestrel（开发）+ Windows Service（生产）
数据库: SQL Server 2022 Express（开发）/ SQL Server 2022（生产）
日志系统: Serilog（Console + File + MSSqlServer）
认证授权: JWT Bearer + ASP.NET Core Identity
配置管理: appsettings.json + 环境变量
健康检查: ASP.NET Core Health Checks
```

### 1.3 MVP约束与安全策略

**技术黑名单**（禁止使用）：
- ❌ Redis缓存（使用MemoryCache替代）
- ❌ RabbitMQ/Kafka消息队列（使用InMemoryEventBus替代）
- ❌ Docker容器化（生产环境使用Windows Service）
- ❌ Kubernetes编排（MVP阶段无需）
- ❌ 反向代理（Nginx/HAProxy，MVP阶段使用IIS或Kestrel直接访问）

**安全基线**：
- ✅ HTTPS强制（生产环境）
- ✅ JWT认证（15-30分钟AccessToken）
- ✅ 速率限制（登录3-5次/5分钟、API 500-1000次/分钟）
- ✅ 配置验证（生产环境启动前强制验证）
- ✅ 敏感信息加密（JWT密钥、数据库密码通过环境变量）

---

## 2. 部署前准备

### 2.1 环境要求

**最低要求**：
```
操作系统: Windows 10/11 或 Windows Server 2016+
.NET SDK: .NET 8.0 SDK（开发环境）
.NET Runtime: ASP.NET Core Runtime 8.0（生产环境）
数据库: SQL Server 2022 Express或以上
内存: 4GB RAM（最低）/ 8GB RAM（推荐）
磁盘: 20GB可用空间（包含日志）
网络: 开放5000（HTTP）、5001（HTTPS）端口
```

**推荐配置**：
```
CPU: 4核心（物理核心）
内存: 16GB RAM
磁盘: SSD 50GB（系统+应用）+ 100GB（数据库+日志）
网络: 千兆以太网
```

### 2.2 工具依赖

**开发环境**：
```bash
# 1. 安装 .NET 8 SDK
dotnet --version  # 验证版本 >= 8.0

# 2. 安装 SQL Server 2022 Express
# 下载地址: https://www.microsoft.com/sql-server/sql-server-downloads

# 3. 安装 SQL Server Management Studio (SSMS)
# 下载地址: https://aka.ms/ssmsfullsetup

# 4. 安装 PowerShell 7+（推荐）
pwsh --version
```

**生产环境**：
```bash
# 1. 安装 ASP.NET Core Runtime 8.0（仅需Runtime，无需SDK）
dotnet --list-runtimes  # 验证 Microsoft.AspNetCore.App 8.0

# 2. 安装 SQL Server 2022
# 选择合适的版本（Standard/Enterprise）

# 3. 配置 Windows Firewall
New-NetFirewallRule -DisplayName "LYBT WebAPI HTTP" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
New-NetFirewallRule -DisplayName "LYBT WebAPI HTTPS" -Direction Inbound -Protocol TCP -LocalPort 5001 -Action Allow
```

### 2.3 权限要求

**开发环境**：
- 普通用户权限（无需管理员）
- 数据库访问权限（Windows集成认证或SQL Server认证）

**生产环境**：
- 管理员权限（安装Windows Service）
- 数据库db_owner权限（迁移和初始化）
- 日志目录写权限（C:\logs\LYBT\）
- 证书管理权限（HTTPS证书绑定）

---

## 3. 开发环境部署（Kestrel控制台模式）

### 3.1 环境变量配置（可选）

开发环境默认使用`appsettings.json`硬编码配置，无需环境变量。

**如需覆盖配置**（推荐用于团队协作）：
```powershell
# PowerShell（仅当前会话）
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=LYBTDB_Dev;Trusted_Connection=True;TrustServerCertificate=true"

# 永久环境变量（系统级，谨慎使用）
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development", "Machine")
```

### 3.2 数据库初始化

```bash
# 1. 还原数据库（从备份文件）
# 如果有初始数据库备份文件
Restore-SqlDatabase -ServerInstance localhost -Database LYBTDB -BackupFile "D:\backups\LYBTDB_Init.bak"

# 2. 或使用EF Core迁移创建数据库
cd D:\source\repos\LYBTZYZS\src\Server\Services\LYBT.WebAPI

# 应用所有迁移
dotnet ef database update --project ../../../Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj

# 3. 验证数据库连接
sqlcmd -S localhost -E -Q "SELECT name FROM sys.databases WHERE name='LYBTDB'"
```

### 3.3 启动WebAPI

```bash
# 1. 进入WebAPI项目目录
cd D:\source\repos\LYBTZYZS\src\Server\Services\LYBT.WebAPI

# 2. 还原依赖（首次运行）
dotnet restore

# 3. 编译项目
dotnet build -c Debug

# 4. 运行WebAPI（开发模式）
dotnet run --launch-profile https

# 预期输出：
# ╔══════════════════════════════════════════════════════════╗
# ║                LYBT WebAPI 开发模式                      ║
# ║            凌隐宝堂中医诊所诊疗系统 WebAPI                ║
# ╚══════════════════════════════════════════════════════════╝
#
# [启动] ✅ 环境: Development
# [启动] ✅ 服务地址: https://localhost:5001
# [启动] ✅ Swagger文档: https://localhost:5001/swagger
# [启动] ✅ 启动时间: 2025-10-30 14:30:00
# [启动] ✅ 数据库连接正常
#
# 🚀 服务启动完成！按 Ctrl+C 停止服务
```

### 3.4 验证服务

**1. 访问Swagger UI**：
```
https://localhost:5001/swagger
```

**2. 健康检查端点**：
```bash
# 检查服务健康状态
curl https://localhost:5001/health

# 预期响应
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database connection is healthy"
    }
  ]
}
```

**3. 测试登录端点**：
```bash
# 使用默认管理员账号登录
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "sysadmin",
    "password": "LybtAdmin2025@SecurePass!"
  }'

# 预期响应（200 OK）
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "...",
  "expiresIn": 1800
}
```

### 3.5 停止服务

```bash
# 方法1: 在控制台按 Ctrl+C（推荐）
# 触发优雅关闭流程，日志输出：
# ⏹️ 正在停止服务...
# ✅ 服务已安全停止

# 方法2: 关闭终端窗口（不推荐，可能导致数据库连接未释放）
```

---

## 4. 生产环境部署（Windows Service模式）

### 4.1 配置环境变量（⚠️ 必需）

生产环境**必须**通过环境变量配置敏感信息，禁止硬编码。

**步骤1：创建环境变量配置脚本**

创建文件 `D:\deploy\LYBT\set-production-env.ps1`：

```powershell
# ========== 生产环境变量配置 ==========
# ⚠️ 请根据实际环境修改以下值

# 1. 环境标识
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")

# 2. 数据库连接字符串（⚠️ 必需）
$dbConnectionString = "Server=PROD-SQL-SERVER;Database=LYBTDB_Prod;User Id=lybt_app_user;Password=YourStrongPassword123!;TrustServerCertificate=false;Encrypt=true;MultipleActiveResultSets=true;Connection Timeout=60;Command Timeout=60;Max Pool Size=100;Min Pool Size=5;Pooling=true"
[System.Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $dbConnectionString, "Machine")

# 3. JWT密钥（⚠️ 必需 - 使用强随机密钥）
$jwtSecret = "YOUR_PRODUCTION_JWT_SECRET_KEY_AT_LEAST_32_CHARACTERS_BASE64_ENCODED"
[System.Environment]::SetEnvironmentVariable("Lybt__Authentication__Jwt__SecretKey", $jwtSecret, "Machine")

# 4. 系统管理员配置（⚠️ 必需）
[System.Environment]::SetEnvironmentVariable("Lybt__Authentication__DefaultPasswords__SysAdminPassword", "ProductionAdminPass@2025!", "Machine")
[System.Environment]::SetEnvironmentVariable("Lybt__Authentication__DefaultPasswords__NewUserPassword", "ProductionUserPass@2025!", "Machine")

# 5. 管理员账户信息
[System.Environment]::SetEnvironmentVariable("Lybt__Business__SystemAdmin__Username", "prod_admin", "Machine")
[System.Environment]::SetEnvironmentVariable("Lybt__Business__SystemAdmin__Email", "admin@example.com", "Machine")

# 6. 允许的主机名（可选）
[System.Environment]::SetEnvironmentVariable("AllowedHosts", "api.example.com;*.example.com", "Machine")

Write-Host "✅ 生产环境变量配置完成" -ForegroundColor Green
Write-Host "⚠️ 请重启系统或重新登录以使环境变量生效" -ForegroundColor Yellow
```

**步骤2：执行配置脚本**

```powershell
# 以管理员身份运行PowerShell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
D:\deploy\LYBT\set-production-env.ps1

# 验证环境变量（重新打开PowerShell）
[System.Environment]::GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Machine")
[System.Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Machine")
```

### 4.2 生成JWT密钥

```csharp
// 使用C#生成强随机JWT密钥
using System;
using System.Security.Cryptography;

var keyBytes = new byte[64]; // 512 bits
using (var rng = RandomNumberGenerator.Create())
{
    rng.GetBytes(keyBytes);
}
var base64Key = Convert.ToBase64String(keyBytes);
Console.WriteLine($"JWT Secret Key (Base64): {base64Key}");

// 输出示例：
// JWT Secret Key (Base64): J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==
```

**或使用PowerShell生成**：

```powershell
# 生成64字节（512位）随机密钥
$keyBytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($keyBytes)
$base64Key = [Convert]::ToBase64String($keyBytes)
Write-Host "JWT Secret Key (Base64): $base64Key"
```

### 4.3 发布应用程序

```bash
# 1. 发布为自包含应用（推荐）
dotnet publish D:\source\repos\LYBTZYZS\src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj `
  -c Release `
  -o D:\deploy\LYBT\WebAPI `
  --self-contained true `
  -r win-x64

# 参数说明：
# -c Release: 发布Release版本（优化编译）
# -o: 输出目录
# --self-contained true: 包含.NET Runtime（无需服务器安装.NET）
# -r win-x64: 目标运行时标识符（Windows x64）

# 2. 验证发布输出
ls D:\deploy\LYBT\WebAPI\LYBT.WebAPI.exe  # 主程序
ls D:\deploy\LYBT\WebAPI\appsettings.Production.json  # 生产配置
```

### 4.4 安装Windows Service

**步骤1：安装服务**

```powershell
# 使用sc命令创建Windows服务
sc.exe create "LYBTWebAPI" `
  binPath= "D:\deploy\LYBT\WebAPI\LYBT.WebAPI.exe" `
  start= auto `
  DisplayName= "凌隐宝堂WebAPI服务" `
  description= "凌隐宝堂中医诊所诊疗系统WebAPI服务"

# 参数说明：
# start=auto: 系统启动时自动启动
# binPath: 可执行文件路径（注意等号后有空格）
```

**步骤2：配置服务恢复选项**

```powershell
# 配置服务失败后的恢复策略
sc.exe failure "LYBTWebAPI" reset= 86400 actions= restart/60000/restart/60000/restart/60000

# 参数说明：
# reset=86400: 24小时后重置失败计数
# actions=: 失败后的操作（重启，延迟60秒）
```

**步骤3：启动服务**

```powershell
# 启动服务
Start-Service -Name "LYBTWebAPI"

# 验证服务状态
Get-Service -Name "LYBTWebAPI"

# 预期输出：
# Status   Name               DisplayName
# ------   ----               -----------
# Running  LYBTWebAPI         凌隐宝堂WebAPI服务

# 查看服务日志（Windows事件查看器）
Get-EventLog -LogName Application -Source "LYBTWebAPI" -Newest 10
```

### 4.5 配置验证（⚠️ 强制执行）

生产环境启动时会自动执行`ProductionConfigurationValidator`验证：

**验证清单**（7个必需配置）：
1. ✅ 数据库连接字符串（`ConnectionStrings:DefaultConnection`）
2. ✅ JWT签名密钥（`Lybt:Authentication:Jwt:SecretKey`，≥32字符）
3. ✅ 管理员默认密码（`Lybt:Authentication:DefaultPasswords:SysAdminPassword`）
4. ✅ 新用户默认密码（`Lybt:Authentication:DefaultPasswords:NewUserPassword`）
5. ✅ 管理员用户名（`Lybt:Business:SystemAdmin:Username`）
6. ✅ 管理员邮箱（`Lybt:Business:SystemAdmin:Email`，邮箱格式）
7. ⚪ 允许的主机名（`AllowedHosts`，可选）

**验证失败示例**：

```
╔═══════════════════════════════════════════════════════════╗
║  ❌ Production 配置验证失败                               ║
╚═══════════════════════════════════════════════════════════╝

发现 2 个配置错误：

⚠️ CRITICAL 错误（必须修复）:

  [1] JWT 签名密钥
      配置路径: Lybt:Authentication:Jwt:SecretKey
      环境变量: Lybt__Authentication__Jwt__SecretKey
      问题: 配置值包含占位符: #{JWT_SECRET_KEY}#
      示例: [自动生成的 Base64 字符串，至少 32 字符]
      修复方法（Windows）:
      setx Lybt__Authentication__Jwt__SecretKey "<your-value>"
      修复方法（Linux）:
      export Lybt__Authentication__Jwt__SecretKey="<your-value>"

───────────────────────────────────────────────────────────
📖 详细配置指南: docs/deployment/production-setup.md
🔧 验证脚本: .\scripts\validate-production-config.ps1
```

**解决方法**：
1. 查看错误报告中的"环境变量"列
2. 使用`setx`命令设置缺失的环境变量
3. 重启Windows Service
4. 验证服务启动成功

---

## 5. IIS托管部署（备选方案）

### 5.1 安装IIS和ASP.NET Core模块

```powershell
# 1. 启用IIS功能
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationDevelopment
Enable-WindowsOptionalFeature -Online -FeatureName IIS-AspNet45

# 2. 下载并安装ASP.NET Core Hosting Bundle
# 下载地址: https://dotnet.microsoft.com/download/dotnet/8.0
# 选择 "Hosting Bundle" 版本

# 3. 验证ASP.NET Core模块安装
Get-WebGlobalModule | Where-Object {$_.Name -like "*AspNetCore*"}

# 预期输出：
# Name                       Image
# ----                       -----
# AspNetCoreModuleV2         %windir%\System32\inetsrv\aspnetcorev2.dll
```

### 5.2 创建IIS应用程序池

```powershell
# 1. 导入IIS管理模块
Import-Module WebAdministration

# 2. 创建应用程序池
New-WebAppPool -Name "LYBTWebAPIAppPool"

# 3. 配置应用程序池
Set-ItemProperty -Path "IIS:\AppPools\LYBTWebAPIAppPool" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\LYBTWebAPIAppPool" -Name "startMode" -Value "AlwaysRunning"
Set-ItemProperty -Path "IIS:\AppPools\LYBTWebAPIAppPool" -Name "processModel.idleTimeout" -Value "00:00:00"
Set-ItemProperty -Path "IIS:\AppPools\LYBTWebAPIAppPool" -Name "recycling.periodicRestart.time" -Value "00:00:00"

# 参数说明：
# managedRuntimeVersion="": 无托管代码（ASP.NET Core自托管）
# startMode=AlwaysRunning: 预加载模式（立即启动）
# idleTimeout=00:00:00: 永不超时（保持运行）
# periodicRestart.time=00:00:00: 禁用定期回收
```

### 5.3 创建IIS网站

```powershell
# 1. 创建网站
New-Website -Name "LYBTWebAPI" `
  -PhysicalPath "D:\deploy\LYBT\WebAPI" `
  -ApplicationPool "LYBTWebAPIAppPool" `
  -Port 80 `
  -HostHeader "api.example.com"

# 2. 配置HTTPS绑定（推荐）
New-WebBinding -Name "LYBTWebAPI" -Protocol https -Port 443 -HostHeader "api.example.com"

# 3. 绑定SSL证书（需先导入证书）
$cert = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*api.example.com*"}
$binding = Get-WebBinding -Name "LYBTWebAPI" -Protocol https
$binding.AddSslCertificate($cert.Thumbprint, "my")
```

### 5.4 配置web.config

IIS部署需要`web.config`文件配置ASP.NET Core模块。

创建 `D:\deploy\LYBT\WebAPI\web.config`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath=".\LYBT.WebAPI.exe"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess"
                  forwardWindowsAuthToken="false">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

**参数说明**：
- `hostingModel="inprocess"`: 进程内托管（性能更好）
- `stdoutLogEnabled="true"`: 启用stdout日志（调试用）
- `stdoutLogFile`: stdout日志路径
- `forwardWindowsAuthToken="false"`: 禁用Windows认证令牌转发

### 5.5 启动IIS网站

```powershell
# 1. 启动网站
Start-Website -Name "LYBTWebAPI"

# 2. 验证网站状态
Get-Website -Name "LYBTWebAPI"

# 预期输出：
# Name         : LYBTWebAPI
# State        : Started
# Bindings     : http/*:80:api.example.com, https/*:443:api.example.com

# 3. 测试访问（需在hosts文件配置域名解析）
# C:\Windows\System32\drivers\etc\hosts
# 127.0.0.1    api.example.com

curl http://api.example.com/health
```

---

## 6. 配置管理最佳实践

### 6.1 配置文件层级

```
配置优先级（从高到低）：
1. 环境变量（最高优先级，生产环境必须）
2. appsettings.{Environment}.json（环境专用配置）
3. appsettings.Security.json（生产环境敏感配置模板）
4. appsettings.json（基础配置模板）
```

**加载策略（Program.cs:19-37）**：

```csharp
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

if (environment == "Test")
{
    // 测试环境：仅加载 appsettings.Test.json
    configBuilder.AddJsonFile("appsettings.Test.json", optional: false);
}
else if (environment == "Development")
{
    // 开发环境：appsettings.json + appsettings.Development.json
    configBuilder.AddJsonFile("appsettings.json", optional: false);
    configBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true);
}
else
{
    // 生产环境：appsettings.Security.json + appsettings.Production.json
    configBuilder.AddJsonFile("appsettings.Security.json", optional: false);
    configBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true);
}

// 环境变量覆盖（所有环境）
configBuilder.AddEnvironmentVariables();
```

### 6.2 敏感信息管理

**❌ 错误做法（硬编码）**：

```json
// appsettings.Production.json（错误示例）
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-sql;Database=LYBTDB;User Id=sa;Password=admin123"
  },
  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "my-secret-key-12345"
      }
    }
  }
}
```

**✅ 正确做法（环境变量占位符）**：

```json
// appsettings.Production.json（正确示例）
{
  "ConnectionStrings": {
    "DefaultConnection": "#{DATABASE_CONNECTION_STRING}#"
  },
  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "#{JWT_SECRET_KEY}#"
      }
    }
  }
}
```

**环境变量设置**：

```powershell
# Windows（Machine级别）
[System.Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=prod-sql;...", "Machine")
[System.Environment]::SetEnvironmentVariable("Lybt__Authentication__Jwt__SecretKey", "your-strong-key", "Machine")

# Linux
export ConnectionStrings__DefaultConnection="Server=prod-sql;..."
export Lybt__Authentication__Jwt__SecretKey="your-strong-key"
```

**注意**：
- 环境变量键使用双下划线`__`替代冒号`:`（如`Lybt:Authentication:Jwt:SecretKey` → `Lybt__Authentication__Jwt__SecretKey`）
- 数组索引使用双下划线和数字（如`Lybt:Security:IpWhitelist:0` → `Lybt__Security__IpWhitelist__0`）

### 6.3 环境对比表

| 配置项 | Development | Production | 说明 |
|--------|------------|-----------|------|
| **JWT AccessToken过期** | 30分钟 | 15分钟 | 生产环境更短，提升安全性 |
| **JWT ClockSkew** | 300秒 | 60秒 | 时钟偏差容忍度 |
| **密码最小长度** | 8字符 | 12字符 | 生产环境更严格 |
| **登录限流** | 5次/分钟 | 3次/5分钟 | 生产环境防暴力破解 |
| **全局限流** | 200次/分钟 | 1000次/分钟 | 生产环境更宽松（多用户） |
| **数据库连接池Max** | 20 | 100 | 生产环境支持更多并发 |
| **数据库连接池Min** | 2 | 5 | 生产环境预热连接 |
| **内存缓存大小** | 100MB | 256MB | 生产环境更大缓存 |
| **日志级别** | Information | Warning | 生产环境减少日志量 |
| **Serilog批量提交** | 50条/5秒 | 100条/10秒 | 生产环境批量优化 |
| **自动迁移** | false | false | 所有环境禁用（手动迁移） |

---

## 7. 数据库部署与迁移

### 7.1 数据库创建

**方法1：使用EF Core迁移（推荐）**：

```bash
# 1. 安装EF Core工具（首次）
dotnet tool install --global dotnet-ef

# 2. 应用迁移到目标数据库
dotnet ef database update --project D:\source\repos\LYBTZYZS\src\Server\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj --startup-project D:\source\repos\LYBTZYZS\src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj --connection "Server=PROD-SQL;Database=LYBTDB_Prod;User Id=lybt_admin;Password=***"

# 3. 验证迁移历史
dotnet ef migrations list --project D:\source\repos\LYBTZYZS\src\Server\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj
```

**方法2：使用SQL脚本**：

```bash
# 1. 生成SQL脚本
dotnet ef migrations script --project D:\source\repos\LYBTZYZS\src\Server\Core\LYBT.Infrastructure\LYBT.Infrastructure.csproj --output D:\deploy\LYBT\Scripts\InitDB.sql

# 2. 在生产数据库执行脚本
sqlcmd -S PROD-SQL-SERVER -U lybt_admin -P *** -d LYBTDB_Prod -i D:\deploy\LYBT\Scripts\InitDB.sql
```

### 7.2 初始数据种子

WebAPI启动时会自动执行`DatabaseInitializationService`：

**自动创建内容**（src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs）：
1. ✅ 系统管理员账号（`Lybt:Business:SystemAdmin:Username`配置）
2. ✅ 默认角色（Admin, Doctor, Staff）
3. ✅ SystemLogs表（Serilog日志表，如不存在则自动创建）

**验证初始化**：

```sql
-- 1. 验证管理员账号
SELECT Id, UserName, Email, DisplayName FROM Users WHERE UserName = 'prod_admin';

-- 2. 验证角色
SELECT Id, Name FROM Roles;

-- 预期输出：
-- Admin
-- Doctor
-- Staff

-- 3. 验证日志表
SELECT TOP 10 * FROM SystemLogs ORDER BY Timestamp DESC;
```

### 7.3 数据库备份策略

**备份计划（推荐）**：

```sql
-- 1. 完整备份（每日）
BACKUP DATABASE LYBTDB_Prod
TO DISK = 'D:\backups\LYBTDB_Prod_Full_20251030.bak'
WITH FORMAT, COMPRESSION, STATS = 10;

-- 2. 差异备份（每4小时）
BACKUP DATABASE LYBTDB_Prod
TO DISK = 'D:\backups\LYBTDB_Prod_Diff_20251030_1400.bak'
WITH DIFFERENTIAL, COMPRESSION, STATS = 10;

-- 3. 事务日志备份（每15分钟）
BACKUP LOG LYBTDB_Prod
TO DISK = 'D:\backups\LYBTDB_Prod_Log_20251030_1415.trn'
WITH COMPRESSION, STATS = 10;
```

**自动化备份脚本（PowerShell + SQL Server Agent）**：

```powershell
# FullBackup-LYBTDB.ps1
$backupPath = "D:\backups\LYBT"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "$backupPath\LYBTDB_Prod_Full_$timestamp.bak"

Invoke-Sqlcmd -ServerInstance "PROD-SQL" -Query @"
BACKUP DATABASE LYBTDB_Prod
TO DISK = '$backupFile'
WITH FORMAT, COMPRESSION, STATS = 10;
"@

# 删除7天前的备份
Get-ChildItem -Path $backupPath -Filter "LYBTDB_Prod_Full_*.bak" |
  Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-7)} |
  Remove-Item -Force
```

### 7.4 数据库连接池优化

**配置对比**（appsettings.json vs appsettings.Production.json）：

```json
// Development（开发环境）
{
  "Lybt": {
    "Infrastructure": {
      "Database": {
        "ConnectionPool": {
          "MaxConnections": 20,     // 最大连接数
          "MinConnections": 2,      // 最小连接数
          "ConnectionTimeoutSeconds": 30,
          "CommandTimeoutSeconds": 30
        }
      }
    }
  }
}

// Production（生产环境）
{
  "Lybt": {
    "Infrastructure": {
      "Database": {
        "ConnectionPool": {
          "MaxConnections": 100,    // 5倍并发支持
          "MinConnections": 5,      // 预热连接
          "ConnectionTimeoutSeconds": 60,  // 更长超时
          "CommandTimeoutSeconds": 60
        }
      }
    }
  }
}
```

**连接字符串完整示例**：

```
Server=PROD-SQL-SERVER;
Database=LYBTDB_Prod;
User Id=lybt_app_user;
Password=***;
TrustServerCertificate=false;
Encrypt=true;
MultipleActiveResultSets=true;
Connection Timeout=60;
Command Timeout=60;
Max Pool Size=100;
Min Pool Size=5;
Pooling=true;
Application Name=LYBT.WebAPI
```

---

## 8. 日志和监控

### 8.1 Serilog配置

**日志输出目标**（3个Sink）：

1. **Console Sink**（控制台输出，开发环境实时查看）
2. **File Sink**（文件日志，所有环境保留30-90天）
3. **MSSqlServer Sink**（数据库日志，结构化查询）

**日志级别对比**：

| 日志级别 | Development | Production |
|---------|------------|-----------|
| **Default** | Information | Warning |
| **Microsoft.AspNetCore** | Warning | Error |
| **Microsoft.EntityFrameworkCore** | Warning | Error |
| **LYBT.Module** | Information | Information |
| **LYBT.WebAPI.Controllers** | Information | Warning |

**文件日志配置**：

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/lybt-web-api-.log",
          "rollingInterval": "Day",              // 每日滚动
          "retainedFileCountLimit": 30,          // 保留30天（开发）/ 90天（生产）
          "fileSizeLimitBytes": 10485760,        // 10MB（开发）/ 100MB（生产）
          "rollOnFileSizeLimit": true,           // 文件大小超限时滚动
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### 8.2 数据库日志表结构

**SystemLogs表**（自动创建，由Serilog MSSqlServer Sink管理）：

```sql
CREATE TABLE [dbo].[SystemLogs] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Level] NVARCHAR(128),                -- 日志级别（Information, Warning, Error, Fatal）
    [Timestamp] DATETIME NOT NULL,        -- 日志时间戳
    [Message] NVARCHAR(MAX),              -- 日志消息
    [Exception] NVARCHAR(MAX),            -- 异常堆栈
    [LoggerName] NVARCHAR(256),           -- 日志来源（类名）
    [Properties] NVARCHAR(MAX),           -- 附加属性（JSON）
    [UserId] UNIQUEIDENTIFIER,            -- 用户ID（自定义列）
    [RequestId] NVARCHAR(36),             -- 请求ID（自定义列）
    [MachineName] NVARCHAR(100),          -- 机器名（自定义列）
    [ThreadId] INT                         -- 线程ID（自定义列）
);

-- 创建索引（性能优化）
CREATE NONCLUSTERED INDEX IX_SystemLogs_Timestamp ON SystemLogs(Timestamp DESC);
CREATE NONCLUSTERED INDEX IX_SystemLogs_Level ON SystemLogs(Level);
CREATE NONCLUSTERED INDEX IX_SystemLogs_UserId ON SystemLogs(UserId);
```

**查询示例**：

```sql
-- 1. 查询最近1小时的Error级别日志
SELECT TOP 100
    Timestamp, Level, LoggerName, Message, Exception
FROM SystemLogs
WHERE Level IN ('Error', 'Fatal')
  AND Timestamp > DATEADD(HOUR, -1, GETDATE())
ORDER BY Timestamp DESC;

-- 2. 统计每小时的Error数量
SELECT
    DATEPART(HOUR, Timestamp) AS Hour,
    COUNT(*) AS ErrorCount
FROM SystemLogs
WHERE Level = 'Error'
  AND Timestamp > DATEADD(DAY, -1, GETDATE())
GROUP BY DATEPART(HOUR, Timestamp)
ORDER BY Hour;

-- 3. 查询特定用户的操作日志
SELECT
    Timestamp, LoggerName, Message
FROM SystemLogs
WHERE UserId = 'your-user-guid'
  AND Timestamp > DATEADD(DAY, -7, GETDATE())
ORDER BY Timestamp DESC;
```

### 8.3 日志清理策略

**自动清理脚本**（定期执行，避免日志表过大）：

```sql
-- 删除90天前的日志（生产环境）
DELETE FROM SystemLogs
WHERE Timestamp < DATEADD(DAY, -90, GETDATE());

-- 删除30天前的Information级别日志（仅保留Warning/Error/Fatal）
DELETE FROM SystemLogs
WHERE Level = 'Information'
  AND Timestamp < DATEADD(DAY, -30, GETDATE());
```

**SQL Server Agent作业配置**：

```sql
-- 创建每日清理作业
USE msdb;
GO

EXEC dbo.sp_add_job
    @job_name = N'LYBT_LogCleanup',
    @enabled = 1,
    @description = N'清理90天前的日志记录';

EXEC sp_add_jobstep
    @job_name = N'LYBT_LogCleanup',
    @step_name = N'DeleteOldLogs',
    @subsystem = N'TSQL',
    @command = N'DELETE FROM LYBTDB_Prod.dbo.SystemLogs WHERE Timestamp < DATEADD(DAY, -90, GETDATE());',
    @database_name = N'LYBTDB_Prod';

EXEC sp_add_schedule
    @schedule_name = N'Daily_2AM',
    @freq_type = 4,                    -- 每日
    @freq_interval = 1,
    @active_start_time = 020000;       -- 凌晨2:00执行

EXEC sp_attach_schedule
    @job_name = N'LYBT_LogCleanup',
    @schedule_name = N'Daily_2AM';

EXEC sp_add_jobserver
    @job_name = N'LYBT_LogCleanup',
    @server_name = N'(LOCAL)';
```

---

## 9. 安全配置

### 9.1 HTTPS证书配置

**开发环境（自签名证书）**：

```powershell
# 1. 生成开发证书
dotnet dev-certs https --trust

# 2. 验证证书
dotnet dev-certs https --check

# 预期输出：
# A valid HTTPS certificate is already present.
```

**生产环境（正式证书）**：

```powershell
# 方法1：从证书颁发机构（CA）购买

# 方法2：使用Let's Encrypt免费证书（需域名）
# 安装Certify The Web工具：https://certifytheweb.com

# 方法3：导入现有证书到服务器
Import-PfxCertificate -FilePath "D:\certs\api.example.com.pfx" -CertStoreLocation Cert:\LocalMachine\My -Password (ConvertTo-SecureString -String "cert-password" -AsPlainText -Force)

# 验证证书
Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*api.example.com*"}
```

**Kestrel HTTPS端点配置**（appsettings.json）：

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "D:\\certs\\api.example.com.pfx",
          "Password": "#{CERT_PASSWORD}#"
        }
      }
    }
  }
}
```

### 9.2 速率限制配置

**登录端点保护**（防暴力破解）：

```csharp
// AuthController.cs
[HttpPost("login")]
[EnableRateLimiting("Login")]  // 应用Login速率限制策略
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // 登录逻辑
}
```

**限流策略配置对比**：

| 策略 | Development | Production | 说明 |
|------|------------|-----------|------|
| **全局限流** | 200次/分钟 | 1000次/分钟 | 所有API端点 |
| **登录限流** | 5次/分钟 | 3次/5分钟 | 防暴力破解 |
| **API限流** | 100次/分钟 | 500次/分钟 | 业务API端点 |
| **白名单IP** | 127.0.0.1, ::1 | （无） | 开发环境宽松 |

**白名单IP配置**：

```json
// appsettings.Production.json
{
  "Lybt": {
    "Security": {
      "RateLimiting": {
        "WhitelistedIPs": []  // 生产环境无白名单，严格限流
      }
    }
  }
}
```

**超限响应**（HTTP 429 Too Many Requests）：

```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Please try again later."
}
```

### 9.3 JWT密钥轮换策略

**密钥轮换步骤**（零停机时间）：

```csharp
// Step 1: 配置多密钥验证（appsettings.Production.json）
{
  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "#{JWT_SECRET_KEY_PRIMARY}#",      // 主密钥（用于签名）
        "SecretKeys": [
          "#{JWT_SECRET_KEY_PRIMARY}#",                 // 主密钥（验证）
          "#{JWT_SECRET_KEY_SECONDARY}#"                // 旧密钥（验证）
        ]
      }
    }
  }
}

// Step 2: 生成新密钥
$newKeyBytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($newKeyBytes)
$newBase64Key = [Convert]::ToBase64String($newKeyBytes)

// Step 3: 设置新密钥为Primary，旧密钥为Secondary
[System.Environment]::SetEnvironmentVariable("JWT_SECRET_KEY_SECONDARY", $oldPrimaryKey, "Machine")
[System.Environment]::SetEnvironmentVariable("JWT_SECRET_KEY_PRIMARY", $newBase64Key, "Machine")

// Step 4: 重启WebAPI服务
Restart-Service -Name "LYBTWebAPI"

// Step 5: 等待旧Token过期（默认15分钟AccessToken）

// Step 6: 移除Secondary密钥
[System.Environment]::SetEnvironmentVariable("JWT_SECRET_KEY_SECONDARY", "", "Machine")
```

**轮换频率建议**：
- 正常情况：每6个月轮换一次
- 密钥泄漏：立即轮换
- 员工离职：1周内轮换
- 安全审计要求：按审计周期轮换

---

## 10. 性能调优

### 10.1 连接池优化

**配置参数详解**：

```json
{
  "Lybt": {
    "Infrastructure": {
      "Database": {
        "ConnectionPool": {
          "MaxConnections": 100,              // 最大连接数（根据并发量调整）
          "MinConnections": 5,                // 最小连接数（预热连接，避免冷启动）
          "ConnectionTimeoutSeconds": 60,     // 连接超时（秒）
          "CommandTimeoutSeconds": 60         // 命令超时（秒）
        }
      }
    }
  }
}
```

**连接池大小计算公式**：

```
MaxConnections = (并发用户数 × 每用户平均并发请求数 × 每请求平均执行时间) / 连接复用率

示例：
- 并发用户：20人
- 每用户并发请求：2个
- 平均执行时间：500ms = 0.5秒
- 连接复用率：0.8（80%的连接可以复用）

MaxConnections = (20 × 2 × 0.5) / 0.8 = 25

建议设置：25 × 1.5（缓冲系数）= 38，取整为40
```

**监控连接池使用率**：

```sql
-- 查询当前连接数
SELECT
    DB_NAME(dbid) AS DatabaseName,
    COUNT(dbid) AS ConnectionCount,
    loginame AS LoginName
FROM sys.sysprocesses
WHERE DB_NAME(dbid) = 'LYBTDB_Prod'
GROUP BY dbid, loginame;

-- 查询连接池统计（需启用DMV）
SELECT
    counter_name,
    cntr_value
FROM sys.dm_os_performance_counters
WHERE object_name LIKE '%SQLServer:General Statistics%'
  AND counter_name IN ('User Connections', 'Logins/sec', 'Logouts/sec');
```

### 10.2 内存缓存配置

**缓存策略对比**：

```json
// Development（开发环境）
{
  "Lybt": {
    "Infrastructure": {
      "Cache": {
        "MemoryCache": {
          "Enabled": true,
          "SizeLimit": 104857600,           // 100MB
          "DefaultExpirationMinutes": 5     // 默认5分钟过期
        }
      }
    }
  }
}

// Production（生产环境）
{
  "Lybt": {
    "Infrastructure": {
      "Cache": {
        "MemoryCache": {
          "Enabled": true,
          "SizeLimit": 268435456,           // 256MB
          "DefaultExpirationMinutes": 30    // 默认30分钟过期
        }
      }
    }
  }
}
```

**输出缓存策略**（`UnifiedServiceRegistration.cs:122-157`）：

```csharp
services.AddOutputCache(options =>
{
    // 草药数据缓存1小时（读多写少）
    options.AddPolicy("HerbsCache", builder =>
        builder.Expire(TimeSpan.FromHours(1))
               .Tag("herbs"));

    // 配方模板缓存2小时（读多写少）
    options.AddPolicy("FormulasCache", builder =>
        builder.Expire(TimeSpan.FromHours(2))
               .Tag("formulas"));

    // 患者数据缓存30分钟（中等更新频率）
    options.AddPolicy("PatientsCache", builder =>
        builder.Expire(TimeSpan.FromMinutes(30))
               .Tag("patients"));

    // 处方缓存10分钟（更新频繁）
    options.AddPolicy("PrescriptionsCache", builder =>
        builder.Expire(TimeSpan.FromMinutes(10))
               .Tag("prescriptions"));
});
```

**Controller应用缓存**：

```csharp
[HttpGet]
[OutputCache(PolicyName = "HerbsCache")]  // 应用草药缓存策略
public async Task<IActionResult> GetHerbs()
{
    var herbs = await _herbService.GetAllAsync();
    return Ok(herbs);
}
```

**缓存失效**：

```csharp
// 在创建/更新/删除操作后失效缓存
[HttpPost]
public async Task<IActionResult> CreateHerb([FromBody] CreateHerbDto dto)
{
    var herb = await _herbService.CreateAsync(dto);

    // 失效草药缓存
    _outputCacheStore.EvictByTagAsync("herbs", cancellationToken);

    return CreatedAtAction(nameof(GetHerbById), new { id = herb.Id }, herb);
}
```

### 10.3 异步编程优化

**全异步方法链**（避免阻塞）：

```csharp
// ❌ 错误做法（阻塞主线程）
public IActionResult GetUsers()
{
    var users = _userService.GetAllAsync().Result;  // 阻塞！
    return Ok(users);
}

// ✅ 正确做法（完全异步）
public async Task<IActionResult> GetUsers()
{
    var users = await _userService.GetAllAsync();
    return Ok(users);
}
```

**并发查询优化**（Task.WhenAll）：

```csharp
// ✅ 并发查询多个数据源
public async Task<IActionResult> GetDashboardData()
{
    // 并发执行3个查询
    var usersTask = _userService.GetCountAsync();
    var patientsTask = _patientService.GetCountAsync();
    var casesTask = _medicalCaseService.GetCountAsync();

    await Task.WhenAll(usersTask, patientsTask, casesTask);

    return Ok(new
    {
        UserCount = usersTask.Result,
        PatientCount = patientsTask.Result,
        CaseCount = casesTask.Result
    });
}
```

---

## 11. 常见问题与故障排查

### 11.1 问题1：配置验证失败

**症状**：
```
❌ Production 配置验证失败
发现 1 个配置错误：

⚠️ CRITICAL 错误（必须修复）:
  [1] JWT 签名密钥
      配置路径: Lybt:Authentication:Jwt:SecretKey
      环境变量: Lybt__Authentication__Jwt__SecretKey
      问题: 配置值包含占位符: #{JWT_SECRET_KEY}#
```

**原因分析**：
1. 环境变量未设置或拼写错误
2. 环境变量未生效（未重启服务/系统）
3. 环境变量作用域错误（User级别 vs Machine级别）

**解决方法**：

```powershell
# Step 1: 验证环境变量是否存在
[System.Environment]::GetEnvironmentVariable("Lybt__Authentication__Jwt__SecretKey", "Machine")

# 如果返回null，表示未设置

# Step 2: 设置环境变量（使用Machine级别）
[System.Environment]::SetEnvironmentVariable("Lybt__Authentication__Jwt__SecretKey", "your-base64-key-at-least-32-chars", "Machine")

# Step 3: 验证设置成功
[System.Environment]::GetEnvironmentVariable("Lybt__Authentication__Jwt__SecretKey", "Machine")

# Step 4: 重启Windows Service
Restart-Service -Name "LYBTWebAPI"

# Step 5: 查看服务日志验证启动成功
Get-EventLog -LogName Application -Source "LYBTWebAPI" -Newest 10
```

### 11.2 问题2：服务启动失败

**症状**：
```
Windows服务启动后立即停止
事件查看器显示：应用程序启动失败
```

**诊断步骤**：

```powershell
# Step 1: 手动运行可执行文件，查看详细错误
cd D:\deploy\LYBT\WebAPI
.\LYBT.WebAPI.exe

# 预期：显示详细的启动错误信息

# Step 2: 检查stdout日志（如果配置了）
Get-Content -Path "D:\deploy\LYBT\WebAPI\logs\stdout_*.log" -Tail 50

# Step 3: 检查Serilog文件日志
Get-Content -Path "D:\deploy\LYBT\WebAPI\logs\lybt-web-api-*.log" -Tail 50
```

**常见原因与解决方法**：

| 原因 | 解决方法 |
|------|---------|
| **端口被占用** | `netstat -ano | findstr :5001` → 杀死占用进程或修改端口 |
| **数据库连接失败** | 验证连接字符串、SQL Server服务运行状态 |
| **权限不足** | 以管理员身份运行服务，或授予日志目录写权限 |
| **DLL缺失** | 重新发布（`--self-contained true`） |
| **appsettings.json丢失** | 确保发布输出包含所有配置文件 |

### 11.3 问题3：数据库连接超时

**症状**：
```
Microsoft.Data.SqlClient.SqlException: Timeout expired.
The timeout period elapsed prior to completion of the operation or the server is not responding.
```

**诊断步骤**：

```sql
-- Step 1: 检查SQL Server服务状态
Get-Service -Name "MSSQL*"

-- Step 2: 测试网络连接
Test-NetConnection -ComputerName "PROD-SQL-SERVER" -Port 1433

-- Step 3: 检查当前连接数
SELECT
    DB_NAME(dbid) AS DatabaseName,
    COUNT(dbid) AS ConnectionCount
FROM sys.sysprocesses
WHERE DB_NAME(dbid) = 'LYBTDB_Prod'
GROUP BY dbid;

-- Step 4: 检查长时间运行的查询
SELECT
    session_id,
    start_time,
    status,
    command,
    DATEDIFF(SECOND, start_time, GETDATE()) AS DurationSeconds,
    wait_type,
    blocking_session_id,
    text AS QueryText
FROM sys.dm_exec_requests
CROSS APPLY sys.dm_exec_sql_text(sql_handle)
WHERE DATEDIFF(SECOND, start_time, GETDATE()) > 30  -- 运行超过30秒
ORDER BY start_time;
```

**解决方法**：

```json
// 1. 增加连接超时和命令超时
{
  "Lybt": {
    "Infrastructure": {
      "Database": {
        "ConnectionPool": {
          "ConnectionTimeoutSeconds": 120,  // 从60秒增加到120秒
          "CommandTimeoutSeconds": 120      // 从60秒增加到120秒
        }
      }
    }
  }
}

// 2. 优化慢查询（创建索引、重写查询）

// 3. 增加连接池大小
{
  "Lybt": {
    "Infrastructure": {
      "Database": {
        "ConnectionPool": {
          "MaxConnections": 200  // 从100增加到200
        }
      }
    }
  }
}
```

### 11.4 问题4：内存泄漏

**症状**：
- 服务运行一段时间后内存占用持续增长
- 最终导致OutOfMemoryException或服务崩溃

**诊断工具**：

```powershell
# 1. 使用dotnet-counters监控内存
dotnet tool install --global dotnet-counters
dotnet-counters monitor --process-id <pid> System.Runtime

# 2. 生成内存转储
dotnet tool install --global dotnet-dump
dotnet-dump collect --process-id <pid>

# 3. 分析内存转储
dotnet-dump analyze <dump-file>
> dumpheap -stat
> gcroot <object-address>
```

**常见原因**：

1. **未释放DbContext**：
```csharp
// ❌ 错误做法
public class MyService
{
    private readonly AppDbContext _context;  // Singleton服务持有DbContext
}

// ✅ 正确做法
public class MyService
{
    private readonly IServiceProvider _serviceProvider;

    public async Task DoWork()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // 使用context
    } // context自动释放
}
```

2. **未释放HttpClient**：
```csharp
// ❌ 错误做法
using (var client = new HttpClient())  // 每次创建新实例
{
    await client.GetAsync("...");
}

// ✅ 正确做法
services.AddHttpClient();  // 使用IHttpClientFactory
```

3. **缓存未设置过期时间**：
```csharp
// ❌ 错误做法
_cache.Set("key", value);  // 永不过期

// ✅ 正确做法
_cache.Set("key", value, TimeSpan.FromMinutes(30));  // 30分钟过期
```

### 11.5 问题5：Swagger UI无法访问

**症状**：
```
访问 https://localhost:5001/swagger 返回404 Not Found
```

**原因分析**：
1. Swagger未在生产环境启用
2. ASPNETCORE_ENVIRONMENT配置错误
3. 中间件顺序错误

**解决方法**：

```csharp
// Program.cs（检查中间件配置）
var app = builder.Build();

// ✅ Swagger应在生产环境启用（根据配置）
if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("Lybt:Application:WebApi:Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LYBT WebAPI v1"));
}

// ⚠️ 中间件顺序：UseRouting → UseAuthentication → UseAuthorization → MapControllers
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**验证环境变量**：

```powershell
# 检查环境变量
[System.Environment]::GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Machine")

# 预期输出：Development 或 Production
```

---

## 12. 部署检查清单

### 12.1 部署前检查（Pre-Deployment）

**基础环境检查**：
- [ ] .NET 8 Runtime已安装（`dotnet --list-runtimes`）
- [ ] SQL Server服务运行正常（`Get-Service MSSQL*`）
- [ ] 网络端口开放（5000 HTTP, 5001 HTTPS）
- [ ] 磁盘空间充足（≥20GB可用）
- [ ] 备份现有数据库（如升级部署）

**配置文件检查**：
- [ ] appsettings.Production.json存在
- [ ] 所有敏感信息使用占位符（`#{VARIABLE}#`）
- [ ] 环境变量已设置且验证通过
- [ ] JWT密钥长度≥32字符
- [ ] 数据库连接字符串正确

**安全检查**：
- [ ] HTTPS证书有效且未过期
- [ ] 密码策略符合要求（≥12字符）
- [ ] 速率限制已启用
- [ ] 允许的主机名配置正确
- [ ] 日志不包含敏感信息（密码、Token）

### 12.2 部署中检查（During Deployment）

**发布验证**：
- [ ] 发布输出完整（`LYBT.WebAPI.exe`存在）
- [ ] 配置文件包含在输出（appsettings.*.json）
- [ ] DLL依赖完整（无缺失引用）
- [ ] 文件权限正确（IIS_IUSRS可读）

**数据库迁移**：
- [ ] 迁移脚本已生成并审查
- [ ] 数据库备份已创建
- [ ] 迁移成功应用（`dotnet ef database update`）
- [ ] 初始数据种子成功（管理员账号创建）

**服务安装**（Windows Service模式）：
- [ ] 服务已创建（`sc create`）
- [ ] 服务启动类型为自动（`start=auto`）
- [ ] 服务恢复策略已配置
- [ ] 服务依赖项已设置（SQL Server服务）

### 12.3 部署后检查（Post-Deployment）

**服务验证**：
- [ ] Windows Service运行正常（`Get-Service LYBTWebAPI`）
- [ ] 进程存在（`Get-Process LYBT.WebAPI`）
- [ ] 端口监听正常（`netstat -ano | findstr 5001`）
- [ ] 无错误日志（检查事件查看器）

**功能测试**：
- [ ] Swagger UI可访问（https://localhost:5001/swagger）
- [ ] 健康检查端点返回Healthy（/health）
- [ ] 登录功能正常（使用管理员账号）
- [ ] API端点响应正常（测试核心端点）
- [ ] 数据库读写正常

**监控验证**：
- [ ] Serilog日志正常写入（Console + File + Database）
- [ ] SystemLogs表有新日志记录
- [ ] 文件日志目录存在（logs/）
- [ ] 日志级别符合预期（Production为Warning）

**性能验证**：
- [ ] 首次请求响应时间 <3秒（冷启动）
- [ ] 后续请求响应时间 <500ms（热状态）
- [ ] 数据库连接池正常（Min连接预热）
- [ ] 内存占用合理（初始 <500MB）

### 12.4 回滚检查清单（Rollback）

**如果部署失败，执行回滚**：

```powershell
# Step 1: 停止新服务
Stop-Service -Name "LYBTWebAPI"

# Step 2: 还原备份文件
Copy-Item -Path "D:\backups\LYBT\WebAPI_Backup_*" -Destination "D:\deploy\LYBT\WebAPI" -Recurse -Force

# Step 3: 还原数据库（如已迁移）
Restore-SqlDatabase -ServerInstance "PROD-SQL" -Database "LYBTDB_Prod" -BackupFile "D:\backups\LYBTDB_Prod_PreDeploy.bak" -ReplaceDatabase

# Step 4: 启动旧服务
Start-Service -Name "LYBTWebAPI"

# Step 5: 验证回滚成功
Invoke-WebRequest -Uri "https://localhost:5001/health" -UseBasicParsing
```

---

## 13. 最佳实践

### 13.1 配置管理

1. **分离敏感信息**：
   - ✅ 生产环境所有密钥通过环境变量配置
   - ✅ 开发环境使用默认配置（appsettings.json）
   - ❌ 禁止在代码或配置文件硬编码密钥

2. **配置文件版本控制**：
   - ✅ appsettings.json提交到Git（模板）
   - ✅ appsettings.Production.json提交到Git（占位符）
   - ❌ appsettings.Development.json不提交（开发人员本地配置）

3. **环境变量命名规范**：
   - ✅ 使用双下划线`__`替代冒号（`Lybt__Authentication__Jwt__SecretKey`）
   - ✅ 使用Machine级别（所有用户可用）
   - ❌ 避免使用User级别（仅当前用户）

### 13.2 日志管理

1. **日志级别策略**：
   - Development: Information（详细日志，便于调试）
   - Production: Warning（仅警告和错误，减少日志量）
   - Critical System: Error（仅记录错误和致命问题）

2. **结构化日志**：
   - ✅ 使用Serilog的结构化属性（`{UserId}`, `{RequestId}`）
   - ✅ 包含上下文信息（IP地址、用户、时间戳）
   - ❌ 避免记录敏感信息（密码、Token、身份证号）

3. **日志清理**：
   - 文件日志：滚动删除（保留30-90天）
   - 数据库日志：定期清理（SQL Server Agent作业）
   - 归档策略：重要日志压缩归档到冷存储

### 13.3 安全实践

1. **最小权限原则**：
   - 数据库账号：仅授予必要权限（db_datareader + db_datawriter）
   - Windows Service：使用专用服务账号（非Administrator）
   - 文件系统：仅授予日志目录写权限

2. **密钥管理**：
   - JWT密钥：每6个月轮换一次
   - 数据库密码：每季度更新
   - HTTPS证书：到期前1个月更新

3. **审计日志**：
   - 记录所有登录尝试（成功和失败）
   - 记录敏感操作（密码修改、权限变更）
   - 定期审查异常日志（频繁失败、异常IP）

### 13.4 性能优化

1. **连接池调优**：
   - Min连接数 = 并发用户数 × 20%
   - Max连接数 = 并发用户数 × 3 × 1.5（缓冲系数）
   - 定期监控连接池使用率（避免耗尽）

2. **缓存策略**：
   - 读多写少数据：长缓存（1-2小时）
   - 中等更新频率：中缓存（10-30分钟）
   - 频繁更新数据：短缓存（1-5分钟）或不缓存

3. **异步编程**：
   - Controller方法：全部使用async/await
   - Service层：所有I/O操作异步
   - Repository层：所有数据库查询异步

### 13.5 监控与告警

1. **健康检查**：
   - 配置ASP.NET Core Health Checks
   - 监控数据库连接状态
   - 监控外部依赖（如第三方API）

2. **指标监控**：
   - CPU使用率（建议 <70%）
   - 内存使用率（建议 <80%）
   - 请求响应时间（建议P95 <1秒）
   - 错误率（建议 <1%）

3. **告警策略**：
   - 服务停止：立即告警（短信 + 邮件）
   - 错误率突增：5分钟内告警
   - 性能下降：持续10分钟告警
   - 磁盘空间不足：低于10%告警

---

## 14. 参考资料

### 14.1 官方文档

- [ASP.NET Core部署文档](https://learn.microsoft.com/aspnet/core/host-and-deploy/)
- [Kestrel Web服务器配置](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel)
- [Windows Service托管](https://learn.microsoft.com/aspnet/core/host-and-deploy/windows-service)
- [IIS托管ASP.NET Core](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/)
- [Serilog官方文档](https://serilog.net/)
- [Entity Framework Core迁移](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)

### 14.2 项目内部文档

- `docs/architecture/server/README.md` - Server端三层架构设计
- `docs/how-to-guides/server/webapi-development.md` - WebAPI开发指南
- `docs/how-to-guides/server/auth-integration.md` - JWT认证集成指南
- `src/Server/Services/LYBT.WebAPI/README.md` - WebAPI项目说明（待创建）

### 14.3 配置文件索引

- `src/Server/Services/LYBT.WebAPI/appsettings.json` - 开发环境基础配置
- `src/Server/Services/LYBT.WebAPI/appsettings.Development.json` - 开发环境配置
- `src/Server/Services/LYBT.WebAPI/appsettings.Production.json` - 生产环境配置模板
- `src/Server/Services/LYBT.WebAPI/appsettings.Security.json` - 安全配置模板
- `src/Server/Services/LYBT.WebAPI/Properties/launchSettings.json` - 启动配置

### 14.4 脚本工具

- `scripts/deployment/deploy-webapi.ps1` - WebAPI部署脚本（待创建）
- `scripts/deployment/validate-production-config.ps1` - 生产配置验证脚本（待创建）
- `scripts/deployment/backup-database.ps1` - 数据库备份脚本（待创建）
- `scripts/deployment/rollback-deployment.ps1` - 回滚脚本（待创建）

---

**最后更新**: 2025-10-30
**维护负责**: Server端开发组
**文档版本**: v1.0
