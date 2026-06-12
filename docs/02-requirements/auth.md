# 认证与会话管理 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所电子化后，患者敏感医疗数据 (诊断记录、处方信息、个人身份信息) 集中存储在系统中。缺乏身份验证和访问控制意味着任何人都可以查看、修改甚至删除这些数据。同时，诊所环境下医生频繁接诊，登录流程必须尽可能减少摩擦，避免打断诊疗节奏。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 医生 | 每次开机/超时后需重新输入密码，打断诊疗节奏 | 日均浪费 5-10 分钟在登录操作上 |
| 医生 | 外出诊疗 (本地模式) 无网络时仍需安全访问 | 离线场景下数据安全无保障 |
| 管理员 | 无法控制离职/调岗人员的系统访问 | 敏感数据泄露风险 |
| 管理员 | 同一账号在多台设备登录无法检测 | 无法追溯操作责任人 |

### 1.3 证据

- 卫生部门信息化安全要求: 医疗系统必须实现身份认证和访问审计
- 临床工作流观察: 医生日均接诊 15-30 人，每次重登录消耗 30 秒以上
- 产品需求分析: 系统包含 4 级角色权限 (SuperAdmin/Admin/Doctor/Receptionist)，需要统一认证入口

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | 登录/登出/Token 刷新/会话超时延长 |
| Admin | 登录/登出/Token 刷新/会话超时延长 |
| Doctor | 登录/登出/Token 刷新/会话超时延长 |
| Receptionist | 登录/登出/Token 刷新/会话超时延长 |

> 认证操作本身不区分角色，所有已注册用户均可使用。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 患者数据安全 | 卫生部门信息化安全要求，医疗数据必须有访问控制 |
| 诊疗连续性 | 自动登录 + 滑动刷新最小化登录摩擦，不打断接诊节奏 |
| 操作可追溯 | JWT + 安全审计日志确保每个操作可追溯到具体用户 |
| 离线安全 | 本地模式密码验证 + 不活跃超时保障离线场景数据安全 |

### 3.2 Why Now

系统电子化后，患者医疗数据从纸质病历迁移到集中数据库，访问控制从"物理隔离" (锁柜) 变为"逻辑隔离" (身份认证)。这是系统可用的前提条件，不是可选功能。

---

## 4. Solution Overview

认证模块采用 JWT Bearer Token 机制，实现完整的身份验证生命周期管理:

**核心能力:**
- **密码登录**: 用户名 + 密码 → JWT AccessToken + RefreshToken
- **自动登录**: AutoLoginToken (DPAPI 加密 + HMAC 校验) 实现免密启动
- **滑动刷新**: AccessToken 即将过期时自动使用 RefreshToken 获取新 Token，用户无感知
- **安全防护**: Token Family 重放攻击检测、单会话登录 (新设备踢旧设备)、不活跃超时登出
- **双模式支持**: 远程 (JWT Token) + 本地 (LocalAuthService 密码验证)

**认证流程:**
```
应用启动 → 检查 AutoLoginToken → [有] 自动登录 → 成功 → 进入工作台
                                                → 失败 → 手动登录
                               → [无] 手动登录 → 输入用户名密码 → 验证 → 进入工作台
工作中 → AccessToken 即将过期 → 滑动刷新 (用户无感) → 继续工作
      → 不活跃 15 分钟 → 静默登出 → 跳转登录页
      → 其他设备登录 → Token Family 撤销 → 强制登出
```

---

## 5. Success Metrics

| 指标 | 当前 (纸质流程) | v1.0 目标 | 衡量方式 |
|------|----------------|----------|---------|
| 登录成功率 | N/A | > 99% | 日志统计 (SecurityAuditLog) |
| 自动登录启用率 | 0% | > 80% 医生启用 "记住密码" | 凭据文件统计 |
| Token 刷新无感率 | N/A | 100% (用户零感知) | 零 "会话过期" 投诉 |
| 安全事件检出率 | N/A | 100% 重放攻击检出 | SecurityAuditLog |
| 单次登录耗时 | 30 秒+ (输入密码) | < 3 秒 (自动登录) | 操作日志 |

---

## 6. Epic Hypothesis

We believe that 实现 JWT 自动登录 + 滑动刷新 + 重放攻击检测 + 不活跃超时的认证体系 for 诊所全部用户 (医生/管理员/前台) will achieve 数据安全访问控制与最小登录摩擦的平衡。We'll know we're right when 自动登录启用率 > 80%、Token 刷新零用户感知、且零未授权数据访问事件。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-AUTH-001 | 用户登录 | Must |
| US-AUTH-002 | 自动登录 | Must |
| US-AUTH-003 | Token 刷新 | Must |
| US-AUTH-004 | 重放攻击检测 | Should |
| US-AUTH-005 | 用户登出 | Must |
| US-AUTH-006 | 不活跃超时 | Should |
| US-AUTH-007 | 登出前警告 | Should |
| US-AUTH-008 | Token 验证 | Must |
| US-AUTH-009 | 凭证本地存储 | Must |
| US-AUTH-010 | 登录状态机 | Must |
| US-AUTH-011 | Token 刷新失败分级处理 | Should |
| US-AUTH-012 | 登录界面 | Must |
| US-AUTH-013 | 认证事件体系 | Should |

---

### US-AUTH-001: 用户登录

> As a 注册用户, I want to 通过用户名和密码登录系统,
> so that 我可以获取访问令牌并安全使用系统功能。

**Acceptance Criteria:**
- [ ] 正确凭据 → 返回 200 + AccessToken + RefreshToken + UserDetailDto
- [ ] 错误凭据 → 返回 401 + ErrorCode=InvalidCredentials(10101)
- [ ] 禁用用户登录 → 返回 403 + ErrorCode=UserDisabled
- [ ] 60秒内第6次登录 → 返回 429 Too Many Requests

**Business Rules:**
1. 验证用户名和密码匹配
2. 验证用户状态为 Enabled
3. 登录成功返回 AccessToken + RefreshToken + 用户信息
4. 登录失败累计 FailedLoginCount
5. 登录端点限流: 5次/60秒
6. 单会话登录: 登录时撤销该用户所有现有 Token Family，旧设备下次请求/刷新时强制登出 (AUTH-D06)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/auth/login`，服务端验证凭据，返回 JWT Token |
| 本地 | LocalAuthService 本地验证，不生成 JWT |

### US-AUTH-002: 自动登录

> As a 医生, I want to 应用启动时自动登录,
> so that 我不必每次开机都输入密码，可以快速进入诊疗状态。

**Acceptance Criteria:**
- [ ] 保存凭据后应用重启 → 自动登录成功，获取新 Token
- [ ] AutoLoginToken 被撤销后 → 自动登录返回 401，跳转手动登录
- [ ] HMAC 校验失败 → 自动清除凭据 + 记录安全警告日志

**Business Rules:**
1. AutoLoginToken 由服务端生成，仅在 RememberMe=true 时返回
2. AutoLoginToken 使用 DPAPI 加密存储在本地
3. 存储时计算 HMAC-SHA256 完整性校验值
4. 读取时验证 HMAC，失败则删除凭据并记录安全警告
5. 服务端可随时撤销 AutoLoginToken

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/auth/auto-login`，发送 UserName + AutoLoginToken |
| 本地 | 不支持。本地模式无 Token 机制，每次启动需手动登录 |

### US-AUTH-003: Token 刷新

> As a 已登录用户, I want to Token 在后台自动刷新,
> so that 我可以持续工作而不被 "会话过期" 打断。

**Acceptance Criteria:**
- [ ] AccessToken 剩余 < 5 分钟 → 自动使用 RefreshToken 获取新 Token
- [ ] RefreshToken 过期 → 返回 401，尝试 AutoLogin 或跳转手动登录
- [ ] 用户不活跃超过 15 分钟 → 不触发刷新，等待用户操作后再刷新

**Business Rules:**
1. 仅在用户活跃时执行 Token 刷新 (滑动过期)
2. RefreshToken 有效期 7 天
3. 使用 FamilyId 追踪 Token 家族
4. 每个 RefreshToken 仅可使用一次 (IsUsed 标记)
5. 绝对过期时间限制: 30 天会话期限

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/auth/refresh`，返回新 AccessToken + 新 RefreshToken |
| 本地 | 不需要 (无 JWT) |

### US-AUTH-004: 重放攻击检测

> As a 系统安全管理者, I want to 检测 RefreshToken 重复使用的安全威胁,
> so that 被窃取的 Token 无法被攻击者利用。

**Acceptance Criteria:**
- [ ] 重复使用已标记 IsUsed 的 RefreshToken → 同 FamilyId 所有 Token 失效
- [ ] 客户端收到 ErrorCode=TokenRevoked → 清除 Token + 显示 "会话已在其他设备终止"

**Business Rules:**
1. RefreshToken 使用后标记 IsUsed=true
2. 已使用的 RefreshToken 再次提交，判定为重放攻击
3. 重放攻击时使整个 Token Family (同 FamilyId) 失效
4. 返回 ErrorCode=TokenRevoked
5. 客户端收到 TokenRevoked 后立即清除 Token，显示 "会话已在其他设备终止"

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 服务端检测并处理 |
| 本地 | 不适用 |

### US-AUTH-005: 用户登出

> As a 已登录用户, I want to 主动退出登录,
> so that 我离开工位时他人无法使用我的账号操作系统。

**Acceptance Criteria:**
- [ ] 登出后使用旧 AccessToken 请求 → 返回 401
- [ ] 登出后即使保存了密码 → 需重新输入密码登录 (不支持会话恢复)
- [ ] 网络断开时登出 → 本地立即清除 Token，服务端登出加入重试队列

**Business Rules:**
1. 本地登出优先: 立即清除内存中的 Token
2. 服务端登出: 调用 logout API 撤销 RefreshToken
3. 服务端失败时加入待处理队列，网络恢复时重试
4. 登出后必须重新输入密码，不支持会话恢复
5. 允许使用过期 Token 进行登出操作
6. 必须提供 RefreshToken 或 UserName

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/auth/logout` |
| 本地 | 清除本地会话状态 |

### US-AUTH-006: 不活跃超时

> As a 诊所管理者, I want to 用户长时间不操作时自动登出,
> so that 医生离开工位后患者数据不会被未授权人员查看。

**Acceptance Criteria:**
- [ ] 15 分钟无键盘/鼠标操作 → 静默登出，跳转登录界面
- [ ] 任何键盘/鼠标操作 → 重置不活跃计时器

**Business Rules:**
1. 追踪键盘输入和鼠标操作
2. 超过不活跃超时时间后静默登出 (不显示警告)
3. 默认超时: 15 分钟，可配置

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 客户端检测不活跃，触发登出流程 |
| 本地 | 同远程模式 (本地也需要安全控制) |

### US-AUTH-007: 登出前警告 (已延期/移除)

> **[已延期]** 此功能在 simplify-auth 重构 (2026-02-21) 中已移除，仅保留静默登出 (US-AUTH-006)。超时前警告机制不再纳入 v1.0 范围。
>
> 原始需求: As a 医生, I want to 在即将超时登出前收到警告,
> so that 我可以选择继续工作而不丢失未保存的数据。

**Acceptance Criteria:**
- [ ] 超时前 2 分钟 → 弹出警告对话框 (保持登录/立即登出)
- [ ] 远程模式点击 "保持登录" → 刷新 Token + 重置超时计时器
- [ ] 本地模式点击 "保持登录" → 仅重置不活跃计时器

**Business Rules:**
1. 在超时前 2 分钟显示警告 (可配置)
2. 用户可选 "保持登录" (刷新 Token) 或 "立即登出"
3. 不操作则到期后自动登出

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 弹出警告对话框，选择保持登录时刷新 Token |
| 本地 | 同远程模式，"保持登录" 仅重置不活跃计时器 (无 Token 刷新) |

### US-AUTH-008: Token 验证

> As a 系统, I want to 验证当前 AccessToken 的有效性,
> so that 可以在操作前确认用户会话仍然有效。

**Acceptance Criteria:**
- [ ] 有效 Token → 返回 200 + valid=true + 剩余有效时间
- [ ] 过期 Token → 返回 401 + ErrorCode=TokenExpired(10201)

**Business Rules:**
1. 检查 Token 是否存在
2. 检查 Token 是否过期
3. 检查 Token 是否即将过期 (5 分钟内)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/auth/validate` |
| 本地 | 不适用 |

### US-AUTH-009: 凭证本地存储

> As a 医生, I want to 安全地保存登录凭证到本地,
> so that 下次启动应用时可以自动登录而不担心凭证被窃取。

**Acceptance Criteria:**
- [ ] 搜索本地文件 → AccessToken/RefreshToken 不出现在任何磁盘文件中
- [ ] AutoLoginToken → DPAPI 加密 + HMAC-SHA256 完整性校验
- [ ] 旧格式凭据 (无 HMAC) 登录成功后 → 自动迁移到新格式

**Business Rules:**
1. Token 严格存储在内存中，不持久化，应用重启 Token 清除
2. AutoLoginToken 使用 DPAPI 加密后存储
3. 支持 "记住用户名" 和 "记住密码" 两个选项
4. 勾选 "记住密码" 时自动勾选 "记住用户名"
5. 登出时清除 AutoLoginToken，可保留用户名
6. 检测旧格式凭据 (无 HMAC)，成功登录后自动迁移到新格式

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | DPAPI 加密 + HMAC 校验 |
| 本地 | 同远程模式 |

### US-AUTH-010: 登录状态机

> As a 系统, I want to 通过状态机管理登录过程的状态转换,
> so that 各模块可以根据认证状态做出正确响应。

**Acceptance Criteria:**
- [ ] 登录成功 → 状态从 Idle → Validating → Active
- [ ] StateChanged 事件 → 包含 PreviousState + CurrentState + 时间戳

**Business Rules:**
1. 状态: Idle → Validating → Active / Idle
2. 刷新: Active → Refreshing → Active
3. 超时: Active → Expired (静默，不显示警告)
4. 登出: 任意状态 → Idle
5. 状态变更触发 StateChanged 事件 (含 PreviousState、CurrentState、时间戳)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 完整状态机 (Idle/Validating/Active/Refreshing/Expired) |
| 本地 | 简化版 (Idle → Active → Idle) |

### US-AUTH-011: Token 刷新失败分级处理

> As a 系统, I want to 根据刷新失败原因采取不同处理策略,
> so that 网络抖动不会导致用户被踢出，而安全威胁能被立即阻断。

**Acceptance Criteria:**
- [ ] 网络错误 → 指数退避重试 (1s/2s/4s，最多 3 次)
- [ ] TokenRevoked → 立即清除 Token + 显示安全提示

**Business Rules:**
1. 网络错误: 指数退避重试 (1秒、2秒、4秒，最多 3 次)
2. TokenExpired: 尝试 AutoLogin
3. TokenRevoked: 立即清除 Token，显示 "会话已在其他设备终止"

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 客户端实现分级处理 |
| 本地 | 不适用 |

### US-AUTH-012: 登录界面

> As a 用户, I want to 在简洁的全屏登录界面输入凭据,
> so that 我可以专注于登录操作，不被其他界面元素干扰。

**Acceptance Criteria:**
- [ ] 登录界面 → 无边框全屏，不可调整大小/拖动
- [ ] 远程模式 API 状态 → 绿 (已连接) / 红 (失败) / 黄 (检查中)

**Business Rules:**
1. 无边框窗口，不可调整大小、不可拖动
2. 登录框右上角关闭按钮 (唯一退出入口)
3. Alt+F4 在登录界面允许退出，在工作台阻止
4. "记住用户名" 和 "记住密码" 水平对齐
5. "记住密码" 后显示警告 "仅在可信设备使用"
6. 远程模式显示 API 状态指示器: 已连接(绿)、失败(红)、检查中(黄)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 显示 API 状态指示器和连接模式 |
| 本地 | 不显示 API 状态指示器 |

### US-AUTH-013: 认证事件体系

> As a 系统模块开发者, I want to 订阅认证生命周期事件,
> so that 我的模块可以在用户登录/登出/Token 刷新时做出响应。

**Acceptance Criteria:**
- [ ] 登录成功 → 触发 LoginSucceeded 事件 (含 UserId/Timestamp)
- [ ] Token 刷新失败 → 触发 TokenRefreshFailed 事件

**Business Rules:**
1. 登录事件: LoginStarted / LoginSucceeded / LoginFailed / AutoLoginAttempted
2. 会话事件: SessionExpiring / SessionExpired / SessionExtended
3. 登出事件: LogoutStarted / LogoutCompleted / ForcedLogout
4. Token 事件: TokenRefreshed / TokenRefreshFailed
5. 每个事件包含标准载荷 (UserId、Timestamp 等)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 完整事件体系 |
| 本地 | 简化版事件 |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| OAuth2/OIDC 第三方登录 | 小型诊所无需，后续版本考虑 |
| 多因素认证 (MFA) | 增加操作复杂度，诊所场景不必要 |
| 生物识别登录 (指纹/面部) | 硬件依赖，超出 v1.0 范围 |
| JWT 黑名单机制 | 复杂度高，延迟踢出 (最长 30 分钟) 在诊所场景可接受 (AUTH-D08) |
| 触摸事件活跃追踪 | WPF 触摸追踪复杂度高，诊所以鼠标键盘为主 |
| 服务端登出失败重试队列 | 重试队列复杂度高非当前优先级，Sprint 后续实现 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| JWT 密钥泄露 | Token 伪造，全系统数据暴露 | DPAPI 加密存储 + 定期密钥轮换 |
| 本地模式无 Token | 安全级别低于远程模式 | 本地密码验证 + 不活跃超时 + 应用级访问控制 |
| AccessToken 延迟踢出 | 旧设备在 Token 有效期内 (最长 30 分钟) 仍可操作 | 诊所场景可接受 (AUTH-D08)，后续版本考虑 JWT 黑名单 |
| 凭据文件被拷贝 | 他机上使用拷贝的凭据文件 | DPAPI LocalMachine 绑定机器，跨机器无法解密 |
| 4 个 PRD 定义事件缺失 | SessionExpiring/SessionExtended/LogoutStarted/ForcedLogout 未实现 | 延期到事件体系 Epic |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-AUTH-01 | 密码过期策略是否启用? | 预留设计 (PasswordExpired 错误码已定义)，v1.0 不启用 |
| OQ-AUTH-02 | validate 端点是否返回剩余有效时间? | 延期。当前返回 valid=true/false，剩余时间 Sprint 后续补充 |
| OQ-AUTH-03 | "记住密码" 安全警告文案是否上线? | 延期。UI 文案非当前优先级，功能本身可用 |
| OQ-AUTH-04 | 后续版本 USB 加密狗认证扩展时机? | ICredentialStore 接口已预留 (AUTH-D11)，待业务需求明确 |

---

## Data Model

### AuthSession (认证会话)

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 会话ID |
| UserId | Guid | 用户ID (FK) |
| TokenHash | string(256) | 会话令牌哈希 |
| LoginTime | DateTime | 登录时间 |
| LogoutTime | DateTime? | 登出时间 |
| ExpiryTime | DateTime | 过期时间 |
| IpAddress | string(45) | IP 地址 |
| UserAgent | string(500)? | 用户代理 |
| IsRevoked | bool | 是否已撤销 |

### RefreshToken (刷新令牌)

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 令牌ID |
| Token | string(512) | 令牌值 (加密，唯一索引) |
| UserId | Guid | 用户ID (FK) |
| Jti | string(128) | JWT ID |
| ExpiresAt | DateTime | 过期时间 |
| IsRevoked | bool | 是否已撤销 |
| FamilyId | string(128)? | Token 家族ID (重放检测) |
| IsUsed | bool | 是否已使用 |
| DeviceId | string(128)? | 设备标识 |

### SecurityAuditLog (安全审计日志)

| 字段 | 类型 | 说明 |
|------|------|------|
| EventType | string(50) | 事件类型 (Login/Logout/RefreshToken 等) |
| UserId | Guid? | 用户ID |
| Success | bool | 操作是否成功 |
| IpAddress | string(50)? | 客户端IP |
| ErrorMessage | string(500)? | 错误消息 |

---

## Configuration

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Jwt:AccessTokenExpirationMinutes` | 30 | AccessToken 有效期 (分钟) |
| `Jwt:RefreshTokenExpirationDays` | 7 | RefreshToken 有效期 (天) |
| `Jwt:ClockSkewSeconds` | 300 | 时钟偏差容限 (秒) |
| `Session:TimeoutMinutes` | 120 | 会话超时 (分钟) |
| `Session:InactivityTimeoutMinutes` | 15 | 不活跃超时 (分钟) |
| `Session:WarningBeforeTimeoutMinutes` | 2 | 警告提前时间 (分钟) |
| `Security:RateLimiting:LoginLimit` | 5次/60秒 | 登录限流 |
| `PasswordPolicy:MinLength` | 8 | 密码最小长度 |

---

## Error Codes

> 认证错误码归入用户模块命名空间 (1xxxx)，与 [users.md](users.md) 统一编号体系。

| 错误码 | 编号 | HTTP | 说明 |
|--------|------|------|------|
| InvalidCredentials | 10101 | 401 | 用户名或密码错误 |
| TokenExpired | 10201 | 401 | AccessToken 已过期 |
| TokenInvalid | 10202 | 401 | Token 格式或签名无效 |
| TokenRevoked | 10203 | 401 | Token 已被撤销 |
| RefreshTokenExpired | 10204 | 401 | RefreshToken 已过期 |
| RefreshTokenInvalid | 10205 | 401 | RefreshToken 无效 |
| UnauthorizedAccess | 10300 | 403 | 权限不足 |

---

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| AUTH-D06 | 单会话登录策略 | US-AUTH-001 | 已确定: 同一账号仅允许一台设备登录。新设备登录时撤销旧设备所有 Token Family，旧设备下次请求/刷新时触发 TokenRevoked → 强制登出 |
| AUTH-D07 | 角色变更即时生效 | US-AUTH-003 + users.md US-USER-004 | 已确定: 用户角色变更时立即撤销该用户 Token Family，强制重登录。通过 `ICrossModuleAuthService.RevokeAllUserTokensAsync()` 实现 Users → Auth 跨模块调用 (ISP 原则) |
| AUTH-D08 | 延迟踢出 (不引入JWT黑名单) | US-AUTH-001 AUTH-D06 | 已确定: 新设备登录撤销旧Token Family后，旧设备在AccessToken有效期内 (最长30分钟) 仍可操作。诊所场景30分钟延迟可接受 |
| AUTH-D09 | 踢出提示统一泛化 | US-AUTH-004 AUTH-D06 | 已确定: 客户端收到 TokenRevoked 统一显示 "您的账号已在其他设备登录，请重新登录"，不区分撤销原因。具体原因记录在 SecurityAuditLog |
| AUTH-D10 | 凭证存储采用 DPAPI LocalMachine + HMAC | US-AUTH-009 | 已确定: DPAPI DataProtectionScope.LocalMachine 加密 + HMAC-SHA256 完整性校验。LocalMachine 作用域不绑定 Windows 用户账号，适合诊所共用电脑场景 |
| AUTH-D11 | 后续版本预留 USB 加密狗扩展 | US-AUTH-009 | 已确定: 凭证存储抽象为 ICredentialStore 接口。v1.0 实现 LocalFileCredentialStore (DPAPI)，后续版本可新增 UsbKeyCredentialStore |
| AUTH-D12 | 并发Token刷新客户端互斥锁 | US-AUTH-003 US-AUTH-011 | 已确定: 客户端使用 SemaphoreSlim(1,1) 保证同一时刻仅一个刷新请求。业界标准 (MSAL/Auth0 SDK/Firebase Auth) |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | 内外网统一限流，移除内部网络限流区分 | 内外网区分增加复杂度，统一限流更简单安全 | AUTH-13 |
| 2026-02-21 | 移除触摸事件活跃追踪 | WPF 触摸追踪复杂度高，诊所以鼠标键盘为主 | AUTH-15 |
| 2026-02-21 | 移除登出前警告功能 | simplify-auth 重构移除超时前警告，仅保留静默登出 | AUTH-02 |
| 2026-02-21 | PRD 状态命名对齐代码 | 确保 Idle/Validating/Active/Refreshing/Expired 与代码一致 | AUTH-19 |
| 2026-02-21 | 移除 AuthSession 独立实体 | 当前 RefreshToken 表已满足会话管理需求 | AUTH-11 |

---

## Operation Flows

### 单会话登录踢出流程 (AUTH-D06 + AUTH-D08)

设备B登录时的完整踢出时序:

1. 设备B → POST /auth/login
2. 服务端验证凭据成功
3. 服务端查询该 UserId 所有未撤销的 RefreshToken → 批量 Revoke(reason="NewDeviceLogin")
4. 服务端撤销该用户所有 AutoLoginToken Family
5. 生成新 Token Family → 返回给设备B
6. 设备A (后续): AccessToken 未过期 → 正常操作 (JWT无状态，延迟踢出)
7. 设备A: AccessToken 过期 → 尝试 Refresh → 401 TokenRevoked
8. 设备A: 尝试 AutoLogin → 401 TokenRevoked
9. 设备A: 显示 "您的账号已在其他设备登录，请重新登录" → 跳转登录页

> 注意: LoginAsync 当前代码未实现步骤3-4 (撤销旧Token Family)，需在实现 AUTH-D06 时补充。

### Token 刷新失败分级处理 (US-AUTH-011)

客户端检测 AccessToken 即将过期 (剩余<5分钟) 时的决策流程:

1. 获取刷新互斥锁 (AUTH-D12)
   - 获取失败 (其他请求正在刷新) → 等待结果 → 用新Token重试
   - 获取成功 → POST /auth/refresh
2. 根据响应分级处理:
   - **200 OK** → 替换Token → 释放锁 → 继续
   - **网络错误/5xx** → 指数退避重试 (1s→2s→4s，最多3次)
     - 重试成功 → 替换Token → 释放锁
     - 3次均失败 → 释放锁 → 保持当前Token → 用户下次操作再触发
   - **401 RefreshTokenExpired (10204)** → 释放锁 → 尝试 AutoLogin
     - AutoLogin 成功 → 获取全新Token
     - AutoLogin 失败 → 跳转登录页
   - **401 TokenRevoked (10203)** → 释放锁 → 立即清除所有本地Token → 显示踢出提示 (AUTH-D09) → 跳转登录页

### 不活跃超时流程 (US-AUTH-006)

1. 登录成功 → 启动 InactivityTimer (默认15分钟，可配置)
2. 用户操作 (键盘/鼠标) → 重置 Timer
3. Timer 到期 → 静默登出:
   - 远程模式: 清除内存Token → POST /auth/logout (Best-effort) → 清除 AutoLoginToken → 保留 "记住用户名" → 跳转登录页
   - 本地模式: 清除本地会话状态 → 保留 "记住用户名" → 跳转登录页
4. 无弹窗警告 (simplify-auth 已移除超时前警告)

### 凭证本地存储流程 (US-AUTH-009 + AUTH-D10)

**写入** (登录成功 + RememberMe=true):
AutoLoginToken → DPAPI Protect (LocalMachine + entropy) → HMAC-SHA256 签名 → 写入 %LOCALAPPDATA%/LYBT/credentials.dat

**读取** (应用启动自动登录):
1. 读取 credentials.dat → 检查 FormatFlags
2. 有HMAC → 验证签名 → 匹配则 DPAPI Unprotect → 获取 AutoLoginToken → POST /auth/auto-login
3. 无HMAC (旧格式) → DPAPI Unprotect → 登录成功后用新格式覆盖 (透明迁移)
4. HMAC不匹配 → 删除文件 + 记录安全警告 + 回退手动登录

**清除** (登出时):
删除 AutoLoginToken → 保留 "记住用户名" (如已勾选) → 保留连接模式设置

---

## Change Log

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 7 个认证 spec + AuthController 代码提取 |
| 2026-02-17 | v1.1 | Round 10: US-AUTH-001 补充单会话登录规则 (AUTH-D06)，新增角色变更即时生效决策 (AUTH-D07) |
| 2026-02-17 | v1.2 | PRD审查修复: A4-本地模式有不活跃超时(防泄露), D2-错误码对齐5位数体系(1xxxx) |
| 2026-02-18 | v1.3 | US-AUTH-007本地模式明确: "保持登录"仅重置计时器(无Token刷新)，验收标准拆分远程/本地 |
| 2026-02-21 | v1.4 | PRD vs Code 偏差分析修订: 5 项修订, 4 项延期标注 |
| 2026-02-21 | v1.5 | Phase 2 模块功能细化: 新增 AUTH-D08~D12，补充 4 个操作流程 |
| 2026-02-22 | v1.6 | Token Family 撤销接口重命名 (A3): ICrossModuleAuthService (ISP 原则) |
| 2026-03-06 | v2.0 | PRD 全面重写: FR→US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节 |
