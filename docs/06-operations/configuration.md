# 配置说明

本文档包含 LYBT.WebAPI 的完整配置项说明。配置文件位于 `appsettings.json`，生产环境通过 `appsettings.Production.json` 覆盖。

> 部署架构总览见 [README.md](./README.md)

---

## 配置节总览

| 配置节 | 说明 | 文件 |
|--------|------|------|
| `ConnectionStrings` | 数据库连接 | appsettings.json |
| `Jwt` | Token 签名密钥、过期时间 | appsettings.json |
| `PasswordPolicy` | 密码复杂度要求 | appsettings.json |
| `Session` | 会话超时、并发控制 | appsettings.json |
| `Security.RateLimiting` | 限流策略 | appsettings.json |
| `Database` | 连接池、重试策略 | appsettings.json |
| `MemoryCache` | 内存缓存策略 | appsettings.json |
| `Kestrel` | Web 服务器端口和限制 | appsettings.json |
| `SystemAdmin` | 系统管理员初始化 | appsettings.json |
| `Serilog` | 日志级别、输出目标 | appsettings.json |

---

## JWT 配置

```json
{
  "Jwt": {
    "SecretKey": "...",                      // 生产环境必须更换
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "AccessTokenExpirationMinutes": 30,      // Access Token 有效期
    "RefreshTokenExpirationDays": 7,         // Refresh Token 有效期
    "ClockSkewSeconds": 300                  // 时钟偏差容忍
  }
}
```

---

## 密码策略配置

```json
{
  "PasswordPolicy": {
    "MinLength": 8,                          // 最小长度
    "RequireDigit": true,                    // 要求包含数字
    "RequireLowercase": true,                // 要求包含小写字母
    "RequireUppercase": true,                // 要求包含大写字母
    "RequireSpecialChar": true               // 要求包含特殊字符
  }
}
```

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
      "GlobalLimit": { "PermitLimit": 200, "WindowSeconds": 60 },
      "LoginLimit": { "PermitLimit": 5, "WindowSeconds": 60 },
      "ApiLimit": { "PermitLimit": 100, "WindowSeconds": 60 },
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
      "MaxConnections": 20,
      "MinConnections": 2,
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
| JWT Token 立即过期 | `AccessTokenExpirationMinutes` 设置过小 | 生产环境建议 30 分钟，开发环境可设 120 分钟 |
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

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 从 README.md 拆分，补充 PasswordPolicy/Session/MemoryCache/Kestrel/SystemAdmin 配置节 |
| 2026-02-22 | v1.1 | 新增常见配置问题 + 配置变更生效方式表 |
