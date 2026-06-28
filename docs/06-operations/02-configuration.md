# 配置说明

本文档包含 LYBT.WebAPI 的完整配置项说明。配置文件位于 `appsettings.json`，生产环境通过 `appsettings.Production.json` 覆盖。

> 部署架构总览见 [README.md](./README.md)

---

## 配置节总览

| 配置节 | 说明 | 文件 |
|--------|------|------|
| `ConnectionStrings` | 数据库连接 | appsettings.json |
| `Jwt` | Token 签名密钥、过期时间 | appsettings.json |
| `DefaultPasswords` | 默认密码（开发占位明文，生产由 DefaultPasswordService 随机生成） | appsettings.json |
| `DesktopUpdate` | Desktop 客户端升级配置 | appsettings.Production.json |
| `Session` | 会话超时、并发控制 | appsettings.json |
| `Security.RateLimiting` | 限流策略 | appsettings.json |
| `Database` | 连接池、重试策略 | appsettings.json |
| `MemoryCache` | 内存缓存策略 | appsettings.json |
| `Kestrel` | Web 服务器端口和限制 | appsettings.json |
| `SystemAdmin` | 系统管理员初始化 | appsettings.json |
| `Serilog` | 日志级别、输出目标 | appsettings.json |
| `FeatureToggles` | 功能开关 | appsettings.json |
| `ClinicSettings` | 诊所业务参数 | appsettings.json |

---

## 环境变量覆盖机制

ASP.NET Core 支持通过环境变量覆盖 JSON 配置节，使用 `__`（双下划线）作为层级分隔符：

| JSON 配置路径 | 环境变量名 |
|---------------|-----------|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Jwt:SecretKey` | `Jwt__SecretKey` |
| `Jwt:AccessTokenExpirationMinutes` | `Jwt__AccessTokenExpirationMinutes` |
| `DefaultPasswords:SysAdminPassword` | `DefaultPasswords__SysAdminPassword` |
| `Database:AutoMigrate` | `Database__AutoMigrate` |
| `Serilog:MinimumLevel:Default` | `Serilog__MinimumLevel__Default` |

**生产环境建议**：敏感信息（连接字符串、密钥、密码）通过环境变量或 Azure Key Vault 注入，不要写入配置文件。

```bash
# Windows 命令行设置环境变量（重启生效）
setx ConnectionStrings__DefaultConnection "Server=.;Database=LYBTDB_Dev;Trusted_Connection=True;TrustServerCertificate=true"
setx Jwt__SecretKey "YourSecureSecretKeyAtLeast32CharactersLong"

# PowerShell 临时设置（当前会话有效）
$env:ConnectionStrings__DefaultConnection = "Server=.;Database=LYBTDB_Dev;Trusted_Connection=True;TrustServerCertificate=true"
$env:Jwt__SecretKey = "YourSecureSecretKeyAtLeast32CharactersLong"
```

---

## 连接字符串

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LYBTDB_Dev;Trusted_Connection=True;TrustServerCertificate=true;Encrypt=true",
    "LYBTDesktop": "Server=(localdb)\\MSSQLLocalDB;Database=LYBTDesktop;Integrated Security=true"
  }
}
```

> **N1 数据孤立（v1.0 范围）**：v1.0 远程库（`DefaultConnection`）与本地库（`LYBTDesktop`，LocalDB）**数据孤立、不互通**。本地模式定位为「远程故障应急降级」，断网期录入的数据事后手动补录或可丢。双向同步属 **v2.0 规划**，v1.0 不实现。

| 参数 | 说明 |
|------|------|
| `Server` | SQL Server 实例名（`.` = 本地默认实例） |
| `Database` | 数据库名 |
| `Trusted_Connection` | Windows 身份验证 |
| `TrustServerCertificate` | 跳过证书验证（开发环境） |
| `Encrypt` | 启用加密（生产环境必须 `true`） |
| `Connection Timeout` | 连接超时秒数（默认 15） |

---

## 功能开关

```json
{
  "FeatureToggles": {
    "EnableDesktopAutoUpdate": true,
    "EnableRegistrationQueue": true,
    "EnableOfflineMode": true,
    "EnablePrintPrescription": true,
    "EnableSyncService": true,
    "EnableDebugDiagnostics": false
  }
}
```

| 开关 | 默认值 | 说明 |
|------|--------|------|
| `EnableDesktopAutoUpdate` | `true` | Desktop 客户端自动升级 |
| `EnableRegistrationQueue` | `true` | 挂号排队号自动编号 |
| `EnableOfflineMode` | `true` | Desktop 离线模式支持 |
| `EnablePrintPrescription` | `true` | 处方打印功能 |
| `EnableSyncService` | `true` | 远程同步服务 |
| `EnableDebugDiagnostics` | `false` | 诊断端点（生产环境关闭） |

---

## 诊所业务参数

```json
{
  "ClinicSettings": {
    "ClinicName": "凌隐宝堂中医诊所",
    "BusinessHours": "08:30-17:30",
    "MaxDailyRegistrations": 100,
    "DefaultRegistrationFee": 0,
    "PrescriptionPageSize": 16,
    "HerbRoleOrder": ["君", "臣", "佐", "使"]
  }
}
```

| 参数 | 说明 |
|------|------|
| `ClinicName` | 诊所名称（用于打印输出） |
| `BusinessHours` | 营业时间（展示用） |
| `MaxDailyRegistrations` | 每日最大挂号数 |
| `DefaultRegistrationFee` | 默认挂号费 |
| `PrescriptionPageSize` | 处方单每页药材数 |
| `HerbRoleOrder` | 药材角色排序（君→臣→佐→使） |

---

## JWT 配置

```json
{
  "Jwt": {
    "SecretKey": "...",                      // 生产环境必须更换
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "AccessTokenExpirationMinutes": 480,     // Access Token 有效期（开发默认 8 小时；Production 覆盖为 30 分钟）
    "RefreshTokenExpirationDays": 7,         // Refresh Token 有效期
    "ClockSkewSeconds": 30                   // 时钟偏差容忍
  }
}
```

---

## 默认密码配置

```json
{
  "DefaultPasswords": {
    "SysAdminPassword": "SysAdmin@2026!",        // 开发占位明文（设计如此）
    "AdminPassword": "Admin@123456",             // 开发占位明文（设计如此）
    "NewUserPassword": "User@123456",            // 开发占位明文（设计如此）
    "ForceChangeOnFirstLogin": true              // 首次登录强制修改密码
  }
}
```

> **说明**：`appsettings.json` 中的默认密码为**开发环境占位明文**（设计如此，便于初始化）。生产环境由 `DefaultPasswordService` 随机生成强密码（US-SHELL-017 生产环境安全门控），不沿用此占位值。环境变量覆盖优先级：`DefaultPasswords__SysAdminPassword` 等 > JSON。

---

## 会话配置

```json
{
  "Session": {
    "TimeoutMinutes": 120,                   // 会话超时 (分钟)
    "AllowConcurrentSessions": false,        // 禁止并发会话 (同一账号仅允许一处登录)
    "SlidingExpiration": true                 // 滑动过期 (有活动时自动延长)
  }
}
```

---

## 限流配置

```json
{
  "Security": {
    "RateLimiting": {
      "Enabled": true,
      "GlobalLimit": {
        "PermitLimit": 200,
        "WindowSeconds": 60,
        "QueueLimit": 0
      },
      "LoginLimit": {
        "PermitLimit": 5,
        "InternalPermitLimit": 20,
        "WindowSeconds": 60,
        "QueueLimit": 0,
        "InternalQueueLimit": 0
      },
      "ApiLimit": {
        "PermitLimit": 100,
        "AdminPermitLimit": 200,
        "WindowSeconds": 60,
        "QueueLimit": 0
      },
      "WhitelistedIPs": ["127.0.0.1", "::1"]
    }
  }
}
```

---

## 数据库配置

```json
{
  "Database": {
    "AutoMigrate": false,                    // 生产环境关闭自动迁移
    "ConnectionPool": {
      "MaxConnections": 100,
      "MinConnections": 5,
      "ConnectionTimeoutSeconds": 30,
      "CommandTimeoutSeconds": 30
    },
    "RetryPolicy": {
      "MaxRetryCount": 3,
      "BaseDelayMs": 1000,
      "MaxDelayMs": 10000
    },
    "Monitoring": {
      "Enabled": true,
      "SlowQueryThresholdMs": 1000
    }
  }
}
```

---

## 内存缓存配置

```json
{
  "MemoryCache": {
    "Enabled": true,                         // 是否启用缓存
    "SizeLimit": 104857600,                  // 缓存大小上限 (字节，约 100MB)
    "CompactionPercentage": 0.05,            // 压缩比例
    "ExpirationScanFrequencySeconds": 60,    // 过期扫描频率 (秒)
    "DefaultExpirationMinutes": 5            // 默认过期时间 (分钟)
  }
}
```

---

## Kestrel 配置

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"       // HTTP 端口
      },
      "Https": {
        "Url": "https://localhost:5001"      // HTTPS 端口
      }
    },
    "Limits": {
      "MaxRequestBodySize": 10485760         // 请求体上限 (字节，约 10MB)
    }
  }
}
```

---

## 系统管理员配置

```json
{
  "SystemAdmin": {
    "UserName": "sysadmin",                  // 管理员用户名
    "Email": "admin@lybt.com",               // 管理员邮箱
    "DisplayName": "系统管理员",               // 显示名称
    "AutoCreateOnStartup": true,             // 启动时自动创建 (不存在时)
    "SessionTimeoutMinutes": 240             // 管理员会话超时 (分钟)
  }
}
```

---

## 生产环境注意事项

1. **Jwt.SecretKey** - 必须替换为强随机密钥，禁止使用开发环境密钥
2. **Database.AutoMigrate** - 生产环境必须设为 `false`，使用手动迁移
3. **DefaultPasswords** - 首次部署后应立即修改默认密码
4. **ConnectionStrings** - 生产环境建议使用环境变量或密钥管理服务注入

---

## 常见配置问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| JWT Token 立即过期 | `AccessTokenExpirationMinutes` 设置过小 | 默认 480 分钟（8小时），Production 覆盖为 30 分钟 |
| 登录 5 次后被锁 | `LoginLimit.PermitLimit` 触发限流 | 调整限流配置或将测试 IP 加入 `WhitelistedIPs` |
| 缓存不生效 | `MemoryCache.Enabled` 为 false | 确认生产配置已启用缓存 |
| 数据库连接超时 | `ConnectionTimeoutSeconds` 过小或网络延迟 | 检查网络连通性，适当增大超时值 |
| sysadmin 未自动创建 | `SystemAdmin.AutoCreateOnStartup` 为 false | 设为 true 并重启，首次创建后建议关闭 |
| 日志文件过大 | 未配置日志清理 | 启用 `Logging.Cleanup` 并设置合理 `RetentionDays` |

### 配置变更生效方式

| 配置节 | 生效方式 | 说明 |
|--------|---------|------|
| `ConnectionStrings` | 重启应用 | 连接池在启动时初始化 |
| `Jwt` | 重启应用 | Token 验证参数在启动时加载 |
| `Security.RateLimiting` | 重启应用 | 限流策略在启动时注册 |
| `Serilog` | 热更新 | 支持运行时调整日志级别 (via Diagnostics API) |
| `MemoryCache` | 重启应用 | 缓存策略在启动时配置 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 从 README.md 拆分，补充 PasswordPolicy/Session/MemoryCache/Kestrel/SystemAdmin 配置节 |
| 2026-02-22 | v1.1 | 新增常见配置问题 + 配置变更生效方式表 |
| 2026-06-25 | v1.2 | 明确 AccessToken 开发默认 8h，Production 覆盖为 30min |
| 2026-06-25 | v1.3 | 新增环境变量覆盖机制、ConnectionStrings 示例、FeatureToggles、ClinicSettings 配置节 |
| 2026-06-28 | v1.4 | ConnectionStrings key 对齐 `DefaultConnection`；DefaultPasswords 如实描述（开发占位明文，生产 DefaultPasswordService 随机生成）；补 N1 数据孤立说明（v1.0 远程/本地不互通） |
