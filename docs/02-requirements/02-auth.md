# 认证与会话 (Authentication & Session)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建

## 模块概述

认证与会话模块负责保障中医诊所系统中患者敏感医疗数据（诊断记录、处方信息、身份信息）的访问安全。系统采用双模式认证架构：远程模式提供完整的 JWT 双令牌机制（2h 访问令牌 + 7d 刷新令牌族），适合多用户多设备协同工作场景；本地模式提供简化 JWT（1 年有效期），适合医生外出诊疗等离线场景。

模块核心包括：用户名密码登录、账户锁定防护、登录限流、令牌刷新与验证、重放攻击检测（令牌族撤销）、安全审计日志、登出、本地自动登录（AutoLoginToken）及轮换、保留用户名拦截、本地简化认证与限流。诊所环境下医生日均接诊 15-30 人，认证流程必须在保障安全的同时尽可能减少摩擦。

## 业务规则

1. **令牌族旋转撤销**：RefreshToken 基于家族（family）管理，刷新时签发新令牌并废弃旧令牌；检测到已废弃令牌被重放时，立即撤销整个令牌族（防重放攻击）。
2. **账户锁定（可配置）**：`SecurityOptions.AccountLockout.MaxFailedCount` 控制最大失败次数，`LockoutMinutes` 控制锁定时长；达到阈值后账户被临时锁定。
3. **登录限流**：`[EnableRateLimiting("Login")]` 应用于 login 与 auto-login 端点，防止暴力破解。
4. **AutoLoginToken 服务端可撤销**：本地自动登录令牌由服务端管理生命周期，成功登录后轮换；服务端可主动撤销以终止本地会话。
5. **登出允许过期令牌**：`[AllowAnonymous]` 装饰登出端点，即使令牌已过期也能正常登出，避免令牌过期导致无法登出的死锁。
6. **保留用户名**：`admin/administrator/root/system/superadmin/sysadmin` 为系统保留用户名，拒绝普通注册与冒用。
7. **AdminSecrets 已移除**（Issue #1909）：SuperAdmin 凭证已统一到 Users 表（Role=100），不再使用独立的 AdminSecrets 表。

## 双模式差异

| 维度 | 远程模式 | 本地模式 |
|------|----------|----------|
| 访问令牌 | JWT 2h | JWT 1 年 |
| 刷新令牌 | RefreshToken 7d（族旋转） | 无 |
| AutoLoginToken | 服务端可撤销 + 轮换 | 由 `LocalJwtConfig` 签发 |
| 限流策略 | `[EnableRateLimiting("Login")]` | 5 次/分 |
| 安全审计 | 完整记录 | 简化 |

## 用户故事

### US-AUTH-001: 用户名密码登录

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 任何系统用户，**我想要** 使用用户名和密码登录系统，**以便** 安全地访问我的工作环境。

**验收标准**:
- [ ] 接受用户名 + 密码组合
- [ ] 验证成功后返回 JWT 访问令牌（远程：2h；本地：1 年）
- [ ] 验证失败返回通用错误信息（不泄露用户名是否存在）
- [ ] 连续失败达到阈值后触发账户锁定
- [ ] 登录端点应用限流策略

**业务规则**:
1. 远程模式返回 access_token (2h) + refresh_token (7d，可旋转)
2. 本地模式返回单一 JWT (1 年，无 refresh)
3. 保留用户名（admin/administrator/root/system/superadmin/sysadmin）拒绝普通注册
4. SuperAdmin 凭证统一存储于 Users 表（Role=100），AdminSecrets 已移除（Issue #1909）

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | JWT 2h + Refresh 7d（族旋转）+ 安全审计 |
| 本地 | JWT 1 年 + 限流 5 次/分 + 无 refresh |

**实现参考**: `AuthController.cs:44` (LoginAsync), `AuthService.cs`, `LocalWebAPI/Controllers/AuthController.cs`

---

### US-AUTH-002: 登录失败锁定

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统管理员，**我想要** 在用户连续登录失败达到阈值后锁定账户，**以便** 防止暴力破解攻击。

**验收标准**:
- [ ] 记录连续登录失败次数
- [ ] 达到 `MaxFailedCount` 后锁定账户 `LockoutMinutes` 分钟
- [ ] 锁定期间拒绝登录，即使密码正确
- [ ] 锁定期满后自动解锁，失败计数清零
- [ ] 登录成功后失败计数清零

**业务规则**:
1. 阈值由 `SecurityOptions.AccountLockout.MaxFailedCount` 配置
2. 锁定时长由 `SecurityOptions.AccountLockout.LockoutMinutes` 配置
3. 锁定状态持久化，重启服务不重置

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `AuthController.cs:44`, `ITokenManagementService`, `SecurityOptions`

---

### US-AUTH-003: 登录限流

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统管理员，**我想要** 对登录端点应用限流策略，**以便** 防止单一来源高频登录尝试。

**验收标准**:
- [ ] login 端点应用 `[EnableRateLimiting("Login")]`
- [ ] auto-login 端点同样应用限流
- [ ] 超出限流阈值返回 429 Too Many Requests
- [ ] 限流基于客户端标识（IP/设备）

**业务规则**:
1. 远程限流策略由 ASP.NET Core RateLimiter 配置
2. 本地模式固定 5 次/分

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 按 `Login` 策略配置（可调） |
| 本地 | 固定 5 次/分 |

**实现参考**: `AuthController.cs:41` (`[EnableRateLimiting("Login")]`), `AuthController.cs:74`

---

### US-AUTH-004: 令牌刷新

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 已登录用户，**我想要** 使用刷新令牌换取新的访问令牌，**以便** 在访问令牌过期后无需重新登录即可继续工作。

**验收标准**:
- [ ] 接受有效的 refresh_token
- [ ] 返回新的 access_token + 新的 refresh_token
- [ ] 旧 refresh_token 立即失效
- [ ] 无效或已撤销的 refresh_token 返回 401
- [ ] 检测到已废弃令牌重放时撤销整个令牌族

**业务规则**:
1. RefreshToken 有效期 7 天
2. 刷新采用族旋转机制：每次刷新签发新令牌，旧令牌标记为已使用
3. 重放检测：已废弃令牌再次出现 → 撤销整个家族

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | RefreshToken 族旋转 + 重放检测 |
| 本地 | 不适用（1 年令牌，无 refresh） |

**实现参考**: `AuthController.cs:128` (RefreshTokenAsync), `ITokenManagementService`

---

### US-AUTH-005: 令牌验证

**角色**: 系统（中间件）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统中间件，**我想要** 验证请求携带的访问令牌有效性，**以便** 确保只有合法会话才能访问受保护资源。

**验收标准**:
- [ ] 从 Authorization Header 提取 Bearer 令牌
- [ ] 验证签名、有效期、签发方
- [ ] 验证通过后将用户声明注入 HttpContext
- [ ] 验证失败返回 401
- [ ] 提供显式 /validate 端点供前端主动校验

**业务规则**:
1. JWT Bearer 中间件自动校验每个受保护请求
2. 显式 validate 端点用于前端会话健康检查

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 JWT 中间件） |

**实现参考**: `AuthController.cs:151` (ValidateTokenFromHeaderAsync), JWT Bearer 中间件

---

### US-AUTH-006: 重放攻击检测（令牌族撤销）

**角色**: 系统安全
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统安全机制，**我想要** 检测刷新令牌的重放行为并撤销整个令牌族，**以便** 防御令牌被盗用后的重放攻击。

**验收标准**:
- [ ] 每个 RefreshToken 归属一个令牌族（family）
- [ ] 正常刷新：旧令牌标记已使用，签发新令牌（同族）
- [ ] 重放检测：已使用的旧令牌再次提交 → 判定为攻击
- [ ] 检测到重放立即撤销该族所有令牌（包括当前有效令牌）
- [ ] 受影响用户需重新登录

**业务规则**:
1. 令牌族在首次登录时创建，族 ID 贯穿整个会话生命周期
2. 撤销操作不可逆，要求所有客户端重新认证
3. 安全审计日志记录重放事件

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 完整族旋转 + 重放撤销 |
| 本地 | 不适用（无 refresh） |

**实现参考**: `AuthController.cs:128`, `ITokenRevocationService`, `ITokenManagementService`

---

### US-AUTH-007: 安全审计日志

**角色**: 系统管理员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 系统管理员，**我想要** 记录认证相关的安全事件，**以便** 审计异常行为并满足合规要求。

**验收标准**:
- [ ] 记录登录成功/失败事件
- [ ] 记录令牌刷新、撤销、重放检测事件
- [ ] 记录账户锁定与解锁
- [ ] 记录登出事件
- [ ] 审计日志包含时间戳、用户名、IP、设备标识、事件类型

**业务规则**:
1. 审计日志由 `ISecurityAuditService` 统一写入
2. 审计日志不可篡改，独立于业务日志
3. 本地模式审计记录简化（无 IP/设备维度）

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 完整审计（含 IP/设备） |
| 本地 | 简化审计（单机环境） |

**实现参考**: `ISecurityAuditService`, `AuthService.cs`

---

### US-AUTH-008: 登出（含过期令牌）

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 已登录用户，**我想要** 主动登出并使当前会话令牌失效，**以便** 在共用设备上保护账户安全。

**验收标准**:
- [ ] 接受当前会话令牌
- [ ] 撤销当前 access_token 与 refresh_token
- [ ] 即使令牌已过期也能成功登出（不报错）
- [ ] 登出后该令牌无法再用于任何请求
- [ ] 登出事件写入审计日志

**业务规则**:
1. 登出端点使用 `[AllowAnonymous]`，避免令牌过期导致无法登出的死锁
2. 登出后服务端标记令牌为已撤销
3. 本地模式登出同样撤销 AutoLoginToken

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 撤销 access + refresh + 记录审计 |
| 本地 | 撤销 JWT + AutoLoginToken |

**实现参考**: `AuthController.cs:104` (LogoutAsync, `[AllowAnonymous]`)

---

### US-AUTH-009: 本地自动登录（AutoLoginToken）

**角色**: 医生
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生，**我想要** 在本地模式启用自动登录，**以便** 频繁接诊时无需反复输入密码。

**验收标准**:
- [ ] 首次登录成功后可选择签发 AutoLoginToken
- [ ] AutoLoginToken 持久化存储于本地
- [ ] 后续启动系统时自动提交 AutoLoginToken 完成登录
- [ ] AutoLoginToken 服务端可主动撤销
- [ ] auto-login 端点应用限流

**业务规则**:
1. AutoLoginToken 由服务端签发与管理，非纯客户端机制
2. 服务端撤销后，本地自动登录立即失效
3. auto-login 端点同样应用 `[EnableRateLimiting("Login")]`

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 签发 + 管理 + 可撤销 |
| 本地 | 提交 AutoLoginToken 完成无密码登录 |

**实现参考**: `AuthController.cs:79` (AutoLoginAsync), `IAutoLoginService`

---

### US-AUTH-010: AutoLoginToken 轮换

**角色**: 系统安全
**优先级**: Should
**状态**: ✅ 已实现

**作为** 系统安全机制，**我想要** 在自动登录成功后轮换 AutoLoginToken，**以便** 缩短单个令牌的有效暴露窗口。

**验收标准**:
- [ ] auto-login 成功后签发新的 AutoLoginToken
- [ ] 旧 AutoLoginToken 立即失效
- [ ] 客户端持久化更新为新令牌
- [ ] 旧令牌再次使用被判为无效

**业务规则**:
1. 轮换机制降低令牌被盗用后的持续风险
2. 轮换由服务端 `ITokenManagementService` 执行
3. 客户端需正确处理令牌更新（避免使用过期令牌）

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `AuthController.cs:79`, `ITokenManagementService`

---

### US-AUTH-011: 保留用户名拦截

**角色**: 系统管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统管理员，**我想要** 拦截系统保留用户名的注册与冒用，**以便** 防止特权账号被恶意占据。

**验收标准**:
- [ ] 保留用户名清单：admin、administrator、root、system、superadmin、sysadmin
- [ ] 创建用户时校验用户名是否命中保留清单
- [ ] 命中保留清单返回明确错误（不区分大小写）
- [ ] 登录时保留用户名走专用校验路径

**业务规则**:
1. 保留清单在系统中硬编码，不可通过配置修改
2. 校验不区分大小写
3. SuperAdmin 账户通过受控流程创建，不走普通注册

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `AuthService.cs`, 保留用户名校验逻辑

---

### US-AUTH-012: 本地简化认证（1 年令牌）

**角色**: 医生
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生，**我想要** 在本地模式使用长效令牌（1 年）进行认证，**以便** 离线外出诊疗时无需频繁重新登录。

**验收标准**:
- [ ] 本地模式由 `LocalJwtConfig` 签发 JWT
- [ ] 令牌有效期 1 年
- [ ] 不签发 refresh_token（无需刷新）
- [ ] 本地令牌通过同一 JWT Bearer 中间件校验
- [ ] 本地 WebAPI 复用统一 Service 层

**业务规则**:
1. 本地模式通过 LocalWebAPI 承载，复用所有服务端模块的 Service 层
2. `LocalJwtConfig` 独立配置签发参数（密钥、签发方、有效期）
3. 本地令牌不参与族旋转与重放检测（无 refresh）

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 不适用 |
| 本地 | `LocalJwtConfig` 签发 1 年 JWT，无 refresh |

**实现参考**: `LocalWebAPI/Controllers/AuthController.cs:19`, `LocalJwtConfig`

---

### US-AUTH-013: 本地登录限流（5 次/分）

**角色**: 系统安全
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统安全机制，**我想要** 对本地模式登录应用限流（5 次/分），**以便** 即使在单机环境也防止暴力尝试。

**验收标准**:
- [ ] 本地 login 端点应用限流
- [ ] 限流阈值固定为 5 次/分
- [ ] 超出阈值返回 429
- [ ] 限流基于客户端标识

**业务规则**:
1. 本地限流策略独立于远程，固定为 5 次/分
2. 限流防止本地环境下密码暴力枚举

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 不适用（使用远程 `Login` 策略） |
| 本地 | 固定 5 次/分 |

**实现参考**: `LocalWebAPI/Controllers/AuthController.cs:19`, 本地限流中间件配置
