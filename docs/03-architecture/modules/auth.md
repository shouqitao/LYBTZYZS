# Auth 模块设计

> v1.0 | 2026-06-28

## 模块概述

Auth 模块负责认证、授权、Token 生命周期管理。支持远程(JWT 30min+Refresh)和本地(简化 1年 Token)双模式。

**职责边界**：用户认证、Token 生成/验证/刷新/撤销、登录锁定/限流、安全审计

**依赖关系**：
- 上游：Users（用户数据）、SuperAdmin（独立账户）
- 下游：所有模块（通过 GetOperator() 获取操作员）

## 接口契约

### Service 接口

| 接口 | 关键方法 |
|------|----------|
| `IAuthService` | LoginAsync, LogoutAsync, RefreshTokenAsync, ValidateTokenAsync |
| `IUserService` | GetUserAsync, ValidateCredentialsAsync |
| `ITokenRevocationService` | RevokeTokenAsync, RevokeAllUserTokens (🧲 v1.0 D3 B+) |
| `ISecurityAuditService` | LogSecurityEventAsync (🧲 v1.0 D3 B+) |

### API 端点映射

| 端点 | 方法 | 权限 | 说明 |
|------|------|------|------|
| POST /auth/login | LoginAsync | 匿名 | 登录(远程/本地) |
| POST /auth/auto-login | AutoLoginAsync | 匿名 | 自动登录(本地) |
| POST /auth/logout | LogoutAsync | 已认证 | 登出+撤销Token |
| POST /auth/refresh | RefreshTokenAsync | 匿名 | Token刷新 |
| GET /auth/validate | ValidateTokenAsync | 已认证 | Token验证 |

## Token 生命周期

```
登录 → AccessToken(30min) + RefreshToken(7d滑动/30d绝对)
  ↓
AccessToken过期 → RefreshToken刷新 → 新Token对
  ↓
登出 → 撤销RefreshToken族 → 清除旧Token
```

**本地模式**：简化为 1 年长效 AccessToken，无 RefreshToken，无 Token 族撤销。

## 异常处理

| 场景 | 异常 | HTTP | 安全约束 |
|------|------|:---:|----------|
| 用户不存在 | AuthInvalidCredentials | 401 | 不区分"不存在"vs"密码错" |
| 用户已禁用 | UserDisabled | 403 | |
| 账户锁定 | UserLocked | 401 | 5次/15分钟 |
| 密码错误 | AuthInvalidCredentials | 401 | 累加 FailedCount |
| 速率限制 | RateLimitExceeded | 429 | 5次/60秒/IP |

## 业务规则

| 规则 | 约束 | US |
|------|------|-----|
| WorkFactor | BCrypt=12，不可降低 | ADR-0008 |
| Token过期 | AccessToken 30min(可配)，Refresh 7d+30d | US-AUTH-004 |
| 旧会话清理 | 新登录撤销旧Token | US-AUTH-008 |
| 安全审计 | 登录/登出/失败记录(🧲v1.0) | US-AUTH-007 |
| Token族旋转 | 每次刷新新Token(🧲v1.0) | US-AUTH-006 |
| DPAPI | Desktop Token加密存储 | US-AUTH-010 |

## 模块交互

| 模块 | 方式 | 场景 |
|------|------|------|
| Users | 查询用户 | 登录验证 |
| 所有模块 | GetOperator() | 操作员上下文传递 |
| Desktop | TokenStorage | Token 内存/DPAPI 存储 |
