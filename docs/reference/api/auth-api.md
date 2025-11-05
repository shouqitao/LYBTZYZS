# Auth API 参考文档

**版本**: v1
**基础路径**: `/api/v1/auth`
**认证方式**: Bearer Token (JWT) / AllowAnonymous
**Issue来源**: #1824 - Desktop客户端启动时Token验证

---

## 📋 目录

- [概述](#概述)
- [API端点](#api端点)
  - [POST /api/v1/auth/login](#1-post-apiv1authlogin---用户登录)
  - [POST /api/v1/auth/validate](#2-post-apiv1authvalidate---验证token从请求体)
  - [GET /api/v1/auth/validate](#3-get-apiv1authvalidate---验证token从header)
- [通用响应格式](#通用响应格式)
- [错误码说明](#错误码说明)

---

## 概述

### 功能说明

Auth API提供用户认证和Token验证功能，支持：
- 用户登录认证（普通用户 + 超级管理员）
- Token有效性验证（支持从请求体和Header两种方式）
- 用户会话信息获取

### 核心业务规则

- **Auth-001**: 超级管理员认证独立于普通用户（配置文件+AdminSecrets表）
- **Auth-002**: Token验证支持两种方式（POST请求体 / GET Header）
- **Auth-003**: Token验证失败返回200状态码 + IsValid=false（而非401）

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

### 2. POST /api/v1/auth/validate - 验证Token（从请求体）

**描述**: 验证JWT Token有效性并返回详细信息（Issue #1824 - Desktop客户端启动时验证）

**路由**: `POST /api/v1/auth/validate`

**认证**: AllowAnonymous

**使用场景**: Desktop客户端启动时验证本地存储的Token

#### 请求体

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**字段说明**:

| 字段 | 类型 | 必填 | 说明 |
|-----|------|------|------|
| token | string | ✅ | 要验证的JWT Token（完整Token字符串） |

#### 成功响应 - Token有效 (200 OK)

```json
{
  "isSuccess": true,
  "data": {
    "isValid": true,
    "userId": 123,
    "username": "doctor",
    "role": "Doctor",
    "expiresAt": "2025-11-05T20:00:00Z",
    "errorMessage": null
  },
  "message": "Token验证完成"
}
```

#### 成功响应 - Token无效 (200 OK)

⚠️ **注意**: Token无效时仍返回200状态码，通过`isValid`字段区分

```json
{
  "isSuccess": true,
  "data": {
    "isValid": false,
    "userId": null,
    "username": null,
    "role": null,
    "expiresAt": null,
    "errorMessage": "Token无效或已过期"
  },
  "message": "Token验证完成"
}
```

#### 失败响应 (400 Bad Request)

```json
{
  "isSuccess": false,
  "data": null,
  "message": "Token不能为空"
}
```

#### 实现细节

- **控制器**: `AuthController.ValidateTokenFromBodyAsync` (Line 220-255)
- **服务**: `IAuthService.ValidateTokenWithDetailsAsync`
- **验证流程**:
  1. 参数验证（ModelState + Token非空）
  2. 调用`IJwtService.ValidateToken`解析JWT
  3. 提取Claims（UserId, Username, Role）
  4. 解析过期时间（jwtToken.ValidTo）
  5. 返回统一结构（无论有效/无效）

**关键特性**:
- ✅ 验证失败不抛异常，返回结构化错误信息
- ✅ 客户端可根据`isValid`字段判断是否需要重新登录
- ✅ 适用于Desktop客户端启动时的本地Token验证

---

### 3. GET /api/v1/auth/validate - 验证Token（从Header）

**描述**: 从Authorization Header验证Token（早期实现，推荐使用POST方式）

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

⚠️ **推荐使用POST /api/v1/auth/validate**: 功能更完整，错误处理更规范

---

## 通用响应格式

所有API响应遵循统一格式：

```csharp
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}
```

---

## 错误码说明

### HTTP状态码

| 状态码 | 说明 | 场景 |
|-------|------|------|
| 200 OK | 请求成功 | Token验证完成（无论有效/无效） |
| 400 Bad Request | 请求参数错误 | 用户名/密码为空，Token格式错误 |
| 401 Unauthorized | 未授权 | Token验证失败（GET方式） |
| 429 Too Many Requests | 限流 | 超过登录频率限制（5次/分钟） |
| 500 Internal Server Error | 服务器错误 | 认证服务异常 |

### 业务错误消息

| 消息 | 场景 | 解决方案 |
|-----|------|---------|
| "用户名或密码错误" | 登录凭据无效 | 检查用户名和密码 |
| "Token不能为空" | 请求体缺少Token字段 | 提供有效Token |
| "Token无效或已过期" | JWT验证失败 | 重新登录获取新Token |
| "Missing Authorization header" | Header验证缺少Authorization | 添加Bearer Token Header |

---

## 相关文件

### Controller
- `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`

### Service
- `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs`

### DTO
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/LoginRequest.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/LoginResponse.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/ValidateTokenRequest.cs` ⭐ **Issue #1824新增**
- `src/Shared/LYBT.Shared.Models/Contracts/Auth/ValidateTokenResponse.cs` ⭐ **Issue #1824新增**

### 配置
- `src/Server/Services/LYBT.WebAPI/appsettings.json`
  - `Lybt:Jwt:*` - JWT配置
  - `Lybt:SystemAdmin:UserName` - 超级管理员用户名（⚠️ Phase 3.1统一为PascalCase）
  - `Lybt:Security:RateLimiting:LoginLimit` - 登录限流配置

---

## 测试建议

### 单元测试

```csharp
[Fact]
public async Task ValidateTokenFromBody_ValidToken_ReturnsSuccess()
{
    // Arrange
    var request = new ValidateTokenRequest { Token = "valid_jwt_token" };

    // Act
    var result = await _controller.ValidateTokenFromBodyAsync(request);

    // Assert
    var okResult = Assert.IsType<ActionResult<ApiResponse<ValidateTokenResponse>>>(result);
    Assert.True(okResult.Value.Data.IsValid);
}

[Fact]
public async Task ValidateTokenFromBody_InvalidToken_ReturnsIsValidFalse()
{
    // Arrange
    var request = new ValidateTokenRequest { Token = "invalid_token" };

    // Act
    var result = await _controller.ValidateTokenFromBodyAsync(request);

    // Assert
    var okResult = Assert.IsType<ActionResult<ApiResponse<ValidateTokenResponse>>>(result);
    Assert.False(okResult.Value.Data.IsValid);
    Assert.NotNull(okResult.Value.Data.ErrorMessage);
}
```

### 集成测试

```bash
# 登录获取Token
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"doctor","password":"Lybt2025@TempPass!"}'

# 验证Token（推荐方式）
curl -X POST http://localhost:5000/api/v1/auth/validate \
  -H "Content-Type: application/json" \
  -d '{"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."}'

# 验证Token（Header方式）
curl -X GET http://localhost:5000/api/v1/auth/validate \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

**最后更新**: 2025-11-05（Issue #1829 - 文档同步）
**相关Issue**: #1824 (Desktop Token验证), #1761 (配置扁平化)
