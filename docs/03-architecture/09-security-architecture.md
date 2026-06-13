# 安全架构

## 1. 概述

系统采用 JWT Bearer Token 认证机制，结合基于角色的授权策略（4 种 Policy），以及 Token Family 管理实现重放攻击检测。安全架构覆盖 Server（ASP.NET Core WebAPI）和 Client（WPF Desktop）两端，确保认证、授权、Token 生命周期管理的完整性和一致性。

核心安全组件分布：

| 组件 | 位置 | 职责 |
|------|------|------|
| AuthenticationServiceCollectionExtensions | `LYBT.WebAPI/Extensions/` | JWT 认证中间件、授权策略注册 |
| JwtService | `LYBT.Module.Auth/Services/` | JWT Token 生成与验证 |
| AuthService | `LYBT.Module.Auth/Services/` | 登录/登出/凭据验证 |
| TokenManagementService | `LYBT.Module.Auth/Services/` | Token 刷新、轮换、Family 撤销 |
| SecurityAuditService | `LYBT.Module.Auth/Services/` | 安全审计日志 |
| SecurityHeadersMiddleware | `LYBT.WebAPI/Middleware/` | 安全响应头 |
| ClaimsNormalizationMiddleware | `LYBT.WebAPI/Middleware/` | Claims 格式标准化 |
| AuthenticationStateMachine | `LYBT.Desktop.Foundation/Security/` | 桌面端认证状态机 |
| TokenLifecycleService | `LYBT.Desktop.Foundation/Security/` | 桌面端 Token 生命周期管理 |
| TokenStorageService | `LYBT.Desktop.Foundation/Security/` | Token 内存安全存储 |

## 2. 认证流程

### 2.1 远程模式登录流程

```
Desktop                          Server (WebAPI)
  │                                  │
  │  POST /api/v1/auth/login         │
  │  { userName, password }          │
  │ ───────────────────────────────> │
  │                                  │  1. 验证用户名/密码非空
  │                                  │  2. 查询用户（IUserCrossModuleService）
  │                                  │  3. 检查用户状态
  │                                  │     ├─ 用户不存在 → 401 (AuthInvalidCredentials)
  │                                  │     ├─ 用户已禁用 → 403 (UserDisabled)
  │                                  │     └─ 账户已锁定 → 401 (UserLocked)
  │                                  │  4. BCrypt 验证密码 (WorkFactor=12)
  │                                  │     ├─ 密码错误 → 累加 FailedLoginCount
  │                                  │     │   达到阈值 → 锁定账户 15 分钟
  │                                  │     │   → 401 (AuthInvalidCredentials)
  │                                  │     └─ 密码正确 → 继续
  │                                  │  5. 重置 FailedLoginCount
  │                                  │  6. 撤销旧会话所有 Token
  │                                  │  7. 生成 JWT AccessToken + RefreshToken
  │                                  │  8. 记录安全审计日志
  │  200 { token, refreshToken,      │
  │        user, expiresAt }         │
  │ <─────────────────────────────── │
  │                                  │
  │  TokenStorageService             │
  │  .SaveAuthenticationAsync()      │
  │  （内存存储，进程退出自动清除）     │
  │                                  │
```

关键安全约束：

- **密码存储**: BCrypt (WorkFactor=12)，支持 hash 自动升级
- **错误信息统一**: 不区分"用户不存在"和"密码错误"，防止用户枚举
- **账户锁定**: 默认 5 次失败后锁定 15 分钟（`SecurityOptions.AccountLockout`）
- **旧会话清理**: 新登录时撤销该用户所有旧 RefreshToken 和 AutoLoginToken
- **速率限制**: 登录端点基于 IP 固定窗口限流，5 次/60 秒

### 2.2 本地模式认证

本地模式 (LocalWebAPI) 使用独立的 JWT 签名密钥，但保持相同的 Claim 结构和策略体系。本地模式通过 `LocalTokenValidator` 在客户端本地验证 Token，无需网络往返。

### 2.3 Token 生命周期

系统使用双 Token 机制：

| Token 类型 | 有效期 | 存储 | 用途 |
|-----------|--------|------|------|
| AccessToken (JWT) | 30 分钟（可配置，5-1440 分钟） | Client 内存 | API 请求授权 |
| RefreshToken | 7 天滑动 + 30 天绝对过期 | Server 数据库 | 无感刷新 AccessToken |
| AutoLoginToken | 长期（可撤销） | Client DPAPI 加密 | 自动登录（RememberMe） |

#### Refresh Token 轮换流程

```
Desktop                          Server
  │  POST /api/v1/auth/refresh      │
  │  { refreshToken }               │
  │ ───────────────────────────────>│
  │                                 │  1. 查询 RefreshToken 记录
  │                                 │  2. 检查 IsUsed → 重放攻击检测
  │                                 │     ├─ IsUsed=true → 撤销整个 Family
  │                                 │     │   → 401 "检测到安全威胁"
  │                                 │     └─ IsUsed=false → 继续
  │                                 │  3. 验证有效性 (IsRevoked/IsDeleted/Expired)
  │                                 │  4. 标记旧 Token 为 IsUsed
  │                                 │  5. 生成新 AccessToken + RefreshToken
  │                                 │  6. 新 Token 继承 FamilyId
  │  200 { newToken, newRefresh }   │
  │ <───────────────────────────────│
```

#### Token Family 机制

每次登录创建一个新的 `FamilyId`。同一会话内的所有 RefreshToken 共享 FamilyId。Token 轮换时新 Token 继承 FamilyId。当检测到已使用的 Token 再次被提交（`IsUsed=true`），系统撤销该 FamilyId 下的所有 Token，防止 Token 被盗用。

参见 ADR-0008: `docs/03-architecture/decisions/0008-token-security-defensive-design.md`。

## 3. JWT Claims Schema

| Claim | 类型 | ClaimType 常量 | 描述 |
|-------|------|---------------|------|
| `sub` / `nameid` | Guid | `ClaimTypes.NameIdentifier` / `JwtRegisteredClaimNames.Sub` | 用户 ID |
| `unique_name` / `name` | string | `ClaimTypes.Name` / `JwtRegisteredClaimNames.UniqueName` | 用户名 |
| `role` | string | `ClaimTypes.Role` | 角色名称 (SuperAdmin/Admin/Doctor/Receptionist) |
| `user_type` | string | 自定义 | "superadmin" 或 "user"（区分双轨认证） |
| `jti` | Guid | `JwtRegisteredClaimNames.Jti` | Token 唯一标识 |
| `iat` | long | `JwtRegisteredClaimNames.Iat` | Token 签发时间 (Unix 秒) |

`ClaimsNormalizationMiddleware` 在每次请求时确保 Claim 格式统一，补全多种别名格式（如 `sub`/`nameid`/`NameIdentifier` 三种形式共存），兼容不同 JWT 库的 Claim 解析习惯。

## 4. 授权策略

系统定义 4 种基于角色的授权策略，通过 `RequireRole()` 声明式配置：

| Policy | 常量 | 满足条件的角色 | 典型用途 |
|--------|------|--------------|----------|
| `AdminOnly` | `PolicyConstants.AdminOnly` | SuperAdmin, Admin | 用户管理、药材管理 |
| `DoctorOrAdmin` | `PolicyConstants.DoctorOrAdmin` | SuperAdmin, Admin, Doctor | 医案 CRUD、处方操作 |
| `PatientAccess` | `PolicyConstants.PatientAccess` | SuperAdmin, Admin, Doctor, Receptionist | 患者信息访问 |
| `SuperAdminOnly` | `PolicyConstants.SuperAdminOnly` | SuperAdmin | 系统配置、危险操作 |

角色层次（隐含权限继承）：

```
SuperAdmin → Admin → Doctor → Receptionist
```

### 策略配置

```csharp
// AuthenticationServiceCollectionExtensions.cs
options.FallbackPolicy = 要求认证用户;  // 默认所有端点需要认证
options.AddPolicy("AdminOnly", RequireRole("SuperAdmin", "Admin"));
options.AddPolicy("DoctorOrAdmin", RequireRole("SuperAdmin", "Admin", "Doctor"));
options.AddPolicy("PatientAccess", RequireRole("SuperAdmin", "Admin", "Doctor", "Receptionist"));
options.AddPolicy("SuperAdminOnly", RequireRole("SuperAdmin"));
```

### 默认安全策略

- **FallbackPolicy**: 要求所有端点默认认证（`RequireAuthenticatedUser`）
- **显式豁免**: `AllowAnonymous` 标注的端点（login、logout、refresh、health）
- **Swagger 不受影响**: Swagger 中间件在 UseRouting 之前，不经过授权管道

## 5. 桌面端认证状态机

桌面端使用转换表驱动的 `AuthenticationStateMachine`，线程安全，通过 Prism PubSubEvent 跨模块通知状态变更。

### 状态定义

```
Idle ──StartLogin──> Authenticating ──CredentialsValidated──> LoadingProfile
  │                                                    │
  └──StartAutoLogin──> ValidatingToken ──TokenValidated─┘
                                                          │
                                             ProfileLoaded│
                                                          ▼
                              LoadingModules ──ModulesLoaded──> Navigating
                                                                    │
                                                        NavigationCompleted│
                                                                    ▼
                                                              Authenticated
                                                            ╱    │     ╲
                                          StartLogout     ╱  SessionExpire StartTokenRefresh
                                                 ╲      ╱        │            ╲
                                                  LoggingOut      │       RefreshingToken
                                                      │           │            │
                                              LogoutSuccess      │     TokenRefreshSuccess
                                                  │              │            │
                                                  ▼              ▼            ▼
                                                Idle       SessionExpired  Authenticated
                                                             ╱    ╲
                                                StartLogin ╱      ╲StartAutoLogin
                                                          ▼
                                                  (回到认证流程)
```

### AuthState 枚举

| 状态 | 描述 | 可触发事件 |
|------|------|-----------|
| `Idle` | 未认证 | StartLogin, StartAutoLogin |
| `Authenticating` | 验证凭证中 | CredentialsValidated, LoginFailure, Reset |
| `ValidatingToken` | 验证 Token 中（自动登录） | TokenValidated, LoginFailure, Reset |
| `LoadingProfile` | 加载用户信息 | ProfileLoaded, LoginFailure, Reset |
| `LoadingModules` | 加载业务模块 | ModulesLoaded, LoginFailure, Reset |
| `Navigating` | 导航到主界面 | NavigationCompleted, LoginFailure, Reset |
| `Authenticated` | 已认证 | StartLogout, SessionExpire, StartTokenRefresh |
| `Failed` | 登录失败 | StartLogin, StartAutoLogin, Reset |
| `LoggingOut` | 登出中 | LogoutSuccess, LogoutFailure, Reset |
| `SessionExpired` | 会话过期 | StartLogin, StartAutoLogin, Reset |
| `RefreshingToken` | Token 刷新中 | TokenRefreshSuccess, TokenRefreshFailure, Reset |

### Token 生命周期状态

`TokenLifecycleService` 独立管理 Token 过期监控：

```
NotAuthenticated → Active → Warning → Expired → NotAuthenticated
                     ↑         │
                     └─刷新成功─┘
```

| 状态 | 描述 | 触发条件 |
|------|------|---------|
| `NotAuthenticated` | 无 Token | 初始/登出 |
| `Active` | Token 有效 | 登录成功 |
| `Warning` | 即将过期 | 剩余 < 5 分钟 |
| `Expired` | 已过期 | Token 过期时间已过 |

定时器每 30 秒检查 Token 状态。进入 Warning 时自动尝试 `TryRefreshTokenAsync`。

### Token 存储

`TokenStorageService` 采用进程内存存储（非磁盘持久化），满足医疗系统合规要求：

- 每次启动必须输入密码
- 多人共享工作站安全（进程结束即清除）
- 不存在磁盘残留 Token 的风险

## 6. Policy-to-Endpoint Matrix

### AuthController (`/api/v1/auth`)

| 端点 | 方法 | Policy | 备注 |
|------|------|--------|------|
| `/login` | POST | AllowAnonymous | RateLimiting("Login") |
| `/auto-login` | POST | AllowAnonymous | RateLimiting("Login") |
| `/logout` | POST | AllowAnonymous | 允许过期 Token 访问 |
| `/refresh` | POST | AllowAnonymous | Token 轮换 |
| `/validate` | GET | FallbackPolicy (需认证) | 验证 Bearer Token |

### UsersController (`/api/v1/users`)

| 端点 | Policy | 备注 |
|------|--------|------|
| 类级别 | FallbackPolicy (需认证) | 控制器无类级 Policy |
| `GET /` | AdminOnly | 用户列表 |
| `GET /{id}` | AdminOnly | 用户详情 |
| `POST /` | AdminOnly | 创建用户 |
| `PUT /{id}` | AdminOnly | 更新用户 |
| `DELETE /{id}` | SuperAdminOnly | 删除用户（仅超管） |
| `PUT /{id}/password` | AdminOnly | 重置密码 |
| `PUT /{id}/role` | SuperAdminOnly | 角色变更（仅超管） |
| `PUT /{id}/status` | AdminOnly | 启用/禁用 |
| `PUT /{id}/profile` | AdminOnly | 个人信息 |

### PatientsController (`/api/v1/patients`)

| 端点 | Policy | 备注 |
|------|--------|------|
| 类级别 | FallbackPolicy (需认证) | |
| 所有 CRUD 方法 | PatientAccess | 包含 Receptionist |

### MedicalCasesController (`/api/v1/medicalcases`)

| 端点 | Policy | 备注 |
|------|--------|------|
| 类级别 | FallbackPolicy (需认证) | |
| 所有方法 | DoctorOrAdmin | 医生及以上权限 |
| 审核相关方法 | DoctorOrAdmin | 医案审核 |

### MedicalCaseProcessingController (`/api/v1/medicalcase-processing`)

| Policy | 备注 |
|--------|------|
| DoctorOrAdmin | 医案处理流程 |

### MedicalCasePrintController (`/api/v1/medicalcase-print`)

| Policy | 备注 |
|--------|------|
| DoctorOrAdmin | 打印处方 |

### MedicalCaseAuditController (`/api/v1/medicalcase-audit`)

| Policy | 备注 |
|--------|------|
| DoctorOrAdmin | 医案审核 |

### HerbsController (`/api/v1/herbs`)

| Policy | 备注 |
|--------|------|
| DoctorOrAdmin | 药材管理 |

### FormulasController (`/api/v1/formulas`)

| Policy | 备注 |
|--------|------|
| DoctorOrAdmin | 验方管理 |

### RegistrationsController (`/api/v1/registrations`)

| 端点 | Policy | 备注 |
|------|--------|------|
| 类级别 | PatientAccess | 接诊员可访问 |
| 创建/更新挂号 | DoctorOrAdmin | 需医生权限 |

### SyncController (`/api/v1/sync`)

| Policy | 备注 |
|--------|------|
| DoctorOrAdmin | 数据同步 |

## 7. 安全考虑

### 7.1 Token 重放检测 (Token Family)

系统通过 Token Family 机制检测 RefreshToken 盗用：

1. 每次登录创建新的 `FamilyId`
2. Token 轮换时旧 Token 标记 `IsUsed=true`，新 Token 继承 `FamilyId`
3. 如果已使用的 Token 再次被提交，系统判定为重放攻击
4. 撤销该 Family 下的所有 Token，强制用户重新登录

详见 `RefreshToken.IsReplayAttack` 属性和 `TokenManagementService.RefreshTokenAsync` 中的检测逻辑。

### 7.2 安全响应头

`SecurityHeadersMiddleware` 为所有响应添加安全头：

| Header | 值 | 用途 |
|--------|---|------|
| `X-Content-Type-Options` | `nosniff` | 防止 MIME 嗅探 |
| `X-Frame-Options` | `DENY` | 防止点击劫持 |
| `X-XSS-Protection` | `1; mode=block` | XSS 过滤（旧浏览器） |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | 控制引用信息 |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | 限制浏览器功能 |
| `Content-Security-Policy` | 严格策略（生产）/ 仅报告（开发） | 防 XSS |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains; preload` | 强制 HTTPS（仅生产） |

生产环境 CSP 策略禁止 `unsafe-inline`、`unsafe-eval`，要求 `trusted-types`。

### 7.3 CORS

系统使用 ASP.NET Core 内建 CORS 支持。由于桌面端为 WPF 应用（非浏览器），CORS 主要用于开发和 Swagger UI。生产环境中 API 仅接受桌面客户端请求。

### 7.4 HTTPS 执行

- 生产环境通过 `Strict-Transport-Security` 头强制 HTTPS
- CSP 策略包含 `upgrade-insecure-requests` 和 `block-all-mixed-content`
- JWT 配置 `RequireSignedTokens = true`，拒绝未签名 Token

### 7.5 速率限制

| 策略 | 限制 | 适用范围 |
|------|------|---------|
| Login | 5 次/60 秒/IP | `/api/v1/auth/login`, `/api/v1/auth/auto-login` |
| ApiCalls | 100 次/分钟/IP | 全局 API 调用 |

速率限制通过 `Security:RateLimiting:Enabled` 配置项控制，开发/测试环境可设为 `false` 禁用。被限制时返回 429 状态码和结构化错误响应 (`ErrorCode.RateLimitExceeded`)。

### 7.6 JWT 密钥安全

- 密钥最小长度 32 字符（`JwtOptions.SecretKey` + `JwtService.ValidateSecretKeyStrength()`）
- 生产环境禁止使用已知默认密钥
- 生产环境密钥必须通过 `JWT_SECRET` 环境变量或 `Jwt:SecretKey` 配置注入
- HMAC-SHA256 签名算法

### 7.7 密码安全

- BCrypt 哈希 (WorkFactor=12)
- 登录成功时自动升级旧版 hash（`PasswordHelper.VerifyPassword` 返回 `NewHashedPassword`）
- 账户锁定: 默认 5 次失败后锁定 15 分钟（`SecurityOptions.AccountLockout`）

### 7.8 安全审计

`SecurityAuditService` 记录所有认证相关事件：

| 事件类型 | 触发时机 |
|---------|---------|
| `Login` | 登录成功 |
| `LoginFailed` | 登录失败 |
| `Logout` | 登出 |
| `RefreshToken` | Token 刷新成功 |
| `RefreshTokenRejected` | Token 刷新被拒绝 |
| `TokenReplayAttack` | 检测到重放攻击 |
| `TokenRevoked` | Token 被撤销 |

审计记录包含：IP 地址（脱敏，如 `192.168.1.*`）、UserAgent（截断至 500 字符）、时间戳。审计日志保留 365 天（`SecurityOptions.AuditRetentionDays`）。

## 8. 决策记录

| ID | 决策 | 原因 |
|----|------|------|
| ADR-0005 | 双轨认证 (AdminSecrets + Users) | SuperAdmin 使用独立凭据表，与普通用户分离管理 |
| ADR-0008 | Token Family 防御性设计 | RefreshToken 轮换 + Family 撤销检测盗用 |
| Issue #1864 | 客户端 JWT 自验证 | Desktop 端本地解析 JWT，移除对 Server 验证 API 的依赖 |
| Issue #1907 | Token 内存存储 | 医疗系统合规：进程结束自动清除，不留磁盘痕迹 |
| Issue #1732 | FallbackPolicy 全局认证 | 默认安全：所有端点需认证，显式豁免仅需 AllowAnonymous |
| Sprint3-A3-08 | SecurityHeadersMiddleware | 统一添加安全响应头，防护 XSS/点击劫持/MIME 嗅探 |

---

## 变更记录

| 日期 | 版本 | 描述 | 作者 |
|------|------|------|------|
| 2026-06-13 | 1.0 | 初始创建：完整安全架构文档 | AI |
