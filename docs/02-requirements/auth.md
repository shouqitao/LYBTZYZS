# 认证与会话管理 需求规格

## 概述

认证模块负责用户身份验证、会话管理和安全控制。采用 JWT Bearer Token 机制，支持密码登录、自动登录 (AutoLoginToken)、Token 滑动刷新、不活跃超时登出，并内置重放攻击检测机制。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | 登录/登出/Token 刷新/会话超时延长 |
| Admin | 登录/登出/Token 刷新/会话超时延长 |
| Doctor | 登录/登出/Token 刷新/会话超时延长 |
| Receptionist | 登录/登出/Token 刷新/会话超时延长 |

> 认证操作本身不区分角色，所有已注册用户均可使用。

---

## 功能清单

### FR-AUTH-001: 用户登录

- **描述**: 用户通过用户名和密码进行身份验证，获取 JWT Token
- **业务规则**:
  1. 验证用户名和密码匹配
  2. 验证用户状态为 Enabled
  3. 登录成功返回 AccessToken + RefreshToken + 用户信息
  4. 登录失败累计 FailedLoginCount
  5. 登录端点限流: 5次/60秒 (内部网络 20次/60秒)
  6. **单会话登录: 登录时撤销该用户所有现有 Token Family，旧设备下次请求/刷新时强制登出** (AUTH-D06)
- **远程模式**: POST `/api/v1/auth/login`，服务端验证凭据，返回 JWT Token
- **本地模式**: LocalAuthService 本地验证，不生成 JWT
- **验收标准**:
  - [ ] 正确凭据 -> 返回 200 + AccessToken + RefreshToken + UserDetailDto
  - [ ] 错误凭据 -> 返回 401 + ErrorCode=InvalidCredentials(101)
  - [ ] 禁用用户登录 -> 返回 403 + ErrorCode=UserDisabled
  - [ ] 60秒内第6次登录 -> 返回 429 Too Many Requests

### FR-AUTH-002: 自动登录

- **描述**: 使用 AutoLoginToken 实现免密码自动登录
- **业务规则**:
  1. AutoLoginToken 由服务端生成，仅在 RememberMe=true 时返回
  2. AutoLoginToken 使用 DPAPI 加密存储在本地
  3. 存储时计算 HMAC-SHA256 完整性校验值
  4. 读取时验证 HMAC，失败则删除凭据并记录安全警告
  5. 服务端可随时撤销 AutoLoginToken
- **远程模式**: POST `/api/v1/auth/auto-login`，发送 UserName + AutoLoginToken
- **本地模式**: 不支持。本地模式无 Token 机制，不提供自动登录功能。每次启动应用需手动输入用户名和密码登录
- **验收标准**:
  - [ ] 保存凭据后应用重启 -> 自动登录成功，获取新 Token
  - [ ] AutoLoginToken 被撤销后 -> 自动登录返回 401，跳转手动登录
  - [ ] HMAC 校验失败 -> 自动清除凭据 + 记录安全警告日志

### FR-AUTH-003: Token 刷新

- **描述**: 在 AccessToken 即将过期时使用 RefreshToken 获取新 Token
- **业务规则**:
  1. 仅在用户活跃时执行 Token 刷新 (滑动过期)
  2. RefreshToken 有效期 7 天
  3. 使用 FamilyId 追踪 Token 家族
  4. 每个 RefreshToken 仅可使用一次 (IsUsed 标记)
  5. 绝对过期时间限制: 30 天会话期限
- **远程模式**: POST `/api/v1/auth/refresh`，返回新 AccessToken + 新 RefreshToken
- **本地模式**: 不需要 (无 JWT)
- **验收标准**:
  - [ ] AccessToken 剩余<5分钟 -> 自动使用 RefreshToken 获取新 Token
  - [ ] RefreshToken 过期 -> 返回 401，尝试 AutoLogin 或跳转手动登录
  - [ ] 用户不活跃超过15分钟 -> 不触发刷新，等待用户操作后再刷新

### FR-AUTH-004: 重放攻击检测

- **描述**: 检测 RefreshToken 被重复使用的安全威胁
- **业务规则**:
  1. RefreshToken 使用后标记 IsUsed=true
  2. 已使用的 RefreshToken 再次提交，判定为重放攻击
  3. 重放攻击时使整个 Token Family (同 FamilyId) 失效
  4. 返回 ErrorCode=TokenRevoked
  5. 客户端收到 TokenRevoked 后立即清除 Token，显示"会话已在其他设备终止"
- **远程模式**: 服务端检测并处理
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 重复使用已标记 IsUsed 的 RefreshToken -> 同 FamilyId 所有 Token 失效
  - [ ] 客户端收到 ErrorCode=TokenRevoked -> 清除 Token + 显示"会话已在其他设备终止"

### FR-AUTH-005: 用户登出

- **描述**: 用户主动退出登录，清理本地和服务端会话
- **业务规则**:
  1. 本地登出优先: 立即清除内存中的 Token
  2. 服务端登出: 调用 logout API 撤销 RefreshToken
  3. 服务端失败时加入待处理队列，网络恢复时重试
  4. 登出后必须重新输入密码，不支持会话恢复
  5. 允许使用过期 Token 进行登出操作
  6. 必须提供 RefreshToken 或 UserName
- **远程模式**: POST `/api/v1/auth/logout`
- **本地模式**: 清除本地会话状态
- **验收标准**:
  - [ ] 登出后使用旧 AccessToken 请求 -> 返回 401
  - [ ] 登出后即使保存了密码 -> 需重新输入密码登录 (不支持会话恢复)
  - [ ] 网络断开时登出 -> 本地立即清除 Token，服务端登出加入重试队列

### FR-AUTH-006: 不活跃超时

- **描述**: 用户长时间不操作时自动终止会话
- **业务规则**:
  1. 追踪键盘输入、鼠标操作、触摸事件
  2. 超过不活跃超时时间后静默登出 (不显示警告)
  3. 默认超时: 15 分钟，可配置
- **远程模式**: 客户端检测不活跃，触发登出流程
- **本地模式**: 同远程模式 (本地也需要安全控制)
- **验收标准**:
  - [ ] 15 分钟无键盘/鼠标/触摸操作 -> 静默登出，跳转登录界面
  - [ ] 任何键盘/鼠标操作 -> 重置不活跃计时器

### FR-AUTH-007: 登出前警告

- **描述**: 在即将超时登出前弹出警告对话框
- **业务规则**:
  1. 在超时前 2 分钟显示警告 (可配置)
  2. 用户可选"保持登录" (刷新 Token) 或"立即登出"
  3. 不操作则到期后自动登出
- **远程模式**: 弹出警告对话框，选择保持登录时刷新 Token
- **本地模式**: 同远程模式
- **验收标准**:
  - [ ] 超时前 2 分钟 -> 弹出警告对话框 (保持登录/立即登出)
  - [ ] 点击"保持登录" -> 刷新 Token + 重置超时计时器

### FR-AUTH-008: Token 验证

- **描述**: 验证当前 AccessToken 的有效性
- **业务规则**:
  1. 检查 Token 是否存在
  2. 检查 Token 是否过期
  3. 检查 Token 是否即将过期 (5 分钟内)
- **远程模式**: GET `/api/v1/auth/validate`
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 有效 Token -> 返回 200 + valid=true + 剩余有效时间
  - [ ] 过期 Token -> 返回 401 + ErrorCode=TokenExpired(201)

### FR-AUTH-009: 凭证本地存储

- **描述**: 安全地存储和管理登录凭证
- **业务规则**:
  1. Token 严格存储在内存中，不持久化，应用重启 Token 清除
  2. AutoLoginToken 使用 DPAPI 加密后存储
  3. 支持"记住用户名"和"记住密码"两个选项
  4. 勾选"记住密码"时自动勾选"记住用户名"
  5. 登出时清除 AutoLoginToken，可保留用户名
  6. 检测旧格式凭据 (无 HMAC)，成功登录后自动迁移到新格式
- **远程模式**: DPAPI 加密 + HMAC 校验
- **本地模式**: 同远程模式
- **验收标准**:
  - [ ] 搜索本地文件 -> AccessToken/RefreshToken 不出现在任何磁盘文件中
  - [ ] AutoLoginToken -> DPAPI 加密 + HMAC-SHA256 完整性校验
  - [ ] 旧格式凭据 (无HMAC) 登录成功后 -> 自动迁移到新格式

### FR-AUTH-010: 登录状态机

- **描述**: 管理登录过程的状态转换
- **业务规则**:
  1. 状态: Idle → Validating → Active / Idle
  2. 刷新: Active → Refreshing → Active
  3. 超时: Active → Expired (静默，不显示警告)
  4. 登出: 任意状态 → Idle
  5. 状态变更触发 StateChanged 事件 (含 PreviousState、CurrentState、时间戳)
- **远程模式**: 完整状态机
- **本地模式**: 简化版 (Idle → Active → Idle)
- **验收标准**:
  - [ ] 登录成功 -> 状态从 Idle → Validating → Active
  - [ ] StateChanged 事件 -> 包含 PreviousState + CurrentState + 时间戳

### FR-AUTH-011: Token 刷新失败分级处理

- **描述**: 根据刷新失败原因采取不同处理策略
- **业务规则**:
  1. 网络错误: 指数退避重试 (1秒、2秒、4秒，最多 3 次)
  2. TokenExpired: 尝试 AutoLogin
  3. TokenRevoked: 立即清除 Token，显示"会话已在其他设备终止"
- **远程模式**: 客户端实现分级处理
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 网络错误 -> 指数退避重试 (1s/2s/4s，最多3次)
  - [ ] TokenRevoked -> 立即清除 Token + 显示安全提示

### FR-AUTH-012: 登录界面

- **描述**: 无边框全屏登录界面
- **业务规则**:
  1. 无边框窗口，不可调整大小、不可拖动
  2. 登录框右上角关闭按钮 (唯一退出入口)
  3. Alt+F4 在登录界面允许退出，在工作台阻止
  4. "记住用户名"和"记住密码"水平对齐
  5. "记住密码"后显示警告"仅在可信设备使用"
  6. 远程模式显示 API 状态指示器: 已连接(绿)、失败(红)、检查中(黄)
- **远程模式**: 显示 API 状态指示器和连接模式
- **本地模式**: 不显示 API 状态指示器
- **验收标准**:
  - [ ] 登录界面 -> 无边框全屏，不可调整大小/拖动
  - [ ] 远程模式 API 状态 -> 绿(已连接)/红(失败)/黄(检查中)

### FR-AUTH-013: 认证事件体系

- **描述**: 发布认证生命周期中的各类事件，供其他模块订阅
- **业务规则**:
  1. 登录事件: LoginStarted / LoginSucceeded / LoginFailed / AutoLoginAttempted
  2. 会话事件: SessionExpiring / SessionExpired / SessionExtended
  3. 登出事件: LogoutStarted / LogoutCompleted / ForcedLogout
  4. Token 事件: TokenRefreshed / TokenRefreshFailed
  5. 每个事件包含标准载荷 (UserId、Timestamp 等)
- **远程模式**: 完整事件体系
- **本地模式**: 简化版事件
- **验收标准**:
  - [ ] 登录成功 -> 触发 LoginSucceeded 事件 (含 UserId/Timestamp)
  - [ ] Token 刷新失败 -> 触发 TokenRefreshFailed 事件

---

## 数据模型

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

## 配置参数

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

## 错误码

| 错误码 | 编号 | HTTP | 说明 |
|--------|------|------|------|
| InvalidCredentials | 101 | 401 | 用户名或密码错误 |
| TokenExpired | 201 | 401 | AccessToken 已过期 |
| TokenInvalid | 202 | 401 | Token 格式或签名无效 |
| TokenRevoked | 203 | 401 | Token 已被撤销 |
| RefreshTokenExpired | 204 | 401 | RefreshToken 已过期 |
| RefreshTokenInvalid | 205 | 401 | RefreshToken 无效 |
| UnauthorizedAccess | 300 | 403 | 权限不足 |

---

## 决策记录

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 本地模式下自动登录的实现方式 | FR-AUTH-002 | 已确定: 不支持 (无 Token 机制，每次手动登录) |
| 2 | 本地模式下的会话超时策略 | FR-AUTH-006 | 已确定: 不适用。本地模式无 Token 超时，登录状态持续到应用退出。安全保障: 5次失败锁定15分钟 |
| AUTH-D06 | 单会话登录策略 | FR-AUTH-001 | 已确定: 同一账号仅允许一台设备登录。新设备登录时撤销旧设备所有 Token Family，旧设备下次请求/刷新时触发 TokenRevoked → 强制登出 |
| AUTH-D07 | 角色变更即时生效 | FR-AUTH-003 + users.md FR-USER-004 | 已确定: 用户角色变更时立即撤销该用户 Token Family，强制重登录。复用 AUTH-D06 的 Token Family 撤销逻辑 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 7 个认证 spec + AuthController 代码提取 |
| 2026-02-17 | v1.1 | Round 10: FR-AUTH-001 补充单会话登录规则 (AUTH-D06)，新增角色变更即时生效决策 (AUTH-D07) |
