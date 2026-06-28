# 安全与密码管理

## 概述

系统采用 BCrypt 密码哈希 + 可配置账户锁定策略，支持 DI 可测试的 `IPasswordService` 接口。本文档覆盖密码生命周期、系统管理员账户管理、开发环境配置。

## 密码服务架构

### IPasswordService 接口

位置: `LYBT.Shared.Utilities/Security/IPasswordService.cs`

| 方法 | 用途 |
|------|------|
| `HashPassword(string)` | BCrypt 哈希 (WorkFactor=11) |
| `VerifyPassword(string, string)` | 验证密码与哈希是否匹配 |
| `VerifyAndRehashIfNeeded(string, string)` | 验证 + 若 WorkFactor 过旧则自动重新哈希 |
| `ValidatePassword(string)` | 密码复杂度验证 (≥8 位，含大小写+数字+特殊字符) |
| `GenerateSecurePassword(int)` | 生成符合策略的安全密码 |
| `GenerateTemporaryPassword()` | 生成 12 位临时密码 |
| `SecureEquals(string, string)` | 定时安全比较，防止时序攻击 |

### 实现类

`BcryptPasswordService` (sealed) 封装 `PasswordHelper` 静态方法，通过 DI 注入:

```csharp
services.AddSingleton<IPasswordService, BcryptPasswordService>();
```

**设计意图**: 将静态工具类包装为可 Mock 接口，便于单元测试中隔离密码逻辑。

## 账户锁定配置

### AccountLockoutOptions

配置节: `Security:AccountLockout`

| 属性 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| `Enabled` | bool | `true` | — | 是否启用账户锁定 |
| `MaxFailedCount` | int | `5` | 1-100 | 最大允许失败次数 |
| `LockoutMinutes` | int | `15` | 1-1440 | 锁定持续时间 (分钟) |

### 环境配置示例

**appsettings.json** (基础):
```json
{
  "Security": {
    "AccountLockout": {
      "Enabled": true,
      "MaxFailedCount": 5,
      "LockoutMinutes": 15
    }
  }
}
```

**appsettings.Test.json** (测试环境):
```json
{
  "Security": {
    "AccountLockout": {
      "Enabled": false
    }
  }
}
```

### 锁定流程

```mermaid
graph TD
    A[登录请求] --> B{密码正确?}
    B -->|是| C[重置 FailedLoginCount = 0]
    C --> D[登录成功]
    B -->|否| E[FailedLoginCount++]
    E --> F{Count >= MaxFailedCount?}
    F -->|否| G[返回错误]
    F -->|是| H[设置 LockoutEnd = Now + LockoutMinutes]
    H --> I[返回账户已锁定]
```

## 系统管理员 (sysadmin) 生命周期

### SystemAdminOptions 配置

配置节: `SystemAdmin`

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `UserName` | `"sysadmin"` | 系统管理员用户名 |
| `AutoCreateOnStartup` | `true` | 启动时自动创建 (若不存在) |
| `ForceResetOnStartup` | `false` | 开发环境启动时强制重置密码和状态 |
| `AllowAutoCreateInProduction` | `false` | 生产环境是否允许自动创建 |
| `InitialSetupToken` | — | 生产环境创建时的安全令牌 |
| `SessionTimeoutMinutes` | `240` | 会话超时时间 |

### 启动流程

```mermaid
graph TD
    A[应用启动] --> B{AutoCreateOnStartup?}
    B -->|否| Z[跳过]
    B -->|是| C{生产环境?}
    C -->|是| D{AllowAutoCreateInProduction && InitialSetupToken 有效?}
    D -->|否| Z
    D -->|是| E[创建/更新 sysadmin]
    C -->|否| F{ForceResetOnStartup?}
    F -->|是| G[重置: 密码 + FailedLoginCount + LockoutEnd + Status]
    F -->|否| H{sysadmin 存在?}
    H -->|是| Z
    H -->|否| E
    G --> E
```

### ForceResetOnStartup 行为 (仅开发/测试环境)

启用后，每次启动时重置以下字段:

| 字段 | 重置为 |
|------|--------|
| `PasswordHash` | 使用配置的默认密码重新哈希 |
| `FailedLoginCount` | `0` |
| `LockoutEnd` | `null` |
| `Status` | `Enabled` |
| `IsDeleted` | `false` |

**安全约束**: 生产环境中 `ForceResetOnStartup` 始终被忽略，即使配置为 `true`。

### 开发环境推荐配置

`appsettings.Development.json`:

```json
{
  "SystemAdmin": {
    "AutoCreateOnStartup": true,
    "ForceResetOnStartup": true
  },
  "DefaultPasswords": {
    "EnableInDevelopment": true
  },
  "Security": {
    "AccountLockout": {
      "Enabled": true,
      "MaxFailedCount": 10,
      "LockoutMinutes": 1
    }
  }
}
```

这样开发者每次启动都能用默认密码登录，不会因为反复测试而被锁定。

## 密码哈希生成

工具: `src/Tools/PasswordHashGenerator/`

```bash
dotnet run --project src/Tools/PasswordHashGenerator/PasswordHashGenerator -- "YourPassword"
```

默认密码 `DevPass123!` 的预计算哈希:
```
$2a$11$0IviQQSC517yFyWB47YDh.P.mHetOQwFkvgdMtl8UFWn6v4iKKJ8e
```

## 安全设计决策

| 决策 | 理由 |
|------|------|
| BCrypt WorkFactor=11 | 平衡安全性与性能 (~200ms/hash) |
| IPasswordService 接口 | 测试可 Mock，避免静态方法直接依赖 |
| 可配置锁定策略 | 替代硬编码常量，便于不同环境调整 |
| ForceResetOnStartup 仅限开发 | 防止生产环境意外重置管理员账户 |
| FixedTimeEquals 令牌比较 | 防止时序攻击泄漏 InitialSetupToken |

## Identity 集成约束（铁律）

> 以下约束源自 ASP.NET Core Identity 集成层，违反将导致运行时错误或安全漏洞。根 `AGENTS.md`「Common Pitfalls」同步。

### 1. DI 注册顺序：`AddIdentity()` 必须在 `AddAuthentication()` 前

`AddIdentity<TUser, TRole>()` 内部会调用 `AddAuthentication()` 并覆盖默认方案配置。若 JWT 的 `AddAuthentication()` 在其后注册，将覆盖 Identity 的默认方案，导致 JWT 持有者认证失效。

```csharp
// ✅ 正确顺序
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(options => { /* JWT Bearer 配置 */ })
    .AddJwtBearer(/* ... */);

// ❌ 错误顺序 — Identity 会覆盖 JWT 方案
builder.Services.AddAuthentication(/* JWT */);
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(/* ... */);
```

### 2. `UserManager<T>` / `RoleManager<T>` 是 **SCOPED** 服务

这两个管理器持有 `DbContext`（Scoped）和 `ILogger`（Singleton）依赖。**禁止从 root provider 直接 resolve**：

```csharp
// ❌ 错误 — 在 startup 配置阶段、单例服务、事件回调中直接获取
var userManager = app.Services.GetRequiredService<UserManager<ApplicationUser>>();
await userManager.CreateAsync(user, password);

// ✅ 正确 — 从 scope 获取
using var scope = app.Services.CreateScope();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
await userManager.CreateAsync(user, password);
```

`IdentitySeedData` 等启动初始化器必须使用 `scope.ServiceProvider`（参见 `DatabaseInitializationService`）。

### 3. BCrypt (`PasswordHelper`) 与 PBKDF2 (Identity) 不兼容 — 全部走 `UserManager`

ASP.NET Core Identity 内部使用 PBKDF2 算法哈希密码。本项目 `PasswordHelper` / `BcryptPasswordService` 使用 BCrypt（WorkFactor=11）。**两套哈希不互通**，直接调 `PasswordHelper` 创建/修改密码的用户无法通过 Identity 登录。

```csharp
// ❌ 错误 — 用 BCrypt 哈希后存库，Identity 验证失败
user.PasswordHash = _passwordService.HashPassword(password);
await _userManager.UpdateAsync(user);

// ✅ 正确 — 全部通过 UserManager 处理（内部用 PBKDF2）
var result = await _userManager.CreateAsync(user, password);
// 修改密码
var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
// 或 ChangePasswordAsync / RemovePasswordAsync + AddPasswordAsync
```

`BcryptPasswordService`（`IPasswordService`）仅用于非 Identity 路径（如 sysadmin 创建脚本的工具校验、`SecureEquals` 防时序比较等），不得作为用户密码落库入口。

### 4. `IdentitySeedData` 仅在 `LastLoginAt == null` 时重置密码

种子数据使用幂等策略：仅在用户**从未登录过**（`LastLoginAt` 为 null）时重置密码到 `DefaultPasswords`。**已登录过的用户密码不会被覆盖**，开发者反复测试登录后重新启动服务不会破坏已设密码。

```csharp
// IdentitySeedData 简化逻辑
var existing = await _userManager.FindByNameAsync(userName);
if (existing?.LastLoginAt == null)
{
    await _userManager.RemovePasswordAsync(existing);
    await _userManager.AddPasswordAsync(existing, configuredPassword);
    _logger.LogInformation("重置密码 (首次启动): {User}", userName);
}
```

如需强制重置，使用 `appsettings.Development.json` 的 `SystemAdmin:ForceResetOnStartup: true`（仅开发环境生效，生产环境始终被忽略）。

## 相关文档

- [配置架构](../03-architecture/07-configuration.md) — Options 模式与验证管道
- [API 认证](../04-api-reference/README.md) — JWT + RefreshToken 认证流程
- [测试指南](05-testing.md) — 测试中如何配置安全选项
