# 🚀 LYBTZYZS部署指南 (Deployment Guide)

## 📋 部署概述

LYBTZYZS凌隐宝堂中医诊所系统采用混合架构设计，包含48个项目组件，支持多种部署方式以适应不同规模的诊所需求。

**部署状态**: ✅ **生产就绪** | 🏗️ **混合架构** | 📊 **48个项目组件** | 🔧 **多部署选项**

---

## 🏗️ 系统架构总览

### 混合架构组件
```
后端 Web API (传统三层架构)
    ├── 8个业务模块API
    ├── 5个系统管理API  
    ├── 统一AppDbContext
    └── JWT认证体系

前端 WPF客户端 (UltraThink双层架构)
    ├── 8个业务模块
    ├── 统一Shell容器
    ├── Prism模块化
    └── Refit HTTP客户端

数据库层
    ├── SQL Server
    ├── EF Core迁移
    └── 智能缓存系统
```

### 48个项目组件统计

| 层级 | 项目数量 | 主要组件 |
|-----|---------|---------|
| **后端Web API** | 16个 | 业务模块、基础设施、实体、共享库 |
| **前端WPF客户端** | 24个 | 业务模块、Shell、服务、工作台 |
| **共享库** | 4个 | Models、Interfaces、Utilities、Common |
| **测试项目** | 4个 | 单元测试、集成测试、API测试 |

---

## 🎯 部署场景选择

### 场景一：小型单诊所部署 (推荐)
**适用规模**: 2-5名医生，<20用户
- **部署方式**: IIS + Windows Server
- **数据库**: SQL Server Express (免费)
- **特点**: 部署简单，维护成本低

### 场景二：中型连锁诊所部署
**适用规模**: 5-10名医生，多分点
- **部署方式**: IIS负载均衡 + SQL Server Standard
- **特点**: 支持异地组网，数据同步

### 场景三：容器化部署
**适用规模**: 技术团队充足的大型机构
- **部署方式**: Docker + Kubernetes
- **特点**: 弹性伸缩，自动化运维

---

## 💾 环境要求

### 服务器最低配置

#### 小型部署 (单诊所)
- **CPU**: 4核心 2.5GHz
- **内存**: 8GB RAM
- **存储**: 100GB SSD
- **操作系统**: Windows Server 2019+
- **网络**: 100Mbps带宽

#### 中型部署 (连锁诊所)
- **CPU**: 8核心 3.0GHz
- **内存**: 16GB RAM  
- **存储**: 500GB SSD + 1TB HDD
- **操作系统**: Windows Server 2022
- **网络**: 1Gbps带宽

#### 大型容器化部署
- **Kubernetes集群**: 3节点最小配置
- **每节点**: 8核16GB内存，200GB SSD
- **负载均衡器**: nginx ingress controller
- **存储**: 持久卷存储类

### 软件依赖版本

| 组件 | 推荐版本 | 最低版本 | 备注 |
|-----|---------|---------|------|
| **.NET Runtime** | 8.0.10 | 8.0.0 | 服务器必装 |
| **SQL Server** | 2022 Express | 2019 Express | 生产建议Standard |
| **IIS** | 10.0 | 8.5 | Windows Server内置 |
| **Redis** (可选) | 7.0 | 6.0 | 分布式缓存 |

---

## 🔧 部署前准备

### 1. 数据库准备

#### 创建数据库
```sql
-- 创建数据库和用户
CREATE DATABASE LYBTDB
COLLATE Chinese_PRC_CI_AS;

-- 创建应用用户 (生产环境)
CREATE LOGIN lybt_app WITH PASSWORD = 'LybtApp#2025$SecurePass!';
USE LYBTDB;
CREATE USER lybt_app FOR LOGIN lybt_app;
ALTER ROLE db_datareader ADD MEMBER lybt_app;
ALTER ROLE db_datawriter ADD MEMBER lybt_app;
ALTER ROLE db_ddladmin ADD MEMBER lybt_app;
```

#### 连接字符串配置
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=LYBTDB;User Id=lybt_app;Password=LybtApp#2025$SecurePass!;TrustServerCertificate=true;",
    "BackupConnection": "Server=backup_server;Database=LYBTDB_Backup;Integrated Security=true;"
  }
}
```

### 2. 环境变量配置

#### appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "LYBT": "Debug"
    }
  },
  "Jwt": {
    "Issuer": "LYBT-Production",
    "Audience": "LYBT-Clients",
    "SecretKey": "生产环境密钥-至少32位字符串",
    "ExpirationHours": 8,
    "RememberMeDays": 30
  },
  "Cache": {
    "DefaultExpirationMinutes": 10,
    "UserCacheMinutes": 30,
    "StatsCacheMinutes": 60
  },
  "HealthChecks": {
    "UI": {
      "Path": "/health-ui",
      "ApiPath": "/health-api"
    }
  }
}
```

---

## 🚀 IIS部署步骤 (推荐)

### 第一步：发布后端API

```batch
# 使用提供的发布脚本
cd D:\source\repos\LYBTZYZS
scripts\publish-production.bat

# 或手动发布
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj ^
  -c Release ^
  -o "C:\inetpub\lybt-api" ^
  --self-contained false ^
  --runtime win-x64
```

### 第二步：配置IIS站点

#### 创建应用程序池
```powershell
# PowerShell脚本 - 以管理员权限运行
Import-Module WebAdministration

# 创建应用程序池
New-WebAppPool -Name "LYBT-API-Pool"
Set-WebConfiguration -Filter '/system.webServer/applicationInitialization' -PSPath 'IIS:\Sites\Default Web Site' -Value @{doAppInitAfterRestart='true'}

# 配置应用程序池
Set-ItemProperty -Path "IIS:\AppPools\LYBT-API-Pool" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
Set-ItemProperty -Path "IIS:\AppPools\LYBT-API-Pool" -Name "enable32BitAppOnWin64" -Value $false
Set-ItemProperty -Path "IIS:\AppPools\LYBT-API-Pool" -Name "managedRuntimeVersion" -Value ""
```

#### 创建IIS站点
```powershell
# 创建站点
New-Website -Name "LYBT-API" -Port 5001 -PhysicalPath "C:\inetpub\lybt-api" -ApplicationPool "LYBT-API-Pool"

# 配置HTTPS (生产环境)
# 需要先安装SSL证书到服务器
New-WebBinding -Name "LYBT-API" -Protocol https -Port 7001 -SslFlags 1
```

### 第三步：数据库迁移

```batch
# 在发布服务器上执行迁移
cd C:\inetpub\lybt-api
dotnet LYBT.WebAPI.dll --migrate-database

# 或使用EF Core CLI工具 (如果已安装)
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI --connection "Server=.\\SQLEXPRESS;Database=LYBTDB;Integrated Security=true;TrustServerCertificate=true;"
```

### 第四步：部署WPF客户端

#### 方法一：ClickOnce发布 (推荐)
```batch
# 发布ClickOnce安装包
dotnet publish src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj ^
  -c Release ^
  -p:PublishProtocol=ClickOnce ^
  -p:PublishUrl="\\server\lybt-client\" ^
  -p:InstallUrl="\\server\lybt-client\" ^
  -p:ApplicationRevision=1
```

#### 方法二：MSI安装包
```batch
# 需要安装WiX Toolset v4
# 或使用Visual Studio Installer Projects扩展
# 详细步骤参见：docs/deployment/msi-packaging-guide.md
```

---

## 🐳 Docker部署

### 第一步：构建镜像

#### Dockerfile (后端API)
```dockerfile
# 使用项目根目录的 Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj", "src/Server/Services/LYBT.WebAPI/"]
COPY ["src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj", "src/Server/Core/LYBT.Infrastructure/"]
COPY ["src/Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj", "src/Shared/LYBT.Shared.Models/"]
RUN dotnet restore "src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj"
COPY . .
WORKDIR "/src/src/Server/Services/LYBT.WebAPI"
RUN dotnet build "LYBT.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LYBT.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LYBT.WebAPI.dll"]
```

#### 构建和运行
```batch
# 构建镜像
docker build -t lybt-api:latest -f docker/Dockerfile.api .

# 运行容器 (开发环境)
docker run -d ^
  --name lybt-api ^
  -p 5001:80 ^
  -e ASPNETCORE_ENVIRONMENT=Development ^
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal\\SQLEXPRESS;Database=LYBTDB;Integrated Security=true;TrustServerCertificate=true;" ^
  lybt-api:latest
```

### 第二步：Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  lybt-api:
    build:
      context: .
      dockerfile: docker/Dockerfile.api
    ports:
      - "5001:80"
      - "7001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=LYBTDB;User Id=sa;Password=LybtDb@2025!;TrustServerCertificate=true;
    depends_on:
      - sqlserver
    networks:
      - lybt-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=LybtDb@2025!
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - lybt-network

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    networks:
      - lybt-network

networks:
  lybt-network:
    driver: bridge

volumes:
  sqlserver-data:
```

---

## ☁️ Kubernetes部署

### 第一步：准备配置

#### namespace.yaml
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: lybt-system
  labels:
    name: lybt-system
```

#### configmap.yaml
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: lybt-config
  namespace: lybt-system
data:
  appsettings.json: |
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=sqlserver-service;Database=LYBTDB;User Id=sa;Password=$(DB_PASSWORD);TrustServerCertificate=true;"
      },
      "Jwt": {
        "SecretKey": "$(JWT_SECRET)",
        "ExpirationHours": 8
      }
    }
```

#### secret.yaml
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: lybt-secrets
  namespace: lybt-system
type: Opaque
data:
  db-password: TEJ5YnRuZGJtQDIwMjUhEWRmZ2dHaFR0CQ== # Base64编码
  jwt-secret: aW1sZWF0Y2FsZXl0Sj10RXR1dGlIYWJuZGF0b20= # Base64编码
```

### 第二步：部署应用

#### deployment.yaml
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: lybt-api
  namespace: lybt-system
spec:
  replicas: 2
  selector:
    matchLabels:
      app: lybt-api
  template:
    metadata:
      labels:
        app: lybt-api
    spec:
      containers:
      - name: lybt-api
        image: lybt-api:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: db-password
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: jwt-secret
        volumeMounts:
        - name: config-volume
          mountPath: /app/appsettings.json
          subPath: appsettings.json
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /api/v1/health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /api/v1/health
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: config-volume
        configMap:
          name: lybt-config
```

#### service.yaml
```yaml
apiVersion: v1
kind: Service
metadata:
  name: lybt-api-service
  namespace: lybt-system
spec:
  selector:
    app: lybt-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 80
  type: LoadBalancer
```

---

## 📊 监控与健康检查

### 健康检查端点

系统提供8个健康检查端点：

| 端点 | 功能 | URL |
|-----|------|-----|
| **总体健康** | 系统总体状态 | `/api/v1/health` |
| **数据库检查** | 数据库连接状态 | `/api/v1/health/database` |
| **缓存检查** | 内存缓存状态 | `/api/v1/health/cache` |
| **存储检查** | 磁盘空间状态 | `/api/v1/health/storage` |
| **内存检查** | 系统内存使用 | `/api/v1/health/memory` |
| **性能监控** | 响应时间统计 | `/api/v1/performance` |
| **安全审计** | 登录会话状态 | `/api/v1/security` |
| **系统监控** | 完整系统指标 | `/api/v1/monitoring` |

### 监控配置示例

```json
{
  "HealthChecks": {
    "UI": {
      "Path": "/health-ui",
      "ApiPath": "/health-api",
      "EvaluationTimeInSeconds": 10,
      "MinimumSecondsBetweenFailureNotifications": 60
    },
    "Database": {
      "ConnectionString": "DefaultConnection",
      "TimeoutSeconds": 30
    },
    "Cache": {
      "CheckKeys": ["system_status", "user_count"],
      "TimeoutSeconds": 10
    }
  }
}
```

---

## 🔒 安全配置

### JWT密钥管理

#### 生成安全密钥
```powershell
# PowerShell脚本生成256位密钥
Add-Type -AssemblyName System.Security
$rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::new()
$bytes = New-Object byte[] 32
$rng.GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)
Write-Host "JWT Secret Key: $key"
```

#### 生产环境密钥轮换
```json
{
  "Jwt": {
    "Primary": {
      "SecretKey": "主密钥-用于签发新token",
      "ExpirationHours": 8
    },
    "Secondary": {
      "SecretKey": "备用密钥-用于验证旧token",
      "ExpirationHours": 8
    },
    "RotationPolicy": {
      "RotateAfterDays": 30,
      "OverlapDays": 7
    }
  }
}
```

### 数据库安全

#### 连接加密
```xml
<!-- 在连接字符串中启用加密 -->
<connectionString>
  Server=server_name;Database=LYBTDB;
  User Id=lybt_app;Password=secure_password;
  Encrypt=true;TrustServerCertificate=false;
  Connection Timeout=30;
</connectionString>
```

#### 数据库权限最小化
```sql
-- 创建只读用户 (报表查询)
CREATE LOGIN lybt_readonly WITH PASSWORD = 'ReadOnly#2025$Pass!';
USE LYBTDB;
CREATE USER lybt_readonly FOR LOGIN lybt_readonly;
ALTER ROLE db_datareader ADD MEMBER lybt_readonly;

-- 创建备份用户
CREATE LOGIN lybt_backup WITH PASSWORD = 'Backup#2025$Pass!';
ALTER SERVER ROLE db_backupoperator ADD MEMBER lybt_backup;
```

---

## 🗄️ 备份与恢复

### 自动备份脚本

#### SQL Server备份脚本
```sql
-- 每日全备份脚本
DECLARE @BackupPath NVARCHAR(255) = 'D:\Backups\LYBTDB\'
DECLARE @FileName NVARCHAR(255) = 'LYBTDB_Full_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + '.bak'

-- 创建备份目录
EXEC xp_create_subdir @BackupPath

-- 执行备份
BACKUP DATABASE LYBTDB 
TO DISK = @BackupPath + @FileName
WITH FORMAT, INIT, COMPRESSION, CHECKSUM,
NAME = 'LYBTDB Full Backup',
DESCRIPTION = 'LYBTZYZS系统完整备份';

-- 清理7天前的备份文件
DECLARE @CleanupDate DATETIME = DATEADD(DAY, -7, GETDATE())
EXEC xp_delete_file 0, @BackupPath, 'bak', @CleanupDate, 1
```

#### PowerShell备份管理脚本
```powershell
# LYBT-Backup.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$BackupPath = "D:\Backups\LYBTDB",
    [Parameter(Mandatory=$false)]
    [int]$RetentionDays = 7
)

# 创建备份目录
if (!(Test-Path $BackupPath)) {
    New-Item -Path $BackupPath -ItemType Directory -Force
}

# 生成备份文件名
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupPath "LYBTDB_Full_$timestamp.bak"

# 执行数据库备份
$sqlcmd = @"
BACKUP DATABASE LYBTDB 
TO DISK = N'$backupFile'
WITH FORMAT, INIT, COMPRESSION, CHECKSUM,
NAME = N'LYBTDB Full Backup',
DESCRIPTION = N'LYBTZYZS系统完整备份 - $timestamp';
"@

try {
    Invoke-Sqlcmd -Query $sqlcmd -ServerInstance ".\SQLEXPRESS" -QueryTimeout 300
    Write-Host "✅ 备份成功: $backupFile" -ForegroundColor Green
    
    # 清理过期备份
    $cutoffDate = (Get-Date).AddDays(-$RetentionDays)
    Get-ChildItem -Path $BackupPath -Filter "*.bak" | 
        Where-Object { $_.CreationTime -lt $cutoffDate } | 
        Remove-Item -Force
    
    Write-Host "🧹 已清理$RetentionDays天前的备份文件" -ForegroundColor Yellow
} catch {
    Write-Error "❌ 备份失败: $_"
    exit 1
}
```

### 恢复流程

#### 完整恢复步骤
```sql
-- 1. 设置数据库为单用户模式
ALTER DATABASE LYBTDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- 2. 执行恢复
RESTORE DATABASE LYBTDB 
FROM DISK = 'D:\Backups\LYBTDB\LYBTDB_Full_20250902_143000.bak'
WITH REPLACE, CHECKDB;

-- 3. 设置数据库为多用户模式
ALTER DATABASE LYBTDB SET MULTI_USER;

-- 4. 验证数据完整性
DBCC CHECKDB('LYBTDB') WITH NO_INFOMSGS;
```

---

## 🚀 性能优化

### IIS性能配置

#### applicationHost.config优化
```xml
<system.webServer>
    <!-- 启用压缩 -->
    <httpCompression directory="%SystemDrive%\inetpub\temp\IIS Temporary Compressed Files">
        <scheme name="gzip" dll="%Windir%\system32\inetsrv\gzip.dll" />
        <dynamicTypes>
            <add mimeType="text/*" enabled="true" />
            <add mimeType="message/*" enabled="true" />
            <add mimeType="application/json" enabled="true" />
        </dynamicTypes>
        <staticTypes>
            <add mimeType="text/*" enabled="true" />
            <add mimeType="message/*" enabled="true" />
            <add mimeType="application/json" enabled="true" />
        </staticTypes>
    </httpCompression>
    
    <!-- 缓存配置 -->
    <caching>
        <profiles>
            <add extension=".js" policy="CacheUntilChange" kernelCachePolicy="CacheUntilChange" duration="7.00:00:00" />
            <add extension=".css" policy="CacheUntilChange" kernelCachePolicy="CacheUntilChange" duration="7.00:00:00" />
            <add extension=".png" policy="CacheUntilChange" kernelCachePolicy="CacheUntilChange" duration="30.00:00:00" />
        </profiles>
    </caching>
</system.webServer>
```

### 数据库性能优化

#### 索引优化脚本
```sql
-- 用户表索引优化
CREATE NONCLUSTERED INDEX IX_Users_Username 
ON Users (Username) 
INCLUDE (Role, IsActive, CreateTime);

CREATE NONCLUSTERED INDEX IX_Users_Role_Active 
ON Users (Role, IsActive) 
INCLUDE (Id, Username, Email);

-- 患者表索引优化  
CREATE NONCLUSTERED INDEX IX_Patients_Phone 
ON Patients (Phone) 
INCLUDE (Name, Gender, DateOfBirth);

CREATE NONCLUSTERED INDEX IX_Patients_Name 
ON Patients (Name) 
INCLUDE (Id, Phone, Gender);

-- 医案表索引优化
CREATE NONCLUSTERED INDEX IX_MedicalCases_Patient_Status 
ON MedicalCases (PatientId, Status) 
INCLUDE (Id, ChiefComplaint, CreateTime);

-- 诊断记录索引优化
CREATE NONCLUSTERED INDEX IX_Consultations_MedicalCase 
ON Consultations (MedicalCaseId) 
INCLUDE (Id, Symptoms, Diagnosis, CreateTime);
```

### 应用程序性能配置

#### Program.cs优化配置
```csharp
var builder = WebApplication.CreateBuilder(args);

// 连接池优化
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(3);
    }));

// 内存缓存优化
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100000; // 限制条目数量
    options.CompactionPercentage = 0.25; // 压缩阈值
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // 清理频率
});

// HTTP客户端优化
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "LYBT-Client/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    MaxConnectionsPerServer = 10,
    UseProxy = false
});

// 响应缓存
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024; // 1MB
    options.UseCaseSensitivePaths = false;
});
```

---

## 🔍 故障排除

### 常见部署问题

#### 问题1：数据库连接失败
**症状**: `Microsoft.Data.SqlClient.SqlException: A network-related or instance-specific error occurred`

**解决方案**:
```batch
# 1. 检查SQL Server服务状态
services.msc → SQL Server (SQLEXPRESS) 确保正在运行

# 2. 启用TCP/IP协议
SQL Server Configuration Manager → SQL Server网络配置 → 协议 → TCP/IP启用

# 3. 检查防火墙
netsh advfirewall firewall add rule name="SQL Server" dir=in action=allow protocol=TCP localport=1433

# 4. 测试连接
sqlcmd -S .\SQLEXPRESS -E -Q "SELECT @@VERSION"
```

#### 问题2：JWT认证失败  
**症状**: `401 Unauthorized` 或 `Invalid token signature`

**解决方案**:
```json
// 检查appsettings.json配置
{
  "Jwt": {
    "SecretKey": "确保至少32位字符的强密钥",
    "Issuer": "与客户端配置一致",
    "Audience": "与客户端配置一致",
    "ExpirationHours": 8
  }
}
```

#### 问题3：WPF客户端连接API失败
**症状**: `HttpRequestException: No connection could be made`

**解决方案**:
```csharp
// 检查client配置 (App.xaml.cs或配置文件)
public static class ApiConfiguration
{
    public static string BaseUrl = "https://localhost:7001/"; // 确保端口正确
    public static string ApiVersion = "v1";
    public static int TimeoutSeconds = 30;
}

// 检查网络连通性
ping localhost
telnet localhost 7001
```

#### 问题4：EF Core迁移失败
**症状**: `Unable to create an object of type 'AppDbContext'`

**解决方案**:
```batch
# 1. 确保在正确项目执行迁移
cd src/Server/Core/LYBT.Infrastructure

# 2. 添加迁移时指定正确的启动项目
dotnet ef migrations add InitialCreate ^
  --project LYBT.Infrastructure.csproj ^
  --startup-project ../../Services/LYBT.WebAPI/LYBT.WebAPI.csproj ^
  --context AppDbContext

# 3. 更新数据库
dotnet ef database update ^
  --project LYBT.Infrastructure.csproj ^
  --startup-project ../../Services/LYBT.WebAPI/LYBT.WebAPI.csproj ^
  --context AppDbContext
```

### 性能问题诊断

#### 诊断工具
```powershell
# 1. 检查应用程序池性能
Get-Counter "\Process(w3wp*)\% Processor Time"
Get-Counter "\Process(w3wp*)\Working Set"

# 2. 数据库连接池监控
SELECT 
    DB_NAME() as DatabaseName,
    COUNT(*) as ConnectionCount,
    login_name,
    host_name,
    program_name
FROM sys.dm_exec_sessions 
WHERE is_user_process = 1
GROUP BY login_name, host_name, program_name;

# 3. 内存缓存命中率检查
# 通过 /api/v1/monitoring 端点查看缓存统计
```

### 日志分析

#### Serilog配置示例
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "LYBT": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/lybt-api-.log",
          "rollingInterval": "Day",
          "rollOnFileSizeLimit": true,
          "fileSizeLimitBytes": 10485760,
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

---

## 📚 相关文档

### 部署相关
- [IIS详细配置指南](docs/deployment/iis-detailed-setup.md)
- [Docker部署完整方案](docs/deployment/docker-deployment-complete.md)
- [Kubernetes生产环境配置](docs/deployment/k8s-production-config.md)
- [SSL证书配置指南](docs/deployment/ssl-certificate-setup.md)

### 运维管理
- [监控告警配置](docs/operations/monitoring-alerting-setup.md)
- [备份恢复策略](docs/operations/backup-recovery-strategy.md)
- [安全加固检查清单](docs/operations/security-hardening-checklist.md)
- [性能调优指南](docs/operations/performance-tuning-guide.md)

### 故障排除
- [常见问题FAQ](docs/troubleshooting/common-issues-faq.md)
- [日志分析指南](docs/troubleshooting/log-analysis-guide.md)
- [数据库维护手册](docs/troubleshooting/database-maintenance-manual.md)

---

## 📞 技术支持

### 支持联系方式
- **技术文档**: [项目Wiki](https://github.com/your-org/LYBTZYZS/wiki)
- **问题报告**: [GitHub Issues](https://github.com/your-org/LYBTZYZS/issues)
- **部署支持**: 技术团队邮箱 support@yourcompany.com

### 版本支持策略
| 版本类型 | 支持周期 | 更新频率 |
|---------|---------|---------|
| **主版本** | 3年 | 每年1-2次 |
| **次版本** | 1年 | 每季度1次 |
| **补丁版本** | 6个月 | 每月1-2次 |
| **安全更新** | 立即 | 按需发布 |

---

**部署指南** - LYBTZYZS系统快速上线指南 🚀