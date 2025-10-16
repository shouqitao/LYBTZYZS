# 配置参数详细指南

**凌隐宝堂中医诊所管理系统 - Configuration Parameters Guide**  
**创建时间**: 2025-10-16  
**适用文档**: docs/quick-reference/config-templates.md  

---

## 🔧 JWT认证配置参数详解

### 开发环境JWT配置 (`Lybt:Authentication:Jwt`)

| 参数名 | 类型 | 默认值 | 取值范围 | 说明 |
|--------|------|--------|----------|------|
| **SecretKey** | string | J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw== | 256位Base64编码 | JWT签名密钥，生产环境必须更换 |
| **Issuer** | string | "LYBT.WebAPI" | 1-100字符 | Token发行者标识，建议使用应用名称 |
| **Audience** | string | "LYBT.Client" | 1-100字符 | Token接收者标识，建议使用客户端应用名称 |
| **AccessTokenExpirationMinutes** | int | 30 | 5-1440 | 访问令牌过期时间（分钟） |
| **RefreshTokenExpirationDays** | int | 7 | 1-365 | 刷新令牌过期时间（天） |
| **ClockSkewSeconds** | int | 300 | 60-600 | 时钟偏移容忍度（秒） |

### 配置建议

#### 开发环境
```json
{
  "AccessTokenExpirationMinutes": 30,
  "RefreshTokenExpirationDays": 7,
  "ClockSkewSeconds": 300
}
```

#### 生产环境
```json
{
  "AccessTokenExpirationMinutes": 15,
  "RefreshTokenExpirationDays": 7,
  "ClockSkewSeconds": 60
}
```

#### 高安全环境
```json
{
  "AccessTokenExpirationMinutes": 5,
  "RefreshTokenExpirationDays": 1,
  "ClockSkewSeconds": 60
}
```

---

## 🔒 密码策略配置参数详解

### 密码策略 (`Lybt:Authentication:PasswordPolicy`)

| 参数名 | 类型 | 默认值 | 取值范围 | 说明 |
|--------|------|--------|----------|------|
| **MinLength** | int | 8/12/6 | 4-128 | 密码最小长度（开发/生产/测试） |
| **RequireDigit** | bool | true | true/false | 要求数字字符 |
| **RequireLowercase** | bool | true | true/false | 要求小写字母 |
| **RequireUppercase** | bool | true | true/false | 要求大写字母 |
| **RequireSpecialChar** | bool | true | true/false | 要求特殊字符 |

### 环境差异说明

| 环境 | MinLength | RequireDigit | RequireLowercase | RequireUppercase | RequireSpecialChar |
|------|-----------|-------------|-----------------|-----------------|-------------------|
| 开发环境 | 8 | true | true | true | true |
| 生产环境 | 12 | true | true | true | true |
| 测试环境 | 6 | false | false | false | false |

---

## 🗄️ 数据库连接配置参数详解

### 连接字符串参数

| 参数名 | 类型 | 默认值 | 取值范围 | 说明 |
|--------|------|--------|----------|------|
| **Server** | string | localhost | 有效主机名/IP | 数据库服务器地址 |
| **Database** | string | LYBTDB | 1-128字符 | 数据库名称 |
| **Trusted_Connection** | bool | true | true/false | Windows身份认证 |
| **TrustServerCertificate** | bool | true | true/false | 信任服务器证书 |
| **MultipleActiveResultSets** | bool | true | true/false | 允许多活动结果集 |
| **Connection Timeout** | int | 30 | 5-300 | 连接超时时间（秒） |
| **Command Timeout** | int | 30 | 30-600 | 命令执行超时时间（秒） |
| **Max Pool Size** | int | 20/10/100 | 10-1000 | 连接池最大连接数 |
| **Min Pool Size** | int | 2/1/5 | 1-20 | 连接池最小连接数 |
| **Pooling** | bool | true | true/false | 启用连接池 |

### 环境配置建议

#### 开发环境（小型项目）
```json
{
  "Max Pool Size": 20,
  "Min Pool Size": 2,
  "Connection Timeout": 30,
  "Command Timeout": 30
}
```

#### 生产环境（中型项目）
```json
{
  "Max Pool Size": 100,
  "Min Pool Size": 5,
  "Connection Timeout": 60,
  "Command Timeout": 60
}
```

#### 小诊所优化环境
```json
{
  "Max Pool Size": 10,
  "Min Pool Size": 1,
  "Connection Timeout": 10,
  "Command Timeout": 15
}
```

---

## 🚦 限流配置参数详解

### 全局限流 (`Lybt:Security:RateLimiting:GlobalLimit`)

| 参数名 | 类型 | 默认值 | 取值范围 | 说明 |
|--------|------|--------|----------|------|
| **PermitLimit** | int | 200/1000/禁用 | 1-10000 | 每分钟允许的请求数 |
| **WindowSeconds** | int | 60 | 1-3600 | 时间窗口（秒） |
| **QueueLimit** | int | 0/100 | 0-1000 | 队列限制数量 |

### 登录限流 (`Lybt:Security:RateLimiting:LoginLimit`)

| 参数名 | 类型 | 默认值 | 取值范围 | 说明 |
|--------|------|--------|----------|------|
| **PermitLimit** | int | 5/3/禁用 | 1-100 | 每分钟登录尝试次数 |
| **InternalPermitLimit** | int | 20/10 | 1-1000 | 内部网络限制 |
| **WindowSeconds** | int | 60/300 | 1-3600 | 时间窗口（秒） |

### 环境配置策略

#### 开发环境
```json
{
  "GlobalLimit": {
    "PermitLimit": 200,
    "WindowSeconds": 60,
    "QueueLimit": 0
  },
  "LoginLimit": {
    "PermitLimit": 5,
    "WindowSeconds": 60
  }
}
```

#### 生产环境
```json
{
  "GlobalLimit": {
    "PermitLimit": 1000,
    "WindowSeconds": 60,
    "QueueLimit": 100
  },
  "LoginLimit": {
    "PermitLimit": 3,
    "WindowSeconds": 300
  }
}
```

---

## 📊 缓存配置参数详解

### 内存缓存 (`Lybt:Infrastructure:Cache:MemoryCache`)

| 参数名 | 类型 | 默认值 | 取值范围 | 说明 |
|--------|------|--------|----------|------|
| **Enabled** | bool | true | true/false | 是否启用内存缓存 |
| **SizeLimit** | long | 100MB/256MB | 1MB-1GB | 缓存大小限制（字节） |
| **CompactionPercentage** | double | 0.05 | 0.01-0.5 | 压缩百分比 |
| **ExpirationScanFrequencySeconds** | int | 60/120 | 10-3600 | 过期扫描频率（秒） |
| **DefaultExpirationMinutes** | int | 5/30/1 | 1-1440 | 默认过期时间（分钟） |

### 缓存优化建议

#### 小型应用（<100用户）
```json
{
  "SizeLimit": 52428800,
  "DefaultExpirationMinutes": 5,
  "ExpirationScanFrequencySeconds": 60
}
```

#### 中型应用（100-1000用户）
```json
{
  "SizeLimit": 268435456,
  "DefaultExpirationMinutes": 30,
  "ExpirationScanFrequencySeconds": 120
}
```

---

## 🔧 系统管理员配置详解

### 管理员配置 (`Lybt:Business:SystemAdmin`)

| 参数名 | 类型 | 默认值 | 取值范围 | 说明 |
|--------|------|--------|----------|------|
| **Username** | string | sysadmin/clinic_admin | 3-50字符 | 系统管理员用户名 |
| **Email** | string | admin@lybt.com | 有效邮箱格式 | 管理员邮箱 |
| **DisplayName** | string | 系统管理员 | 2-100字符 | 显示名称 |
| **AutoCreateOnStartup** | bool | true/false | true/false | 启动时自动创建 |
| **SessionTimeoutMinutes** | int | 240/120 | 30-1440 | 会话超时时间（分钟） |

### 环境配置差异

| 环境 | Username | AutoCreateOnStartup | SessionTimeoutMinutes |
|------|----------|-------------------|----------------------|
| 开发环境 | sysadmin | true | 240分钟（4小时） |
| 生产环境 | clinic_admin | false | 120分钟（2小时） |
| 小诊所环境 | sysadmin | true | 240分钟（4小时） |

---

## 📝 配置最佳实践

### 1. 安全配置原则

#### 密钥管理
- ❌ **禁止**: 在配置文件中硬编码生产密钥
- ✅ **推荐**: 使用环境变量或密钥管理服务
- 🔄 **轮换**: 定期更换JWT密钥（建议每3个月）

#### 连接字符串安全
```json
// ❌ 不安全：包含明文密码
"DefaultConnection": "Server=prod;Database=LYBTDB;User Id=admin;Password=Password123!"

// ✅ 安全：使用环境变量
"DefaultConnection": "#{DATABASE_CONNECTION_STRING}#"
```

### 2. 性能优化配置

#### 连接池调优
```json
// 小型应用（<50并发）
{
  "Max Pool Size": 20,
  "Min Pool Size": 2
}

// 中型应用（50-200并发）
{
  "Max Pool Size": 100,
  "Min Pool Size": 5
}

// 大型应用（>200并发）
{
  "Max Pool Size": 500,
  "Min Pool Size": 10
}
```

#### 缓存策略
```json
// 读多写少场景
{
  "DefaultExpirationMinutes": 30,
  "SizeLimit": "512MB"
}

// 频繁更新场景
{
  "DefaultExpirationMinutes": 5,
  "SizeLimit": "128MB"
}
```

### 3. 监控和日志配置

#### 日志级别建议
| 环境 | Default | Microsoft.AspNetCore | LYBT.* |
|------|---------|---------------------|---------|
| 开发 | Information | Warning | Information |
| 测试 | Information | Warning | Information |
| 生产 | Warning | Error | Information |

---

## 🔍 配置验证清单

### 部署前检查
- [ ] JWT密钥已更换为生产环境密钥
- [ ] 数据库连接字符串使用环境变量
- [ ] 限流配置适合当前环境
- [ ] 缓存大小符合可用内存限制
- [ ] 日志级别设置正确
- [ ] 系统管理员配置已检查

### 运行时监控
- [ ] 数据库连接池使用率 < 80%
- [ ] 缓存命中率 > 70%
- [ ] API响应时间 < 500ms
- [ ] 内存使用率 < 80%
- [ ] CPU使用率 < 70%

---

**文档维护**: 本配置指南应与config-templates.md同步更新，确保参数说明的准确性和完整性。