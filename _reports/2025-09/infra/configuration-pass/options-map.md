# Configuration Options 统一映射报告

## 📊 现有配置对象分析

### 1. 已存在的Options类（Infrastructure层）

#### 1.1 JwtOptions ✅ **标准**
**位置**: `src/Server/Core/LYBT.Infrastructure/Configuration/Options/JwtOptions.cs`
**配置键**: `JwtOptions`
**状态**: 已完善DataAnnotations，无需修改

```csharp
- Secret [Required, MinLength(32)]
- Issuer [Required] = "LYBT.WebAPI"
- Audience [Required] = "LYBT.Client" 
- ExpireMinutes [Range(1,1440)] = 480
- RememberMeExpireMinutes [Range(1440,525600)] = 43200
- ClockSkewSeconds [Range(0,3600)] = 300
```

#### 1.2 SecurityOptions ✅ **标准**
**位置**: `src/Server/Core/LYBT.Infrastructure/Configuration/Options/SecurityOptions.cs`
**配置键**: `Security`
**状态**: 已完善DataAnnotations，结构良好

包含子配置：
- HttpsOptions
- CorsOptions  
- SecurityHeadersOptions
- PasswordPolicyOptions
- RateLimitOptions
- EnvironmentOptions

#### 1.3 DatabaseOptions ✅ **标准**
**位置**: `src/Server/Core/LYBT.Infrastructure/Configuration/Options/DatabaseOptions.cs`
**配置键**: `DatabaseOptions`
**状态**: 已完善DataAnnotations，包含完整数据库配置

包含子配置：
- ConnectionPoolOptions
- DatabaseMonitoringOptions  
- DatabaseBackupOptions

#### 1.4 AuthOptions ✅ **标准**
**位置**: `src/Server/Core/LYBT.Infrastructure/Configuration/Options/AuthOptions.cs`
**配置键**: `AuthOptions`
**状态**: 需要补充DataAnnotations

#### 1.5 SysAdminOptions ⚠️ **需要整合**
**位置**: `src/Server/Core/LYBT.Infrastructure/Configuration/Options/SysAdminOptions.cs`  
**配置键**: `SysAdminOptions`
**问题**: 默认密码配置散布，需要统一治理

```csharp
- DefaultPassword [Required, MinLength(8)] = "Admin@123456"
- RequirePasswordChangeOnFirstLogin = true
- EnableAccountLockout = false
```

### 2. 模块级Options类（需要统一）

#### 2.1 UserOptions ⚠️ **需要迁移到Infrastructure**
**位置**: `src/Server/Modules/LYBT.Module.Users/UserOptions.cs`
**配置键**: `UserOptions`
**问题**: 位置不当，应迁移到Infrastructure层

```csharp
- DefaultUserPassword = "ChangeMe123"
- EnableUserCache = true
- UserCacheExpirationMinutes = 30
- MaxBatchOperationSize = 100
- EnableDetailedAuditLogging = true
- SendPasswordResetNotification = false
```

## 🔧 统一整合方案

### 阶段1：Options类规范化

#### 1.1 迁移UserOptions到Infrastructure层
**行动**: 
```
从: src/Server/Modules/LYBT.Module.Users/UserOptions.cs
到: src/Server/Core/LYBT.Infrastructure/Configuration/Options/UserOptions.cs
```

**更新内容**:
- 添加DataAnnotations验证
- 统一命名空间为 `LYBT.Infrastructure.Configuration.Options`
- 添加SectionName常量

#### 1.2 完善AuthOptions的DataAnnotations
**需要添加的验证注解**:
```csharp
[Range(1, 100, ErrorMessage = "最大登录失败次数必须在1-100之间")]
public int MaxFailedLoginAttempts { get; set; } = 5;

[Range(1, 1440, ErrorMessage = "账户锁定时长必须在1-1440分钟之间")]  
public TimeSpan AccountLockoutDuration { get; set; } = TimeSpan.FromMinutes(30);
```

#### 1.3 创建统一的DefaultPasswordOptions
**新建**: `src/Server/Core/LYBT.Infrastructure/Configuration/Options/DefaultPasswordOptions.cs`
**目标**: 统一管理所有默认密码配置

```csharp
public class DefaultPasswordOptions
{
    public const string SectionName = "DefaultPasswords";
    
    [Required, MinLength(8)]
    public string SystemAdmin { get; set; } = "Admin@123456";
    
    [Required, MinLength(8)] 
    public string NewUser { get; set; } = "ChangeMe123";
    
    /// <summary>开发环境是否启用默认密码（生产环境强制false）</summary>
    public bool EnableInDevelopment { get; set; } = true;
    
    /// <summary>生产环境是否允许默认密码（应始终为false）</summary>
    public bool AllowInProduction { get; set; } = false;
}
```

## 📋 配置键映射表（旧→新）

### 直接保持的配置键
| 现有配置键 | Options类 | 状态 |
|-----------|----------|------|
| `JwtOptions` | JwtOptions | ✅ 保持不变 |
| `Security` | SecurityOptions | ✅ 保持不变 |
| `DatabaseOptions` | DatabaseOptions | ✅ 保持不变 |
| `AuthOptions` | AuthOptions | ✅ 保持不变 |

### 需要迁移的配置键  
| 旧配置键 | 新配置键 | Options类 | 兼容策略 |
|---------|---------|----------|----------|
| `UserOptions` | `UserOptions` | UserOptions | 兼容别名：Module.Users:UserOptions |
| `SysAdminOptions:DefaultPassword` | `DefaultPasswords:SystemAdmin` | DefaultPasswordOptions | 兼容别名3个月 |
| `UserOptions:DefaultUserPassword` | `DefaultPasswords:NewUser` | DefaultPasswordOptions | 兼容别名3个月 |

### 新增配置键
| 配置键 | Options类 | 说明 |
|-------|----------|------|
| `DefaultPasswords` | DefaultPasswordOptions | 统一默认密码管理 |

## 🔒 安全加固要求

### 环境校验规则
1. **生产环境**:
   - `DefaultPasswords:AllowInProduction` 必须为 `false`
   - `JwtOptions:Secret` 必须来自环境变量/Secret
   - `ConnectionStrings:DefaultConnection` 必须来自环境变量/Secret

2. **开发环境**:
   - 允许使用默认配置
   - 允许配置文件中的Secret（仅用于开发）

### 校验实现
```csharp
// 生产环境校验  
if (environment.IsProduction())
{
    var defaultPasswords = GetOption<DefaultPasswordOptions>();
    if (defaultPasswords.AllowInProduction)
    {
        throw new InvalidOperationException("生产环境禁止启用默认密码");
    }
    
    var jwtOptions = GetOption<JwtOptions>();
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_SECRET")))
    {
        throw new InvalidOperationException("生产环境JWT密钥必须通过环境变量设置");
    }
}
```

## 📁 目录结构（目标）

```
src/Server/Core/LYBT.Infrastructure/Configuration/Options/
├── AuthOptions.cs              ✅ 已存在，需完善DataAnnotations
├── DatabaseOptions.cs          ✅ 已存在，已完善
├── DefaultPasswordOptions.cs   🆕 新建，统一默认密码
├── JwtOptions.cs               ✅ 已存在，已完善  
├── SecurityOptions.cs          ✅ 已存在，已完善
├── SysAdminOptions.cs          ⚠️ 重构，移除DefaultPassword
└── UserOptions.cs              🔄 从Module.Users迁移，完善DataAnnotations
```

## 🎯 下一步行动

### 步骤1: 创建DefaultPasswordOptions
- [x] 分析现有默认密码配置分布
- [ ] 创建DefaultPasswordOptions类
- [ ] 添加环境校验逻辑

### 步骤2: 迁移UserOptions  
- [ ] 从Module.Users复制到Infrastructure/Configuration/Options
- [ ] 更新命名空间和添加验证注解
- [ ] 更新DI注册

### 步骤3: 完善现有Options
- [ ] 为AuthOptions添加DataAnnotations
- [ ] 重构SysAdminOptions移除密码配置

### 步骤4: 兼容性处理
- [ ] 添加配置键别名支持  
- [ ] 在日志中记录配置迁移提示
- [ ] 更新相关服务的Options注入

---

**分析完成时间**: 2025-09-13  
**下一步**: 实施Options类统一整合  
**预计完成**: 执行步骤①后立即提交