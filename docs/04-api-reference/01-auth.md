# 认证 API

> Controller: `AuthController` | 路由前缀: `/api/v1/auth` | 默认权限: `[Authorize]`

## 概述

提供用户登录、登出、Token 验证功能。登录端点启用限流策略 `Login`。

> **实现状态**: 全部 5 个端点（login/logout/refresh/auto-login/validate）均已在 `AuthController` 实现。AccessToken 有效期 **60 分钟**（`AddMinutes(60)` 硬编码）。

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

**成功响应** (200) `ApiResponse<LoginResponse>`（仅 `data`）:

```json
{
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
}
```

**失败响应** (401) — 用户名或密码错误（原始 JSON，非 ApiResponse 信封）:

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
| 401/403/404 | — | 通用错误码见 [README](README.md#通用-http-状态码) |
| 429 | 触发限流策略 `Login` |

> 401 在本端点特指「用户名或密码错误」；400 为参数验证失败（用户名/密码为空）。

---

## POST /auth/auto-login

使用 AutoLoginToken 自动登录，支持 Token 轮换机制。

- **权限**: 匿名 (`[AllowAnonymous]`)
- **限流**: `Login` 策略
- **状态**: ✅ 已实现

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

> 通用状态码（401/403/404）见 [README](README.md#通用-http-状态码)。本端点无特有状态码。

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

**成功响应** (200) `ApiResponse`: `data: null`（消息「登出成功」）。

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

**状态码：** 始终返回 200（登出成功）。

---

## POST /auth/refresh

刷新访问令牌（滑动过期）。

- **权限**: 匿名 (`[AllowAnonymous]`)
- **状态**: ✅ 已实现

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
| 401/403/404 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

> 401 在本端点特指：Token 已被撤销 / RefreshToken 已过期 / RefreshToken 无效。

---

## GET /auth/validate

验证当前 Token 是否有效。

- **权限**: 已认证 (`[Authorize]`)
- **Token 来源**: `Authorization: Bearer {token}` 请求头

**请求参数**: 无（Token 通过请求头传递）

**成功响应** (200) `ApiResponse<object>`（仅 `data`）:

```json
{
  "valid": true,
  "sub": {
    "userId": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
    "userName": "admin",
    "role": "Admin"
  },
  "message": "Token is valid"
}
```

**失败响应** (401) — Token 无效（原始 JSON，非 ApiResponse 信封）:

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
# 验证 token（TOKEN 获取见 README）
curl -X GET http://localhost:5000/api/v1/auth/validate \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 200 | Token 验证成功 |
| 401/403/404 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

> 401 在本端点特指：缺少 Authorization 头 / Token 格式错误 / Token 无效或过期。

---

## GET /auth

基础端点，返回 405 Method Not Allowed（原始 JSON）:

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

> 响应信封格式与字段见 [README](README.md#通用响应格式)。

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，5 个端点 |
| 2026-06-25 | v2.0 | 补充全部端点的请求/响应 JSON 示例、curl 命令、错误码表；修正响应字段与源码一致 |
| 2026-06-28 | v2.1 | 文档对齐基线：删除 refresh/auto-login「尚未实现」声明（两端点已在 AuthController 实现，:116/:128）；AccessToken 有效期标注 60 分钟（代码 `AddMinutes(60)`）；错误码表已在 README 精简 |
| 2026-06-28 | v2.2 | 文档结构优化批次1：JSON 示例去 ApiResponse 外壳只留 data；错误响应 JSON 块合并到错误码表；curl 删除 TOKEN 脚本（见 README）；通用状态码引用 README |
