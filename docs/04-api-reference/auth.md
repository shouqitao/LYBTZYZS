# 认证 API

> Controller: `AuthController` | 路由前缀: `/api/v1/auth` | 默认权限: `[Authorize]`

## 概述

提供用户登录、自动登录、登出、Token 刷新和验证功能。登录端点启用限流策略 `Login`。

---

## POST /auth/login

用户登录，获取 JWT 访问令牌。

- **权限**: 匿名 (`[AllowAnonymous]`)
- **限流**: `Login` 策略

**请求体**:

```json
{
  "userName": "string",   // 必填，用户名
  "password": "string"    // 必填，密码
}
```

**成功响应** (200) `LoginResponse`:

```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJ...",                       // JWT 访问令牌
    "user": {                                // 嵌套用户详情
      "id": "guid",
      "username": "string",
      "realName": "string",
      "role": "Doctor|Admin|SuperAdmin|Receptionist",
      "status": "Enabled",
      "isEnabled": true
    },
    "refreshToken": "string",                // 刷新令牌
    "expiresAt": "2026-01-01T00:00:00Z",     // 过期时间
    "autoLoginToken": "string",              // 自动登录令牌
    "mustChangePassword": false              // 是否需要修改密码
  }
}
```

**错误响应** (401):

```json
{
  "success": false,
  "message": "用户名或密码错误",
  "errors": { "code": "InvalidCredentials", "numericCode": 101 }
}
```

| 错误码 | 说明 |
|--------|------|
| InvalidCredentials | 用户名或密码错误 |
| UserNotFound | 用户不存在 |
| UserDisabled | 用户已禁用 |

---

## POST /auth/auto-login

使用 AutoLoginToken 自动登录，支持 Token 轮换机制。

- **权限**: 匿名 (`[AllowAnonymous]`)
- **限流**: `Login` 策略

**请求体**:

```json
{
  "userName": "string",        // 必填，用户名
  "autoLoginToken": "string"   // 必填，AutoLoginToken
}
```

**成功响应** (200): `LoginResponse` (同 `/auth/login` 响应格式)。

**安全说明**:
- AutoLoginToken 可被服务端随时撤销
- 成功登录后返回新的 AutoLoginToken (Token 轮换)
- 不暴露用户密码

---

## POST /auth/logout

用户登出，清理服务端会话。

- **权限**: 匿名 (`[AllowAnonymous]`) -- 允许过期 Token 访问
- **业务规则**: Logout 后必须重新登录，不支持会话恢复

**请求体**:

```json
{
  "refreshToken": "string",   // 可选，用于精确定位会话
  "userName": "string"        // 可选，用于审计日志
}
```

必须提供 `refreshToken` 或 `userName` 中的至少一个。

**成功响应** (200):

```json
{
  "success": true,
  "message": "登出成功"
}
```

---

## POST /auth/refresh

刷新访问令牌 (滑动过期)。

- **权限**: 匿名 (`[AllowAnonymous]`)

**请求体**:

```json
{
  "refreshToken": "string"   // 必填，刷新令牌
}
```

**成功响应** (200): `LoginResponse` (同 `/auth/login` 响应格式)。

**错误响应** (401):

| 错误码 | 说明 |
|--------|------|
| TokenRevoked | Token 已被撤销 |
| RefreshTokenExpired | RefreshToken 已过期 |
| RefreshTokenInvalid | RefreshToken 无效 |

---

## GET /auth/validate

验证当前 Token 是否有效。

- **权限**: 已认证 (`[Authorize]`)
- **Token 来源**: `Authorization: Bearer {token}` 请求头

**成功响应** (200):

```json
{
  "success": true,
  "message": "Token验证成功",
  "data": {
    "valid": true,
    "sub": "session-info",
    "message": "Token is valid"
  }
}
```

**错误响应** (401):

```json
{
  "valid": false,
  "message": "Token is invalid",
  "errorCode": "TokenInvalid"
}
```

---

## GET /auth

基础端点，返回 405 Method Not Allowed。

```json
{
  "message": "Method Not Allowed - Use POST endpoints for authentication"
}
```

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，5 个端点 |
