# 认证 API

> Controller: `AuthController` | 路由前缀: `/api/v1/auth` | 默认权限: `[Authorize]`

## 概述

提供用户登录、登出、Token 验证功能。登录端点启用限流策略 `Login`。

> **注意**: AutoLoginToken 登录和 RefreshToken 刷新功能已设计 DTO（`AutoLoginRequest`、`RefreshTokenRequest`），但服务端 Controller 尚未实现对应端点。

---

## POST /auth/login

用户登录，获取 JWT 访问令牌。

- **权限**: 匿名 (`[AllowAnonymous]`)
- **限流**: `Login` 策略

**请求体** `LoginRequest`:

```json
{
  "userName": "admin",
  "password": "Admin@123456"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `userName` | string | 是 | 用户名，最大 32 字符 |
| `password` | string | 是 | 密码 |
| `clientIp` | string | 否 | 客户端 IP |
| `userAgent` | string | 否 | User-Agent |
| `loginType` | string | 否 | 登录类型，默认 `"Password"` |
| `rememberMe` | bool | 否 | 记住我，默认 `false` |
| `deviceId` | string | 否 | 设备 ID |
| `deviceName` | string | 否 | 设备名称 |

**成功响应** (200) `ApiResponse<LoginResponse>`:

```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
      "username": "admin",
      "realName": "管理员",
      "role": "Admin",
      "status": "Enabled",
      "isEnabled": true,
      "phoneNumber": null,
      "email": null,
      "lastLoginTime": "2026-06-25T10:00:00Z",
      "failedLoginCount": 0,
      "createdAt": "2026-01-01T00:00:00Z",
      "updatedAt": null
    },
    "refreshToken": "dGhpcyBpcyBhIHJlZnJl...",
    "expiresAt": "2026-06-25T11:00:00Z",
    "autoLoginToken": null,
    "mustChangePassword": false
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V..."
}
```

**失败响应** (401) — 用户名或密码错误:

```json
{
  "message": "用户名或密码错误"
}
```

> 注意：登录失败时返回原始 JSON 对象，不使用 `ApiResponse` 信封。

**curl 示例：**

```bash
# 登录
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}'

# sysadmin 登录
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"sysadmin","password":"SysAdmin@2026!"}'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 参数验证失败（用户名/密码为空） |
| 401 | 用户名或密码错误 |
| 429 | 触发限流策略 `Login` |

---

## POST /auth/auto-login

使用 AutoLoginToken 自动登录，支持 Token 轮换机制。

- **权限**: 匿名 (`[AllowAnonymous]`)
- **限流**: `Login` 策略
- **状态**: DTO 已设计，服务端 Controller 尚未实现

**请求体** `AutoLoginRequest`:

```json
{
  "userName": "admin",
  "autoLoginToken": "aW50ZXJuYWx0b2tlbg..."
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `userName` | string | 是 | 用户名，最大 32 字符 |
| `autoLoginToken` | string | 是 | AutoLoginToken（首次登录返回） |
| `clientIp` | string | 否 | 客户端 IP |
| `userAgent` | string | 否 | User-Agent |
| `deviceId` | string | 否 | 设备 ID |
| `deviceName` | string | 否 | 设备名称 |

**成功响应** (200): 与 `/auth/login` 格式相同，`autoLoginToken` 字段为新值（Token 轮换）。

**安全说明：**
- AutoLoginToken 可被服务端随时撤销
- 成功登录后返回新的 AutoLoginToken（Token 轮换）
- 不暴露用户密码

**curl 示例：**

```bash
curl -X POST http://localhost:5000/api/v1/auth/auto-login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","autoLoginToken":"aW50ZXJuYWx0b2tlbg..."}'
```

---

## POST /auth/logout

用户登出，清理服务端会话。

- **权限**: 匿名 (`[AllowAnonymous]`) -- 允许过期 Token 访问
- **业务规则**: Logout 后必须重新登录，不支持会话恢复

**请求体** `LogoutRequest`:

```json
{
  "userName": "admin",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJl..."
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `userName` | string | 否 | 用户名，用于审计日志 |
| `refreshToken` | string | 否 | 刷新令牌，用于精确撤销会话 |
| `deviceId` | string | 否 | 设备 ID，用于撤销特定设备 |

必须提供 `refreshToken` 或 `userName` 中的至少一个。

**成功响应** (200) `ApiResponse`:

```json
{
  "success": true,
  "message": "登出成功",
  "data": null,
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V..."
}
```

**curl 示例：**

```bash
# 通过 userName 登出
curl -X POST http://localhost:5000/api/v1/auth/logout \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin"}'

# 通过 refreshToken 登出
curl -X POST http://localhost:5000/api/v1/auth/logout \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"dGhpcyBpcyBhIHJlZnJl..."}'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 200 | 登出成功（始终返回 200） |

---

## POST /auth/refresh

刷新访问令牌（滑动过期）。

- **权限**: 匿名 (`[AllowAnonymous]`)
- **状态**: DTO 已设计，服务端 Controller 尚未实现

**请求体** `RefreshTokenRequest`:

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJl..."
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `refreshToken` | string | 是 | 刷新令牌 |
| `deviceId` | string | 否 | 设备 ID |

**成功响应** (200): 与 `/auth/login` 格式相同，返回新的 Token 和 RefreshToken。

**curl 示例：**

```bash
curl -X POST http://localhost:5000/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"dGhpcyBpcyBhIHJlZnJl..."}'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 401 | Token 已被撤销 / RefreshToken 已过期 / RefreshToken 无效 |

---

## GET /auth/validate

验证当前 Token 是否有效。

- **权限**: 已认证 (`[Authorize]`)
- **Token 来源**: `Authorization: Bearer {token}` 请求头

**请求参数**: 无（Token 通过请求头传递）

**成功响应** (200) `ApiResponse<object>`:

```json
{
  "success": true,
  "message": "Token验证成功",
  "data": {
    "valid": true,
    "sub": {
      "userId": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
      "userName": "admin",
      "role": "Admin"
    },
    "message": "Token is valid"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V..."
}
```

**失败响应** (401) — Token 无效:

```json
{
  "valid": false,
  "message": "Token is invalid",
  "errorCode": "ERR-10202"
}
```

> 注意：验证失败时返回原始 JSON 对象，不使用 `ApiResponse` 信封。

**curl 示例：**

```bash
# 先登录获取 token
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}' | jq -r '.data.token')

# 验证 token
curl -X GET http://localhost:5000/api/v1/auth/validate \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 200 | Token 验证成功 |
| 401 | 缺少 Authorization 头 / Token 格式错误 / Token 无效或过期 |

---

## GET /auth

基础端点，返回 405 Method Not Allowed。

```json
{
  "message": "Method Not Allowed - Use POST endpoints for authentication"
}
```

**curl 示例：**

```bash
curl -X GET http://localhost:5000/api/v1/auth
```

---

## 响应格式说明

所有成功响应使用统一的 `ApiResponse<T>` 信封：

```json
{
  "success": true,
  "message": "操作消息",
  "data": { ... },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V..."
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `success` | bool | 是否成功 |
| `message` | string | 操作结果消息 |
| `data` | T? | 响应数据（泛型） |
| `errors` | object? | 错误详情 |
| `timestamp` | long | Unix 时间戳（秒） |
| `requestId` | string | 请求追踪 ID |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，5 个端点 |
| 2026-06-25 | v2.0 | 补充全部端点的请求/响应 JSON 示例、curl 命令、错误码表；修正响应字段与源码一致 |
