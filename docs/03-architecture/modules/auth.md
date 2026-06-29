# Auth 模块设计

> 日期: 2026-06-29
> US 数量: 13 (PRD)
> 复杂度: 4/10
> 状态: 草稿

## 模块概述

Auth 管理认证授权，支持远程/本地双模式。远程使用完整 JWT + Refresh Token，本地使用简化 JWT（1年有效期）。

**职责边界**:
- 用户登录/登出
- JWT Token 生成与验证
- Refresh Token 旋转与撤销
- Token 生命周期管理（Active/Warning/Expired）
- 本地模式简化认证

**依赖关系**:
- 上游: Users（用户信息、密码验证）
- 下游: 所有需要认证的模块
- 无模块间直接引用

**关键 US 清单**:
AUTH-001 ~ AUTH-013（详见 PRD）

## 接口契约

### Server 端 Service

```csharp
// 注意：AuthService 存在但被 Controller 绕过
// Controller 直接使用 UserManager/SignInManager
public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<bool> RefreshTokenAsync(RefreshTokenRequest request, out TokenPair? tokens);
    Task<bool> LogoutAsync(string userId);
    Task<bool> ValidateTokenAsync(ValidateTokenRequest request);
}
```

**⚠️ 已知问题**: AuthService（227行）注册为 Scoped，但 WebAPI/LocalWebAPI 两个 Controller **都绕过它直接用 UserManager/SignInManager** → 整个 Service 是死代码。

### API 端点映射

| HTTP | 路由 | 方法 | 权限 |
|------|------|------|------|
| POST | `/api/v1/auth/login` | Login | 匿名 |
| POST | `/api/v1/auth/refresh` | RefreshToken | 匿名 |
| POST | `/api/v1/auth/logout` | Logout | Bearer |
| POST | `/api/v1/auth/validate` | ValidateToken | 匿名 |
| GET | `/api/v1/health` | Health | 匿名 |

### DTO 结构

```
LoginRequest
├── Username: string
├── Password: string
└── RememberMe: bool

LoginResponse
├── AccessToken: string
├── RefreshToken: string
├── ExpiresAt: DateTime
├── UserId: Guid
├── UserName: string
├── RealName: string
└── Roles: List<string>

RefreshTokenRequest
├── RefreshToken: string

TokenPair
├── AccessToken: string
└── RefreshToken: string

ValidateTokenRequest
├── Token: string

ValidateTokenResponse
├── IsValid: bool
├── UserId: Guid?
├── UserName: string?
└── Roles: List<string>?
```

## 认证流程

### 远程模式
```
Desktop → POST /api/v1/auth/login
Controller → UserManager.FindByNameAsync
  → SignInManager.CheckPasswordSignInAsync（失败计数、锁定）
  → 生成 JWT AccessToken（30min）
  → 生成 RefreshToken（7天）
  → 返回 LoginResponse

后续请求:
  → Authorization: Bearer <AccessToken>
  → TokenRefreshHandler 自动刷新（过期前）
  → Refresh: POST /api/v1/auth/refresh
```

### 本地模式
```
Desktop → POST /api/v1/auth/login（嵌入式 LocalWebAPI）
Controller → UserManager.FindByNameAsync
  → 简化密码验证（无锁定/2FA）
  → 生成 JWT AccessToken（1年有效期）
  → 返回 LoginResponse（无 RefreshToken）

⚠️ 本地模式安全风险:
  - Token 有效期 1 年，无撤销机制
  - /refresh 接受任意 JWT 无签名校验
```

## Token 生命周期

```
┌─────────────┐     过期前5min      ┌─────────────┐
│   Active    │ ──────────────────▶ │  Warning    │
│  (活跃)     │                     │  (即将过期)  │
└──────┬──────┘                     └──────┬──────┘
       │                                   │
       │ 刷新成功                          │ 过期
       ▼                                   ▼
┌─────────────┐                     ┌─────────────┐
│   Active    │                     │   Expired   │
│  (重置)     │                     │  (已过期)   │
└─────────────┘                     └─────────────┘
```

**Desktop 实现**:
- `TokenLifecycleService`: 定时器监控 Token 状态
- `TokenRefreshHandler`: DelegatingHandler，自动重试刷新
- `TokenManager`: 线程安全的内存 Token 存储
- `TokenStorageService`: Token 持久化（仅内存，进程退出清除）

## 数据流

### 登录
```
Desktop LoginViewModel
  → ILoginCoordinator.LoginAsync
    → IAuthenticationService.LoginAsync
      → POST /api/v1/auth/login
    → ITokenStorageService.SaveAsync（缓存 LoginResponse）
    → ITokenManager.SetToken（设置 AccessToken）
    → IAuthenticationStateMachine.RaiseEvent(LoginSuccess)
  → 导航到主界面
```

### Token 刷新
```
TokenRefreshHandler.SendAsync（每次 HTTP 请求）
  → 检查 Token 是否即将过期（<5min）
  → 是 → POST /api/v1/auth/refresh
    → 成功 → 更新 TokenManager + TokenStorageService
    → 失败 → 触发 TokenRefreshFailureEvent
      → 重新登录或提示用户
  → 否 → 正常发送请求
```

### 登出
```
Desktop → POST /api/v1/auth/logout
  → TokenManager.ClearToken
  → TokenStorageService.ClearAsync
  → IAuthenticationStateMachine.RaiseEvent(Logout)
  → 导航到登录页
```

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| AuthenticationException | 用户名/密码错误 | 返回 401 + 错误消息 |
| AccountLockedException | 账户锁定（5次失败/15分钟） | 返回 403 + 锁定剩余时间 |
| TokenExpiredException | Token 已过期 | 返回 401，触发重新登录 |
| NetworkException | 网络不可用 | 本地模式降级或提示用户 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| 密码策略 | BCrypt（work factor 11），自动重哈希 | AUTH-003 |
| 账户锁定 | 5次失败锁定15分钟 | AUTH-004 |
| Token 有效期 | AccessToken 30min，RefreshToken 7天 | AUTH-005 |
| 本地 Token | 1年有效期，无刷新 | AUTH-010 |
| Token 族旋转 | Refresh Token 使用后废弃旧 Token | AUTH-006 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| Users | 查询用户信息、密码验证 | Auth → Users |
| 所有模块 | 提供认证上下文 | Auth → 全局 |

**已知问题**:
1. AuthService 是死代码（Controller 直接用 UserManager）
2. 本地模式 /refresh 无签名校验
3. Token 族旋转（D3 B+）待实现
4. 审计日志缺失（登录/登出无记录）
