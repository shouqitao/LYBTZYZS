# DEEP-004: 部署指南

## 概述

凌隐宝堂中医诊所管理系统部署指南涵盖从开发环境到生产环境的完整部署流程，包括环境准备、数据库配置、应用部署、安全设置和监控配置。本指南基于系统的三层架构设计，提供服务器端Web API部署、客户端WPF应用程序分发以及相关基础设施的详细配置方案。

## 部署架构概览

### 1. 系统架构图

```
                    ┌─────────────────────────────────────┐
                    │            用户端 (WPF)               │
                    │         凌隐宝堂诊所管理系统            │
                    └─────────────────┬───────────────────┘
                                      │ HTTPS/WSS
                                      │
                    ┌─────────────────┴───────────────────┐
                    │         负载均衡器 (可选)              │
                    │         Nginx / IIS ARR              │
                    └─────────────────┬───────────────────┘
                                      │
                    ┌─────────────────┴───────────────────┐
                    │       Web API 服务器 (.NET 8)         │
                    │         ASP.NET Core 应用             │
                    └─────────────────┬───────────────────┘
                                      │
                    ┌─────────────────┴───────────────────┐
                    │       SQL Server 数据库              │
                    │     患者数据、医案、处方等            │
                    └─────────────────────────────────────┘

    辅助服务：
    ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
    │   文件存储服务    │  │   日志收集服务    │  │   监控告警服务    │
    │   (本地/网络)    │  │  (Serilog/ELK)  │  │  (Prometheus)   │
    └─────────────────┘  └─────────────────┘  └─────────────────┘
```

### 2. 部署环境分类

| 环境类型 | 用途 | 服务器配置 | 数据库 | 部署频率 |
|---------|------|-----------|--------|---------|
| **开发环境** | 日常开发调试 | 本地开发机 | LocalDB/SQLEXP | 每日 |
| **测试环境** | 功能测试验证 | 2核4GB | SQL Server 2019 | 每周 |
| **预生产环境** | 生产前验证 | 4核8GB | SQL Server 2019 | 发布前 |
| **生产环境** | 正式运营 | 8核16GB | SQL Server 2019 | 按需 |

## 环境准备

### 1. 服务器硬件要求

#### Web API服务器
- **最低配置**：2核CPU，4GB内存，100GB SSD
- **推荐配置**：4核CPU，8GB内存，200GB SSD
- **生产配置**：8核CPU，16GB内存，500GB SSD

#### 数据库服务器
- **最低配置**：2核CPU，4GB内存，200GB SSD
- **推荐配置**：4核CPU，8GB内存，500GB SSD
- **生产配置**：8核CPU，16GB内存，1TB SSD

#### 客户端工作站
- **最低配置**：Intel i3，4GB内存，100GB硬盘
- **推荐配置**：Intel i5，8GB内存，256GB SSD
- **操作系统**：Windows 10/11 Pro

### 2. 软件依赖安装

#### 服务器端软件
```powershell
# 安装 .NET 8.0 Runtime
Invoke-WebRequest -Uri "https://download.microsoft.com/download/6/0/8/6088C0B2-9BD2-44A9-A8E5-82F99E5D16F0/dotnet-runtime-8.0.0-win-x64.exe" -OutFile "dotnet-runtime-8.0.0-win-x64.exe"
Start-Process -FilePath "dotnet-runtime-8.0.0-win-x64.exe" -ArgumentList "/quiet" -Wait

# 安装 ASP.NET Core Hosting Bundle
Invoke-WebRequest -Uri "https://download.microsoft.com/download/6/0/8/6088C0B2-9BD2-44A9-A8E5-82F99E5D16F0/dotnet-hosting-8.0.0-win.exe" -OutFile "dotnet-hosting-8.0.0-win.exe"
Start-Process -FilePath "dotnet-hosting-8.0.0-win.exe" -ArgumentList "/quiet" -Wait

# 安装 SQL Server 2019
# 从微软官网下载 SQL Server 2019 Express 或 Standard 版本

# 安装 IIS (如使用IIS部署)
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpRedirect
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45
```

#### 数据库配置
```sql
-- 创建数据库
CREATE DATABASE LYBT_Clinic
ON PRIMARY
(
    NAME = 'LYBT_Clinic_Data',
    FILENAME = 'C:\Database\LYBT_Clinic.mdf',
    SIZE = 100MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 10MB
)
LOG ON
(
    NAME = 'LYBT_Clinic_Log',
    FILENAME = 'C:\Database\LYBT_Clinic.ldf',
    SIZE = 10MB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 10%
);

-- 创建数据库用户
USE LYBT_Clinic;
CREATE USER lybt_user WITH PASSWORD = 'StrongPassword123!';
ALTER ROLE db_owner ADD MEMBER lybt_user;

-- 配置数据库权限
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO lybt_user;
GRANT EXECUTE ON SCHEMA::dbo TO lybt_user;
```

## 应用部署配置

### 1. Web API部署

#### 1.1 发布应用
```bash
# 发布生产版本
dotnet publish src/Server/LYBT.API/LYBT.API.csproj ^
    --configuration Release ^
    --output C:\Deploy\LYBT.API ^
    --self-contained false ^
    --runtime win-x64 ^
    --no-build

# 发布自包含版本（可选）
dotnet publish src/Server/LYBT.API/LYBT.API.csproj ^
    --configuration Release ^
    --output C:\Deploy\LYBT.API.SelfContained ^
    --self-contained true ^
    --runtime win-x64 ^
    --no-build
```

#### 1.2 配置文件设置
```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=LYBT_Clinic;User Id=lybt_user;Password=StrongPassword123!;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=300;"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-jwt-key-for-production-minimum-32-characters",
    "Issuer": "LYBT.Clinic.API",
    "Audience": "LYBT.Clinic.Client",
    "AccessTokenExpiration": "02:00:00",
    "RefreshTokenExpiration": "7.00:00:00"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "LYBT.Infrastructure": "Information"
    },
    "File": {
      "Path": "logs/lybt-api-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    },
    "EventLog": {
      "SourceName": "LYBT Clinic API",
      "LogName": "Application",
      "RestrictedToMinimumLevel": "Error"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001"
      }
    },
    "Limits": {
      "MaxRequestBodySize": 104857600,
      "MaxRequestBufferSize": 1048576
    }
  },
  "FileStorage": {
    "UploadPath": "C:\FileStorage\Uploads",
    "MaxFileSize": 10485760,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx"]
  },
  "BackupSettings": {
    "Enabled": true,
    "Schedule": "0 2 * * *",  // 每天凌晨2点
    "BackupPath": "C:\Backup\Database",
    "RetentionDays": 30
  }
}
```

#### 1.3 IIS部署配置
```xml
<!-- web.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\LYBT.API.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <httpProtocol>
        <customHeaders>
          <add name="X-Content-Type-Options" value="nosniff" />
          <add name="X-Frame-Options" value="DENY" />
          <add name="X-XSS-Protection" value="1; mode=block" />
          <add name="Strict-Transport-Security" value="max-age=31536000; includeSubDomains" />
        </customHeaders>
      </httpProtocol>
      <security>
        <requestFiltering>
          <requestLimits maxAllowedContentLength="104857600" />
        </requestFiltering>
      </security>
      <staticContent>
        <mimeMap fileExtension=".json" mimeType="application/json" />
        <mimeMap fileExtension=".woff" mimeType="application/font-woff" />
        <mimeMap fileExtension=".woff2" mimeType="application/font-woff2" />
      </staticContent>
    </system.webServer>
  </location>
</configuration>
```

#### 1.4 服务部署（Windows Service）
```csharp
// Program.cs - Windows Service配置
using LYBT.API;

var builder = WebApplication.CreateBuilder(args);

// 检查是否作为Windows Service运行
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService();
}

var app = builder.Build();

app.MapControllers();

app.Run();
```

```bash
# 安装为Windows Service
sc create "LYBT Clinic API" binPath="C:\Deploy\LYBT.API\LYBT.API.exe" start=auto
sc description "LYBT Clinic API" "凌隐宝堂中医诊所管理系统Web API服务"
sc start "LYBT Clinic API"
```

### 2. 客户端部署

#### 2.1 WPF应用程序发布
```bash
# 发布自包含版本
dotnet publish src/Client/LYBT.Desktop/LYBT.Desktop.csproj ^
    --configuration Release ^
    --output C:\Deploy\LYBT.Desktop ^
    --self-contained true ^
    --runtime win-x64 ^
    --p:PublishSingleFile=true ^
    --p:IncludeNativeLibrariesForSelfExtract=true ^
    --p:PublishReadyToRun=true

# 创建安装程序
# 使用Inno Setup或WiX Toolset创建MSI安装包
```

#### 2.2 Inno Setup安装脚本
```inno
; LYBT-Desktop.iss
[Setup]
AppName=凌隐宝堂中医诊所管理系统
AppVersion=1.0.0
DefaultDirName={pf}\LYBT Clinic
DefaultGroupName=凌隐宝堂诊所管理系统
OutputDir=C:\Deploy\Installer
OutputBaseFilename=LYBT-Clinic-Setup
SetupIconFile=icon.ico
Compression=lzma
SolidCompression=yes

[Files]
Source: "C:\Deploy\LYBT.Desktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\凌隐宝堂诊所管理系统"; Filename: "{app}\LYBT.Desktop.exe"
Name: "{commondesktop}\凌隐宝堂诊所管理系统"; Filename: "{app}\LYBT.Desktop.exe"

[Registry]
Root: HKCU; Subkey: "Software\LYBT\Clinic"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKCU; Subkey: "Software\LYBT\Clinic"; ValueType: string; ValueName: "Version"; ValueData: "1.0.0"

[Run]
Filename: "{app}\LYBT.Desktop.exe"; Description: "启动应用程序"; Flags: nowait postinstall skipifsilent
```

#### 2.3 客户端配置文件
```json
// appsettings.Production.json
// Issue #1726: 使用统一配置命名规范
{
  "Lybt": {
    "Client": {
      "Api": {
        "BaseUrl": "https://api.yourclinic.com",
        "TimeoutSeconds": 60,
        "RetryCount": 3,
        "RetryDelaySeconds": 1,
        "IgnoreSslErrors": false
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "LYBT.Desktop": "Information"
    },
    "File": {
      "Path": "logs/lybt-desktop-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 7
    }
  },
  "CacheSettings": {
    "DefaultExpirationMinutes": 15,
    "MaxCacheSize": 100
  },
  "Security": {
    "ValidateServerCertificate": true,
    "TokenRefreshBuffer": "00:05:00"
  },
  "OfflineMode": {
    "Enabled": true,
    "SyncInterval": "00:05:00",
    "MaxOfflineDataAge": "7.00:00:00"
  }
}
```

## 数据库部署与迁移

### 1. 数据库初始化脚本

```sql
-- 01_CreateDatabase.sql
USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'LYBT_Clinic')
BEGIN
    ALTER DATABASE LYBT_Clinic SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LYBT_Clinic;
END
GO

CREATE DATABASE LYBT_Clinic
COLLATE Chinese_PRC_CI_AS;
GO

ALTER DATABASE LYBT_Clinic SET RECOVERY SIMPLE;
GO
```

```sql
-- 02_CreateTables.sql
USE LYBT_Clinic;
GO

-- 患者表
CREATE TABLE Patients (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Gender NVARCHAR(10) NOT NULL,
    DateOfBirth DATE NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    IdentificationNumber NVARCHAR(18) NULL,
    Address NVARCHAR(500) NULL,
    EmergencyContact NVARCHAR(100) NULL,
    EmergencyPhone NVARCHAR(20) NULL,
    Allergies NVARCHAR(1000) NULL,
    MedicalHistory NVARCHAR(2000) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    UpdatedDate DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

CREATE UNIQUE INDEX IX_Patients_IdentificationNumber ON Patients(IdentificationNumber) WHERE IdentificationNumber IS NOT NULL;
CREATE INDEX IX_Patients_Name_DateOfBirth ON Patients(Name, DateOfBirth);
CREATE INDEX IX_Patients_Phone ON Patients(PhoneNumber);
GO

-- 医生表
CREATE TABLE Doctors (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Specialty NVARCHAR(100) NOT NULL,
    Title NVARCHAR(50) NULL,
    LicenseNumber NVARCHAR(50) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    UpdatedDate DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

-- 医案表
CREATE TABLE MedicalCases (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    PatientID INT NOT NULL FOREIGN KEY REFERENCES Patients(ID),
    DoctorID INT NOT NULL FOREIGN KEY REFERENCES Doctors(ID),
    VisitDate DATE NOT NULL,
    ChiefComplaint NVARCHAR(1000) NOT NULL,
    CurrentIllnessHistory NVARCHAR(2000) NULL,
    PastHistory NVARCHAR(2000) NULL,
    PersonalHistory NVARCHAR(1000) NULL,
    FamilyHistory NVARCHAR(1000) NULL,
    PhysicalExamination NVARCHAR(2000) NULL,
    Diagnosis NVARCHAR(1000) NOT NULL,
    TreatmentPrinciple NVARCHAR(1000) NOT NULL,
    Pulse NVARCHAR(100) NULL,
    Tongue NVARCHAR(200) NULL,
    Notes NVARCHAR(2000) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    UpdatedDate DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

CREATE INDEX IX_MedicalCases_PatientId_Date ON MedicalCases(PatientID, VisitDate DESC);
CREATE INDEX IX_MedicalCases_DoctorId_Date ON MedicalCases(DoctorID, VisitDate DESC);
GO

-- 药材表
CREATE TABLE Herbs (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    HerbCode NVARCHAR(50) NOT NULL,
    Category NVARCHAR(100) NULL,
    LatinName NVARCHAR(200) NULL,
    Description NVARCHAR(1000) NULL,
    Unit NVARCHAR(20) NOT NULL DEFAULT 'g',
    UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0,
    Supplier NVARCHAR(200) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    UpdatedDate DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

CREATE UNIQUE INDEX IX_Herbs_Name ON Herbs(Name) WHERE IsActive = 1;
CREATE UNIQUE INDEX IX_Herbs_Code ON Herbs(HerbCode) WHERE IsActive = 1;
GO

-- 处方表
CREATE TABLE Prescriptions (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    MedicalCaseID INT NOT NULL FOREIGN KEY REFERENCES MedicalCases(ID),
    DoctorID INT NOT NULL FOREIGN KEY REFERENCES Doctors(ID),
    PrescriptionDate DATE NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    FinalAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Unpaid',
    Notes NVARCHAR(1000) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    UpdatedDate DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

-- 处方明细表
CREATE TABLE PrescriptionItems (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    PrescriptionID INT NOT NULL FOREIGN KEY REFERENCES Prescriptions(ID),
    HerbID INT NOT NULL FOREIGN KEY REFERENCES Herbs(ID),
    Quantity DECIMAL(10,2) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,
    Instructions NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    UpdatedDate DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

CREATE INDEX IX_PrescriptionItems_PrescriptionId ON PrescriptionItems(PrescriptionID);
CREATE INDEX IX_PrescriptionItems_HerbId ON PrescriptionItems(HerbID);
GO

-- 系统用户表（用于API认证）
CREATE TABLE Users (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Email NVARCHAR(100) NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'User',
    IsActive BIT NOT NULL DEFAULT 1,
    LastLoginDate DATETIME2 NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME2 NULL
);
GO

-- Issue #1909: AdminSecrets表已移除，所有用户（包括SuperAdmin）统一存储在Users表
```

### 2. 数据库迁移脚本

```sql
-- 03_InsertInitialData.sql
USE LYBT_Clinic;
GO

-- 插入初始医生数据
INSERT INTO Doctors (Name, Specialty, Title, LicenseNumber, PhoneNumber, Email)
VALUES
    ('张医师', '中医内科', '主治医师', 'ZY123456', '13800138001', 'zhang@clinic.com'),
    ('李医师', '中医外科', '副主任医师', 'ZY234567', '13800138002', 'li@clinic.com'),
    ('王医师', '中医妇科', '主任医师', 'ZY345678', '13800138003', 'wang@clinic.com');
GO

-- 插入初始药材数据
INSERT INTO Herbs (Name, HerbCode, Category, LatinName, Unit, UnitPrice, Supplier)
VALUES
    ('人参', 'RS001', '补气药', 'Panax ginseng', 'g', 8.50, '北京药材批发市场'),
    ('白术', 'BS001', '补气药', 'Atractylodes macrocephala', 'g', 3.20, '北京药材批发市场'),
    ('茯苓', 'FL001', '利水渗湿药', 'Poria cocos', 'g', 2.80, '北京药材批发市场'),
    ('当归', 'DG001', '补血药', 'Angelica sinensis', 'g', 6.50, '四川药材公司'),
    ('黄芪', 'HQ001', '补气药', 'Astragalus membranaceus', 'g', 4.20, '甘肃药材基地');
GO

-- Issue #1909: 插入三角色体系用户（SuperAdmin自动初始化，这里仅作示例）
-- 注意: SuperAdmin会在应用首次启动时自动创建，无需手动插入
-- 此处仅插入示例Admin和Doctor用户
INSERT INTO Users (UserName, PasswordHash, RealName, Email, Role, IsActive, CreatedAt, UpdatedAt)
VALUES
    -- SuperAdmin (Role=100) 由应用自动创建
    -- Admin示例 (Role=10)
    ('admin1', '$2a$11$YourHashedPasswordHere', '管理员1', 'admin1@clinic.com', 10, 1, GETDATE(), GETDATE()),
    -- Doctor示例 (Role=1)
    ('doctor1', '$2a$11$YourHashedPasswordHere', '医生1', 'doctor1@clinic.com', 1, 1, GETDATE(), GETDATE());
GO
```

### 3. 数据库备份脚本

```sql
-- Backup_Database.sql
DECLARE @BackupPath NVARCHAR(500);
DECLARE @FileName NVARCHAR(500);
DECLARE @DateTime NVARCHAR(20);

SET @DateTime = REPLACE(CONVERT(NVARCHAR, GETDATE(), 120), ':', '-');
SET @FileName = 'LYBT_Clinic_Backup_' + @DateTime + '.bak';
SET @BackupPath = 'C:\Backup\Database\' + @FileName;

-- 执行备份
BACKUP DATABASE LYBT_Clinic
TO DISK = @BackupPath
WITH FORMAT,
     COMPRESSION,
     CHECKSUM,
     STATS = 10;

PRINT '数据库备份完成: ' + @BackupPath;

-- 清理7天前的备份文件
DECLARE @CleanupDate DATETIME = DATEADD(DAY, -7, GETDATE());

EXEC master.dbo.xp_delete_file
    0,
    N'C:\Backup\Database\',
    N'bak',
    @CleanupDate,
    1;
```

## 安全配置

### 1. HTTPS证书配置

```powershell
# 使用Let's Encrypt获取免费SSL证书
# 安装CertifyTheWeb
# 或使用PowerShell脚本自动申请

# 创建自签名证书（仅用于开发环境）
New-SelfSignedCertificate -DnsName "localhost","api.yourclinic.com" -CertStoreLocation "cert:\LocalMachine\My" -NotAfter (Get-Date).AddYears(5)

# 导出证书
$cert = Get-ChildItem -Path Cert:\LocalMachine\My\ | Where-Object { $_.Subject -like "*api.yourclinic.com*" }
Export-PfxCertificate -Cert $cert -FilePath "C:\Certs\api.yourclinic.com.pfx" -Password (ConvertTo-SecureString -String "YourCertificatePassword" -Force -AsPlainText)

# 导入到IIS
Import-Module WebAdministration
Import-PfxCertificate -FilePath "C:\Certs\api.yourclinic.com.pfx" -CertStoreLocation "cert:\LocalMachine\My" -Password (ConvertTo-SecureString -String "YourCertificatePassword" -Force -AsPlainText)

# 绑定HTTPS
New-WebBinding -Name "LYBT Clinic API" -Protocol https -Port 443
Get-ChildItem -Path Cert:\LocalMachine\My\ | Where-Object { $_.Subject -like "*api.yourclinic.com*" } | New-Item -Path "IIS:\SSLBindings\0.0.0.0!443"
```

### 2. 防火墙配置

```powershell
# 配置Windows防火墙规则
New-NetFirewallRule -DisplayName "LYBT Clinic API HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "LYBT Clinic API HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow

# 限制远程访问
Set-NetFirewallRule -DisplayName "SQL Server" -RemoteAddress Any
```

### 3. 数据库安全配置

```sql
-- SQL Server安全配置
-- 启用密码策略
ALTER LOGIN [sa] WITH CHECK_POLICY = ON;
ALTER LOGIN [sa] WITH CHECK_EXPIRATION = ON;

-- 禁用不必要的账户
ALTER LOGIN [sa] DISABLE;

-- 创建专用的应用账户
CREATE LOGIN [lybt_app] WITH PASSWORD = 'StrongAppPassword123!', CHECK_POLICY = ON;
CREATE USER [lybt_app] FOR LOGIN [lybt_app];

-- 授予最小权限
USE LYBT_Clinic;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO lybt_app;
GRANT EXECUTE ON SCHEMA::dbo TO lybt_app;

-- 启用数据库审计
ALTER DATABASE LYBT_Clinic SET AUDIT_LOG ON;
```

## 监控和日志配置

### 1. 应用程序监控

```csharp
// appsettings.Production.json - 监控配置
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "LYBT": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/lybt-api-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "EventLog",
        "Args": {
          "source": "LYBT Clinic API",
          "logName": "Application",
          "restrictedToMinimumLevel": "Error"
        }
      }
    ]
  }
}
```

### 2. 性能监控脚本

```powershell
# Monitor-API.ps1 - API监控脚本
$apiUrl = "https://api.yourclinic.com/health"
$logFile = "C:\Monitoring\api-health.log"

while ($true) {
    try {
        $response = Invoke-RestMethod -Uri $apiUrl -Method Get -TimeoutSec 10

        $logEntry = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - API Status: Healthy - Response: $($response | ConvertTo-Json)"
        Add-Content -Path $logFile -Value $logEntry

        # 检查响应时间
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        Invoke-RestMethod -Uri $apiUrl -Method Get -TimeoutSec 10 | Out-Null
        $stopwatch.Stop()

        if ($stopwatch.ElapsedMilliseconds -gt 5000) {
            Add-Content -Path $logFile -Value "$(Get-Date): WARNING - Slow response time: $($stopwatch.ElapsedMilliseconds)ms"
        }
    }
    catch {
        $logEntry = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - API Status: Error - Error: $($_.Exception.Message)"
        Add-Content -Path $logFile -Value $logEntry

        # 发送告警邮件
        Send-MailMessage -From "monitor@yourclinic.com" -To "admin@yourclinic.com" -Subject "API Health Alert" -Body $logEntry -SmtpServer "smtp.yourclinic.com"
    }

    Start-Sleep -Seconds 60
}
```

### 3. 数据库监控

```sql
-- Database_Monitoring.sql - 数据库监控查询
-- 检查数据库大小
SELECT
    name AS DatabaseName,
    size/128.0 AS DatabaseSizeMB,
    size/128.0/1024.0 AS DatabaseSizeGB
FROM sys.master_files
WHERE name = 'LYBT_Clinic';

-- 检查连接数
SELECT
    DB_NAME(dbid) as DBName,
    COUNT(dbid) as NumberOfConnections,
    loginame as LoginName
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid, loginame
ORDER BY NumberOfConnections DESC;

-- 检查慢查询
SELECT TOP 10
    qs.total_elapsed_time/qs.execution_count/1000.0 AS avg_execution_time_ms,
    qs.total_logical_reads/qs.execution_count AS avg_logical_reads,
    qt.text AS query_text,
    qp.query_plan
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
ORDER BY qs.total_elapsed_time/qs.execution_count DESC;
```

## 自动化部署脚本

### 1. 完整部署脚本

```powershell
# Deploy-LYBT.ps1 - 完整部署脚本
param(
    [Parameter(Mandatory=$true)]
    [string]$Environment,  # Development, Testing, Production

    [Parameter(Mandatory=$true)]
    [string]$DatabaseServer,

    [Parameter(Mandatory=$true)]
    [string]$ApiPath,

    [string]$BackupPath = "C:\Backup"
)

Write-Host "开始部署凌隐宝堂中医诊所管理系统..." -ForegroundColor Green

# 1. 备份当前版本
if (Test-Path $ApiPath) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupFolder = Join-Path $BackupPath "Backup-$timestamp"

    Write-Host "备份当前版本到: $backupFolder"
    Copy-Item -Path $ApiPath -Destination $backupFolder -Recurse
}

# 2. 停止现有服务
Write-Host "停止现有服务..."
Stop-Service -Name "LYBT Clinic API" -ErrorAction SilentlyContinue
Stop-WebAppPool -Name "LYBTClinicAPIPool" -ErrorAction SilentlyContinue

# 3. 数据库部署
Write-Host "部署数据库..."
$connectionString = "Server=$DatabaseServer;Database=master;Integrated Security=True;"

# 执行数据库脚本
$scripts = @(
    "01_CreateDatabase.sql",
    "02_CreateTables.sql",
    "03_InsertInitialData.sql"
)

foreach ($script in $scripts) {
    $scriptPath = Join-Path $PSScriptRoot "Database\Scripts\$script"
    Write-Host "执行数据库脚本: $script"

    $sql = Get-Content -Path $scriptPath -Raw
    Invoke-Sqlcmd -ConnectionString $connectionString -Query $sql -ErrorAction Stop
}

# 4. 部署API应用
Write-Host "部署Web API..."
$publishPath = Join-Path $PSScriptRoot "Publish\API"

if (Test-Path $publishPath) {
    Remove-Item -Path $ApiPath -Recurse -Force -ErrorAction SilentlyContinue
    Copy-Item -Path $publishPath -Destination $ApiPath -Recurse
}

# 5. 配置IIS（如果使用）
Write-Host "配置IIS..."
Import-Module WebAdministration

if (!(Get-Website -Name "LYBT Clinic API" -ErrorAction SilentlyContinue)) {
    New-Website -Name "LYBT Clinic API" -Port 443 -PhysicalPath $ApiPath
}

# 6. 启动服务
Write-Host "启动服务..."
Start-WebAppPool -Name "LYBTClinicAPIPool"
Start-Service -Name "LYBT Clinic API" -ErrorAction SilentlyContinue

# 7. 验证部署
Write-Host "验证部署..."
$healthUrl = "https://localhost/health"
$maxAttempts = 10
$attempt = 0

do {
    try {
        $response = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 10
        Write-Host "部署验证成功！" -ForegroundColor Green
        break
    }
    catch {
        $attempt++
        Write-Host "验证尝试 $attempt/$maxAttempts 失败，等待中..."
        Start-Sleep -Seconds 5
    }
} while ($attempt -lt $maxAttempts)

if ($attempt -eq $maxAttempts) {
    Write-Host "部署验证失败！请检查日志。" -ForegroundColor Red
    exit 1
}

Write-Host "凌隐宝堂中医诊所管理系统部署完成！" -ForegroundColor Green
```

### 2. 回滚脚本

```powershell
# Rollback-LYBT.ps1 - 回滚脚本
param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFolder,

    [string]$ApiPath = "C:\Deploy\LYBT.API"
)

Write-Host "开始回滚凌隐宝堂中医诊所管理系统..." -ForegroundColor Yellow

# 1. 停止服务
Write-Host "停止现有服务..."
Stop-Service -Name "LYBT Clinic API" -ErrorAction SilentlyContinue
Stop-WebAppPool -Name "LYBTClinicAPIPool" -ErrorAction SilentlyContinue

# 2. 恢复文件
Write-Host "恢复文件从: $BackupFolder"
if (Test-Path $BackupFolder) {
    Remove-Item -Path $ApiPath -Recurse -Force -ErrorAction SilentlyContinue
    Copy-Item -Path $BackupFolder -Destination $ApiPath -Recurse
} else {
    Write-Host "备份文件夹不存在: $BackupFolder" -ForegroundColor Red
    exit 1
}

# 3. 数据库回滚（如果需要）
# 这里可以添加数据库回滚逻辑

# 4. 启动服务
Write-Host "启动服务..."
Start-WebAppPool -Name "LYBTClinicAPIPool"
Start-Service -Name "LYBT Clinic API" -ErrorAction SilentlyContinue

Write-Host "系统回滚完成！" -ForegroundColor Green
```

## 故障排除

### 1. 常见问题解决

#### API无法启动
```powershell
# 检查.NET运行时
dotnet --list-runtimes

# 检查端口占用
netstat -ano | findstr :5001

# 检查事件日志
Get-EventLog -LogName Application -Source "ASP.NET Core" -Newest 10
```

#### 数据库连接失败
```sql
-- 测试数据库连接
SELECT @@VERSION;

-- 检查数据库状态
SELECT name, state_desc FROM sys.databases WHERE name = 'LYBT_Clinic';

-- 检查用户权限
SELECT DP1.name AS DatabaseRoleName,
       DP2.name AS MemberName
FROM sys.database_role_members AS DRM
JOIN sys.database_principals AS DP1 ON DRM.role_principal_id = DP1.principal_id
JOIN sys.database_principals AS DP2 ON DRM.member_principal_id = DP2.principal_id
WHERE DP1.name = 'db_owner';
```

### 2. 日志分析

```powershell
# 分析IIS日志
$logPath = "C:\inetpub\logs\LogFiles\W3SVC1\*.log"
Select-String -Path $logPath -Pattern "500|404|403" | Select-Object -Last 50

# 分析应用程序日志
Get-Content "C:\Deploy\LYBT.API\logs\lybt-api-*.log" | Select-String "ERROR|FATAL" | Select-Object -Last 20
```

## 部署检查清单

### 部署前检查
- [ ] 确认目标环境配置符合要求
- [ ] 备份当前系统状态
- [ ] 验证数据库备份完整性
- [ ] 检查SSL证书有效性
- [ ] 确认部署脚本已更新
- [ ] 通知相关人员维护窗口

### 部署过程检查
- [ ] 停止现有服务
- [ ] 部署新版本文件
- [ ] 执行数据库迁移
- [ ] 更新配置文件
- [ ] 启动新版本服务
- [ ] 验证基本功能

### 部署后验证
- [ ] API健康检查通过
- [ ] 数据库连接正常
- [ ] 用户登录功能正常
- [ ] 核心业务流程验证
- [ ] 性能指标正常
- [ ] 日志记录正常
- [ ] 监控告警配置生效

### 回滚准备
- [ ] 备份文件可用
- [ ] 回滚脚本准备就绪
- [ ] 数据库回滚计划确认
- [ ] 应急联系方式更新

通过这套完整的部署指南，凌隐宝堂中医诊所管理系统能够安全、稳定地部署到各种环境中，确保系统的高可用性和可靠性。