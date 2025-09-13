# 登录重试与用户锁定 — 最小可行实现规格

## 文档信息

- **创建日期**: 2025-09-13
- **版本**: v1.0
- **状态**: 已实现
- **实现位置**: `LYBT.Module.Auth/Services/AuthBusinessService.cs`

## 功能概述

在不修改数据库表结构的前提下，基于现有User实体的安全字段实现登录重试限制和账户锁定机制。

## 核心策略

### 1. 失败计数机制

- **存储位置**: `User.FailedLoginCount` (int字段)
- **计数逻辑**: 每次密码验证失败时递增
- **重置条件**: 用户成功登录时清零

### 2. 锁定触发机制

- **触发阈值**: 连续失败次数 ≥ `AuthOptions.MaxFailedLoginAttempts` (默认5次)
- **锁定标记**: 设置 `User.LockoutEnd` 为未来时间点
- **锁定时长**: `AuthOptions.AccountLockoutDuration` (默认15分钟)
- **锁定计算**: `DateTime.UtcNow.Add(AuthOptions.AccountLockoutDuration)`

### 3. 锁定检查机制

- **检查时机**: 每次登录请求的第一步
- **检查条件**: `User.LockoutEnd.HasValue && User.LockoutEnd.Value > DateTime.UtcNow`
- **阻断行为**: 直接返回失败，不进行密码验证，不累加失败次数

### 4. 解锁机制

- **自动解锁**: 锁定期到期后自然解锁
- **成功解锁**: 登录成功时清除锁定状态 (`LockoutEnd = null`)
- **计数重置**: 同时重置 `FailedLoginCount = 0`

## 配置参数

### AuthOptions配置

```json
{
  "AuthOptions": {
    "MaxFailedLoginAttempts": 5,
    "AccountLockoutDuration": "00:15:00"
  }
}
```

### 参数说明

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `MaxFailedLoginAttempts` | int | 5 | 触发锁定的最大失败次数 |
| `AccountLockoutDuration` | TimeSpan | 15分钟 | 账户锁定持续时间 |

## 实现时序

### 登录失败时序

1. **参数验证** - 检查用户名密码非空
2. **用户查询** - 从数据库获取用户信息
3. **锁定检查** - 检查是否在锁定期内
4. **密码验证** - 验证用户密码
5. **失败处理** - 密码错误时执行：
   - 递增 `FailedLoginCount`
   - 检查是否达到阈值
   - 达到阈值时设置 `LockoutEnd`
   - 更新数据库安全字段
6. **返回失败** - 统一返回"用户名或密码错误"

### 登录成功时序

1. **参数验证** - 检查用户名密码非空
2. **用户查询** - 从数据库获取用户信息
3. **锁定检查** - 检查是否在锁定期内
4. **密码验证** - 验证用户密码成功
5. **安全重置** - 执行：
   - `FailedLoginCount = 0`
   - `LockoutEnd = null`
   - 更新数据库安全字段
6. **生成令牌** - 创建JWT令牌
7. **返回成功** - 返回令牌和用户信息

### 锁定期检查时序

1. **用户查询** - 获取用户信息
2. **时间比较** - `LockoutEnd > DateTime.UtcNow`
3. **计算剩余** - `LockoutEnd - DateTime.UtcNow`
4. **直接拒绝** - 返回锁定提示消息
5. **记录日志** - 记录锁定状态和剩余时间

## 日志策略

### 失败计数日志

```csharp
_logger.LogWarning(
    "登录失败: {Username}, 当前失败次数: {FailedCount}/{MaxAttempts}",
    user.Username, user.FailedLoginCount, _authOptions.MaxFailedLoginAttempts);
```

### 账户锁定日志

```csharp
_logger.LogWarning(
    "用户账户已锁定: {Username}, 失败次数: {FailedCount}, 锁定到期时间: {LockoutEnd}, 锁定时长: {Duration}",
    user.Username, user.FailedLoginCount, user.LockoutEnd, _authOptions.AccountLockoutDuration);
```

### 解锁成功日志

```csharp
_logger.LogInformation(
    "用户登录成功，重置失败计数: {Username}", 
    user.Username);
```

## 数据库影响

### 现有字段利用

- **User.FailedLoginCount** (int) - 失败登录计数
- **User.LockoutEnd** (DateTime?) - 锁定结束时间

### Repository方法

- `UpdateUserSecurityAsync(userId, failedCount, lockoutEnd)` - 更新安全字段
- `UpdateFailedLoginInfoAsync(userId, failedCount, lockoutEnd)` - 更新失败信息

### 无表结构变更

本实现完全基于现有User实体字段，无需创建新表或修改现有表结构。

## 安全考虑

### 信息泄露防护

- 锁定和密码错误都返回相同错误消息："用户名或密码错误"
- 避免泄露用户是否存在的信息
- 锁定提示包含剩余时间但不暴露敏感信息

### 时间安全

- 使用 `DateTime.UtcNow` 确保时区一致性
- 锁定时间存储为绝对时间点，避免相对时间计算错误

### 日志安全

- 敏感信息不记录在日志中
- 只记录用户名和状态信息
- 详细的安全事件供管理员监控

## 配置注入

### 服务注册

在 `UnifiedServiceRegistration.cs` 中已配置：

```csharp
services.AddOptions<AuthOptions>()
    .Bind(configuration.GetSection(AuthOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 服务注入

在 `AuthBusinessService` 中注入：

```csharp
public AuthBusinessService(
    // ... 其他依赖
    IOptions<AuthOptions> authOptions)
{
    _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
}
```

## 兼容性说明

### 现有数据兼容

- 现有用户的 `FailedLoginCount` 默认为0
- 现有用户的 `LockoutEnd` 默认为null
- 实现对历史数据完全兼容

### API契约保持

- 登录API接口不变：`POST /api/v1/auth/login`
- 响应格式保持一致
- 错误消息格式不变

### 前端兼容

- 前端无需修改登录逻辑
- 锁定提示通过现有错误处理机制显示
- 无需新增前端界面元素

## 测试要点

见 `lockout-tests.md` 文档中的详细测试用例。