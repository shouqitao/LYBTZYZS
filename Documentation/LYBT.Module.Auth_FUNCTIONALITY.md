# LYBT.Module.Auth 功能说明文档

## 模块概述

认证模块负责系统的用户身份验证和授权管理，包括用户登录验证、JWT令牌生成、登录安全防护、系统管理员密码管理等核心安全功能。本模块采用基于JWT的无状态认证方案，支持多种登录类型和智能防护机制。

## 数据模型

### 依赖模型 (来自Users模块)

#### UserModel (用户实体)

**文件位置**: `LYBT.Module.Users/Models/UserModel.cs`

认证模块依赖于用户模块的UserModel，关键字段：

| 字段名              | 类型                   | 说明       | 认证相关用途      |
| ---------------- | -------------------- | -------- | ----------- |
| Id               | Guid                 | 用户唯一标识   | 日志记录和令牌生成   |
| UserName         | string               | 用户名（唯一）  | 登录凭据验证      |
| RealName         | string               | 真实姓名     | 日志记录和用户信息返回 |
| Roles            | List&lt;UserRole&gt; | 用户角色列表   | 权限控制和令牌生成   |
| IsActive         | bool                 | 启用状态     | 登录权限验证      |
| PasswordHash     | string               | 密码哈希值    | 密码验证        |
| FailedLoginCount | int                  | 连续登录失败次数 | 账户锁定策略      |
| LockoutEnd       | DateTime?            | 账号锁定截止时间 | 登录安全防护      |
| LastLoginTime    | DateTime?            | 最近登录时间   | 登录状态跟踪      |

#### AdminSecretModel (系统管理员密码)

**文件位置**: `LYBT.Module.Users/Models/AdminSecretModel.cs`

| 字段名          | 类型     | 说明     | 认证相关用途       |
| ------------ | ------ | ------ | ------------ |
| UserName     | string | 管理员用户名 | sysadmin密码管理 |
| PasswordHash | string | 密码哈希值  | 系统管理员认证      |

## DTO 数据传输对象

### LoginRequestDto (登录请求)

**使用场景**: 用户登录验证请求
**特点**: 包含登录凭据和客户端信息，用于安全审计

```csharp
- Username: 用户名（必填，最长32字符）
- Password: 密码（必填）
- ClientIp: 客户端IP地址（可选，用于安全日志）
- UserAgent: 用户代理信息（可选，用于安全审计）
- LoginType: 登录类型（默认"Password"，支持扩展）
```

### LoginResponseDto (登录响应)

**使用场景**: 登录成功后返回给客户端
**特点**: 包含JWT令牌和用户基本信息

```csharp
- Token: JWT令牌（用于后续请求认证）
- User: 用户信息（UserDto类型，包含用户基本资料）
```

### LogoutRequestDto (登出请求)

**使用场景**: 用户主动登出
**特点**: 用于记录登出日志和会话管理

```csharp
- Username: 用户名（必填，用于日志记录）
```

### ChangeSysAdminPasswordDto (系统管理员密码修改)

**使用场景**: 系统管理员修改自己的密码
**特点**: 需要验证原密码，确保安全性

```csharp
- OldPassword: 原密码（必填，用于身份验证）
- NewPassword: 新密码（必填）
```

## 服务层 (IAuthService & AuthService)

### 核心认证方法

#### LoginAsync

```csharp
Task<UserDto?> LoginAsync(LoginRequestDto dto)
```

**功能**: 用户登录验证和安全防护
**业务逻辑**: 

- 登录类型验证（支持Password等类型）
- 用户存在性和启用状态检查
- 账户锁定状态检查
- 密码验证（支持普通用户和sysadmin）
- 登录失败计数和账户锁定机制
- 登录成功后重置失败计数和更新登录时间
- 详细的登录日志记录

**安全特性**:

- 连续失败登录保护
- 账户自动锁定机制
- IP地址和UserAgent记录
- 异常处理和日志记录

**使用场景**: 系统登录页面、API认证接口

#### LogoutAsync

```csharp
Task<bool> LogoutAsync(LogoutRequestDto dto)
```

**功能**: 用户登出处理
**业务逻辑**: 

- 记录用户登出日志
- 会话状态清理（客户端处理）

**使用场景**: 用户主动登出、会话超时处理

#### ChangeSysAdminPasswordAsync

```csharp
Task<bool> ChangeSysAdminPasswordAsync(ChangeSysAdminPasswordDto dto)
```

**功能**: 系统管理员密码修改
**业务逻辑**: 

- 原密码验证
- 新密码哈希生成
- 数据库密码更新
- 操作日志记录

**安全特性**:

- 原密码强制验证
- 密码哈希存储
- 操作审计日志

**使用场景**: 系统管理员密码管理

### 内部辅助方法

#### ValidateLoginType

- 验证登录类型是否支持
- 支持多种登录方式扩展

#### GetUserForAuthentication

- 统一用户获取逻辑
- 支持普通用户和sysadmin区分处理

#### CheckAccountLockout

- 账户锁定状态检查
- 锁定时间计算和提示

#### ValidatePasswordAsync

- 密码验证逻辑
- 支持普通用户和sysadmin密码验证

#### HandleFailedLoginAsync

- 登录失败处理
- 失败计数和锁定逻辑

#### HandleSuccessfulLoginAsync

- 登录成功处理
- 状态重置和时间更新

## 仓储层 (IAuthRepository & AuthRepository)

### 用户查询方法

#### GetByUsernameAsync

```csharp
Task<UserModel?> GetByUsernameAsync(string userName)
```

**功能**: 根据用户名获取用户信息
**特点**: 包括禁用用户，用于登录验证
**使用场景**: 登录验证、用户查找

### 登录状态管理

#### UpdateLastLoginTimeAsync

```csharp
Task UpdateLastLoginTimeAsync(Guid id, DateTime loginTime)
```

**功能**: 更新用户最后登录时间
**使用场景**: 登录成功后状态更新

#### UpdateUserLoginProtectionAsync

```csharp
Task UpdateUserLoginProtectionAsync(UserModel user)
```

**功能**: 更新登录防护信息
**包含字段**: 

- 登录失败次数
- 账户锁定时间
  **使用场景**: 登录失败处理、账户解锁

### 系统管理员密码管理

#### GetAdminPasswordHashAsync

```csharp
Task<string?> GetAdminPasswordHashAsync(string userName)
```

**功能**: 获取系统管理员密码哈希
**使用场景**: sysadmin登录验证

#### UpdateAdminPasswordHashAsync

```csharp
Task UpdateAdminPasswordHashAsync(string userName, string passwordHash)
```

**功能**: 更新系统管理员密码哈希
**使用场景**: sysadmin密码修改

## 权限控制策略

### 登录权限

- **启用用户**: 只有IsActive为true的用户才能登录
- **非锁定账户**: 锁定期间的账户无法登录
- **系统管理员**: sysadmin用户不受锁定限制

### 安全防护

- **失败计数**: 记录连续登录失败次数
- **自动锁定**: 超过最大失败次数自动锁定账户
- **锁定时间**: 可配置的账户锁定持续时间
- **IP记录**: 记录登录IP地址用于安全审计

### 密码安全

- **哈希存储**: 所有密码均以哈希形式存储
- **分离管理**: sysadmin密码独立存储和管理
- **验证机制**: 原密码验证确保安全性

## 配置选项 (AuthOptions)

### 安全配置

- `MaxFailedLoginAttempts`: 最大登录失败次数（默认5次）
- `AccountLockoutDuration`: 账户锁定持续时间（默认30分钟）
- `SupportedLoginTypes`: 支持的登录类型列表
- `EnableDetailedLoginLogging`: 是否启用详细登录日志

### 令牌配置

- JWT令牌生成和验证配置
- 令牌有效期设置
- 签名密钥管理

## 日志审计

### 登录日志

所有登录相关操作都会记录详细日志：

- **登录成功**: 用户信息、IP地址、UserAgent
- **登录失败**: 失败原因、IP地址、失败次数
- **账户锁定**: 锁定原因、锁定时间
- **登录异常**: 异常信息、客户端信息

### 系统管理日志

- **密码修改**: sysadmin密码修改记录
- **登出操作**: 用户登出时间记录

### 日志内容

- 操作时间和操作者
- 操作类型和结果
- 客户端信息（IP、UserAgent）
- 详细的错误或成功信息

## 集成依赖

### 基础设施依赖

- **IUnifiedLogService**: 统一日志服务
- **IJwtAuthenticationService**: JWT令牌服务
- **PasswordHelper**: 密码哈希工具
- **SysAdminHandler**: 系统管理员处理器

### 模块依赖

- **LYBT.Module.Users**: 用户模块（用户信息和密码管理）
- **LYBT.Infrastructure**: 基础设施（日志、认证、配置）

## 使用示例

### 用户登录

```csharp
var loginRequest = new LoginRequestDto {
    Username = "doctor001",
    Password = "password123",
    ClientIp = "192.168.1.100",
    UserAgent = "Mozilla/5.0...",
    LoginType = "Password"
};

var user = await authService.LoginAsync(loginRequest);
if (user != null) {
    // 登录成功，生成JWT令牌
    var token = jwtService.GenerateToken(user);
    var response = new LoginResponseDto {
        Token = token,
        User = user
    };
    return response;
}
```

### 用户登出

```csharp
var logoutRequest = new LogoutRequestDto {
    Username = "doctor001"
};

await authService.LogoutAsync(logoutRequest);
```

### 系统管理员密码修改

```csharp
var changePasswordDto = new ChangeSysAdminPasswordDto {
    OldPassword = "oldPassword123",
    NewPassword = "newPassword456"
};

var success = await authService.ChangeSysAdminPasswordAsync(changePasswordDto);
```

### 登录状态检查

```csharp
// 检查JWT令牌有效性
var principal = jwtService.ValidateToken(token);
if (principal != null) {
    var userId = principal.FindFirst("userId")?.Value;
    var userRole = principal.FindFirst("role")?.Value;
    // 用户已认证，继续业务逻辑
}
```

## 安全建议

### 密码策略

- 定期提醒用户修改密码
- 强密码复杂度要求
- 禁止重复使用历史密码

### 会话管理

- JWT令牌合理的过期时间
- 支持令牌刷新机制
- 重要操作需要重新验证

### 监控告警

- 异常登录行为监控
- 批量登录失败告警
- 系统管理员操作审计