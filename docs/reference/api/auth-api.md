# Auth API 参考文档

**文档版本**:  v1
**基础路径**: `/api/v1/auth`
**认证方式**: Bearer Token (JWT) / AllowAnonymous
**最后更新**: 2025-11-07（Token认证安全重构）
**Issue来源**: #1861 - Token认证安全重构

---

## 📋 目录

- [概述](#概述)
- [API端点](#api端点)
  - [POST /api/v1/auth/login](#1-post-apiv1authlogin---用户登录)
  - [POST /api/v1/auth/refresh](#2-post-apiv1authrefresh---刷新token)
  - [POST /api/v1/auth/logout](#3-post-apiv1authlogout---用户登出)
  - [GET /api/v1/auth/validate](#4-get-apiv1authvalidate---验证token从header)
- [通用响应格式](#通用响应格式)
- [错误码说明](#错误码说明)
- [安全特性](#安全特性)

---

## 概述

### 功能说明

Auth API提供完整的用户认证和Token管理功能，支持：
- 用户登录认证（普通用户 + 超级管理员）
- RefreshToken轮换机制（安全刷新AccessToken）
- Token主动撤销（登出和安全事件响应）
- Token验证（Server状态检查场景）
- 完整安全审计日志

### 核心业务规则

- **Auth-001**: 超级管理员和普通用户使用统一Token策略（15分钟AccessToken，7天RefreshToken）
- **Auth-002**: Client端使用JWT自验证，无需调用Server API验证（性能提升10-20倍）
- **Auth-003**: 支持RefreshToken主动撤销，撤销后立即生效（< 1秒）
- **Auth-004**: 所有认证事件记录安全审计日志（30天保留）

---

## API端点

### 1. POST /api/v1/auth/login - 用户登录

**描述**: 用户登录认证，支持普通用户和超级管理员登录

**路由**: `POST /api/v1/auth/login`

**认证**: AllowAnonymous

**限流**: 5次/分钟（外部）, 20次/分钟（内网）

#### 请求体

```json
{
  "userName": "doctor",
  "password": "Lybt2025@TempPass!"
}
```

**字段说明**:

| 字段 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| userName | string | ✅ | 用户名（支持普通用户和超级管理员） |
| password | string | ✅ | 密码 |

#### 成功响应 (200 OK)

```json
{
  "isSuccess": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": "12345678-1234-1234-1234-123456789abc",
      "userName": "doctor",
      "realName": "张医生",
      "role": "Doctor",
      "email": "doctor@example.com"
    },
    "refreshToken": "",
    "expiresAt": "2025-11-05T20:00:00Z"
  },
  "message": "登录成功"
}
```

**超级管理员登录响应**:
```json
{
  "isSuccess": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": "00000000-0000-0000-0000-000000000000",
      "userName": "sysadmin",
      "realName": "系统超级管理员",
      "role": "Admin",
      "email": "admin@lybt.com"
    },
    "refreshToken": "",
    "expiresAt": "2025-11-05T20:00:00Z"
  },
  "message": "登录成功"
}
```

#### 失败响应 (400 Bad Request)

```json
{
  "isSuccess": false,
  "data": null,
  "message": "用户名或密码错误"
}
```

#### 实现细节

- **控制器**: `AuthController.LoginAsync`
- **服务**: `IAuthService.LoginAsync`
- **认证流程**:
  1. 优先检查是否为超级管理员（配置UserName + AdminSecrets表密码哈希）
  2. 普通用户通过Users表+BCrypt验证
  3. 生成JWT Token（8小时有效期）
- **超级管理员**: UserId为全0 GUID，IsSuperAdmin=true claim

---

### 2. POST /api/v1/auth/refresh - 刷新Token

**描述**: 使用RefreshToken获取新的AccessToken和RefreshToken对（Token轮换机制）

**路由**: `POST /api/v1/auth/refresh`

**认证**: AllowAnonymous（通过RefreshToken验证）

**使用场景**: AccessToken过期时自动刷新

#### 请求体

```json
{
  "refreshToken": "abc123...xyz"
}
```

**字段说明**:

| 字段 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| refreshToken | string | ✅ | 有效的RefreshToken |

#### 成功响应 (200 OK)

```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "new_refresh_token_xyz",
    "user": {
      "id": "12345678-1234-1234-1234-123456789abc",
      "userName": "doctor",
      "realName": "张医生",
      "role": "Doctor",
      "email": "doctor@example.com"
    },
    "expiresAt": "2025-11-07T15:30:00Z"
  },
  "message": "Token刷新成功"
}
```

#### 失败响应 - RefreshToken已撤销 (401 Unauthorized)

```json
{
  "success": false,
  "message": "RefreshToken已撤销，请重新登录"
}
```

#### 失败响应 - RefreshToken过期 (401 Unauthorized)

```json
{
  "success": false,
  "message": "RefreshToken已过期，请重新登录"
}
```

#### 实现细节

- **控制器**: `AuthController.RefreshTokenAsync`
- **服务**: `IAuthService.RefreshTokenAsync`
- **刷新流程**:
  1. 验证RefreshToken格式和存在性
  2. 检查数据库中Token状态（是否撤销、是否过期）
  3. 撤销旧RefreshToken（设置IsRevoked=true, RevokedReason="已被新Token替换"）
  4. 生成新的AccessToken（15分钟）和RefreshToken（7天）
  5. 记录审计日志（EventType: RefreshToken）
  6. 返回新Token对

**安全特性**:
- ✅ **Token轮换**: 每次刷新撤销旧RefreshToken
- ✅ **撤销检查**: 立即生效，< 1秒响应
- ✅ **审计日志**: 完整记录刷新事件
- ✅ **链式撤销**: 如果检测到旧Token被重用，撤销整个Token家族

---

### 3. POST /api/v1/auth/logout - 用户登出

**描述**: 用户登出并撤销RefreshToken

**路由**: `POST /api/v1/auth/logout`

**认证**: 需要Bearer Token

#### 请求体

```json
{
  "username": "doctor"
}
```

**字段说明**:

| 字段 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| username | string | ✅ | 用户名 |

#### 成功响应 (200 OK)

```json
{
  "success": true,
  "message": "登出成功"
}
```

#### 实现细节

- **控制器**: `AuthController.LogoutAsync`
- **服务**: `IAuthService.LogoutAsync`
- **登出流程**:
  1. 撤销用户所有有效的RefreshToken
  2. 记录审计日志（EventType: Logout）
  3. Client端需清除本地Token

---

### 4. GET /api/v1/auth/validate - 验证Token（从Header）

**描述**: 从Authorization Header验证Token（用于需要Server状态检查的场景）

**路由**: `GET /api/v1/auth/validate`

**认证**: AllowAnonymous

**请求头**:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### 成功响应 - Token有效 (200 OK)

```json
{
  "valid": true,
  "sub": {
    "userId": "123",
    "userName": "doctor",
    "role": "Doctor"
  },
  "message": "Token is valid"
}
```

#### 失败响应 (401 Unauthorized)

```json
{
  "valid": false,
  "message": "Missing Authorization header"
}
```

#### 实现细节

- **控制器**: `AuthController.ValidateTokenAsync` (Line 160-218)
- **服务**: `IAuthService.ValidateTokenAsync` + `IAuthService.GetSessionInfoAsync`
- **验证流程**:
  1. 检查Authorization Header存在性
  2. 验证Bearer格式
  3. 提取Token并调用验证服务
  4. 返回会话信息

---

## 通用响应格式

所有API响应遵循统一格式：

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}
```

---

## 错误码说明

### HTTP状态码

| 状态码 | 说明 | 场景 |
|-------|------|------|
| 200 OK | 请求成功 | 登录成功、刷新成功、登出成功 |
| 400 Bad Request | 请求参数错误 | 用户名/密码为空，RefreshToken格式错误 |
| 401 Unauthorized | 未授权 | RefreshToken已撤销或过期 |
| 429 Too Many Requests | 限流 | 超过登录频率限制（5次/分钟） |
| 500 Internal Server Error | 服务器错误 | 认证服务异常 |

### 业务错误消息

| 消息 | 场景 | 解决方案 |
|-----|------|---------|
| "用户名或密码错误" | 登录凭据无效 | 检查用户名和密码 |
| "RefreshToken已撤销，请重新登录" | Token已被撤销 | 重新登录 |
| "RefreshToken已过期，请重新登录" | Token过期（7天） | 重新登录 |
| "Token无效或已过期" | AccessToken验证失败 | 使用RefreshToken刷新 |

---

## 安全特性

### 1. Token加密存储（Client端）
- 使用Windows DPAPI加密本地Token
- 文件路径：`%LOCALAPPDATA%\LYBTZYZS\tokens.dat`
- 只有当前Windows用户可以解密

### 2. JWT本地验证（Client端）
- Client端直接验证JWT签名和Claims
- 无需每次调用Server API
- 性能提升10-20倍（~50-100ms → ~5ms）

### 3. RefreshToken撤销机制（Server端）
- 支持单个Token撤销或用户所有Token撤销
- 撤销后立即生效（< 1秒）
- Token轮换：每次刷新撤销旧Token

### 4. 安全审计日志（Server端）
- 记录所有认证事件（Login, Logout, RefreshToken, TokenRevoked）
- IP地址脱敏（192.168.1.100 → 192.168.1.*）
- UserAgent截断（最大500字符）
- 日志保留30天自动清理

### 5. 统一Token策略
- AccessToken: 15分钟有效期
- RefreshToken: 7天有效期
- 超级管理员和普通用户使用相同策略

---

## 相关文件

### Controller
- `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`

### Service
- `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs`

### Entities
- `src/Server/Entities/LYBT.Entities/Auth/RefreshToken.cs` - RefreshToken实体
- `src/Server/Entities/LYBT.Entities/Auth/SecurityAuditLog.cs` - 安全审计日志实体

### DTO
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/LoginRequest.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/LoginResponse.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/RefreshTokenRequest.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/LogoutRequest.cs`

### Client端
- `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation.Auth/Services/SecureTokenStorage.cs` - DPAPI加密存储
- `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation.Auth/Validators/LocalTokenValidator.cs` - JWT本地验证

### 配置
- `src/Server/Services/LYBT.WebAPI/appsettings.json`
  - `Lybt:Jwt:*` - JWT配置（Secret, Issuer, Audience, AccessTokenExpireMinutes, RefreshTokenExpireDays）
  - `Lybt:SystemAdmin:UserName` - 超级管理员用户名
  - `Lybt:Security:RateLimiting:LoginLimit` - 登录限流配置

---

## 测试建议

### 单元测试

```csharp
[Fact]
public async Task RefreshToken_ValidToken_ReturnsNewTokenPair()
{
    // Arrange
    var request = new RefreshTokenRequest { RefreshToken = "valid_refresh_token" };

    // Act
    var result = await _controller.RefreshTokenAsync(request);

    // Assert
    var okResult = Assert.IsType<ActionResult<ApiResponse<LoginResponse>>>(result);
    Assert.True(okResult.Value.Success);
    Assert.NotNull(okResult.Value.Data.Token);
    Assert.NotNull(okResult.Value.Data.RefreshToken);
}

[Fact]
public async Task RefreshToken_RevokedToken_Returns401()
{
    // Arrange
    var request = new RefreshTokenRequest { RefreshToken = "revoked_token" };

    // Act
    var result = await _controller.RefreshTokenAsync(request);

    // Assert
    var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
}
```

### 集成测试

```bash
# 登录获取Token
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"doctor","password":"Lybt2025@TempPass!"}'

# 刷新Token
curl -X POST http://localhost:5000/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"abc123..."}'

# 登出
curl -X POST http://localhost:5000/api/v1/auth/logout \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -d '{"username":"doctor"}'

# 验证Token（从Header）
curl -X GET http://localhost:5000/api/v1/auth/validate \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

**最后更新**: 2025-11-07（Issue #1861 - Token认证安全重构）
**相关Issue**: #1861 (Token认证安全重构), #1838 (JWT Token自动刷新)
