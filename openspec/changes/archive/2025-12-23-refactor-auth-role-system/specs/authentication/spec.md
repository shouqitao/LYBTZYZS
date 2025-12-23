# Authentication Spec Delta

## ADDED Requirements

### Requirement: 统一认证状态机
系统 SHALL 使用单一的 `AuthenticationStateMachine` 管理所有认证状态转换，替代原有的双状态机架构。

#### Scenario: 登录状态转换
- **WHEN** 用户提交登录凭证
- **THEN** 状态机从 `Idle` 转换到 `Authenticating`
- **AND** 认证成功后依次转换到 `ValidatingToken` -> `LoadingProfile` -> `LoadingModules` -> `Authenticated`

#### Scenario: 认证失败状态转换
- **WHEN** 任意认证步骤失败
- **THEN** 状态机转换到 `Failed` 状态
- **AND** 触发统一错误处理流程

#### Scenario: 登出状态转换
- **WHEN** 用户请求登出
- **THEN** 状态机转换到 `LoggingOut`
- **AND** 清理完成后转换到 `Idle`

### Requirement: Token家族追踪
系统 SHALL 为每个登录会话创建 Token 家族，用于追踪和验证 RefreshToken 的使用。

#### Scenario: 创建Token家族
- **WHEN** 用户成功登录
- **THEN** 系统创建新的 TokenFamily 记录
- **AND** 记录 FamilyId、UserId、DeviceFingerprint、当前 RefreshToken

#### Scenario: Token轮换
- **WHEN** 客户端使用 RefreshToken 请求新 Token
- **THEN** 系统验证 RefreshToken 是否为当前有效 Token
- **AND** 生成新的 AccessToken 和 RefreshToken
- **AND** 更新 TokenFamily 的 CurrentRefreshToken

#### Scenario: 重放攻击检测
- **WHEN** 客户端使用已被轮换的旧 RefreshToken
- **THEN** 系统检测到重放攻击
- **AND** 撤销整个 Token 家族
- **AND** 记录安全审计日志
- **AND** 强制用户重新登录

### Requirement: 设备绑定验证
系统 SHALL 在 Token 刷新时验证设备指纹，防止 Token 跨设备滥用。

#### Scenario: 设备指纹匹配
- **WHEN** 客户端请求刷新 Token
- **THEN** 系统验证请求的设备指纹与 TokenFamily 记录匹配
- **AND** 匹配成功则继续刷新流程

#### Scenario: 设备指纹不匹配
- **WHEN** 请求的设备指纹与 TokenFamily 记录不匹配
- **THEN** 系统拒绝刷新请求
- **AND** 返回设备验证失败错误
- **AND** 记录安全审计日志

### Requirement: Token黑名单机制
系统 SHALL 支持主动撤销 Token，用于强制登出和安全响应场景。

#### Scenario: 强制登出
- **WHEN** 管理员对用户执行强制登出操作
- **THEN** 系统将用户所有 TokenFamily 标记为已撤销
- **AND** 将相关 Token 加入黑名单
- **AND** 用户下次请求时返回认证失败

#### Scenario: 黑名单验证
- **WHEN** 系统收到带有 AccessToken 的请求
- **THEN** 首先检查 Token 是否在黑名单中
- **AND** 黑名单中的 Token 立即拒绝，无需完整验证

### Requirement: 统一认证错误处理
系统 SHALL 通过 `AuthenticationErrorHandler` 集中处理所有认证错误，提供一致的用户体验。

#### Scenario: 凭证错误
- **WHEN** 用户提供错误的用户名或密码
- **THEN** 系统返回统一的"用户名或密码错误"消息
- **AND** 不泄露具体是用户名还是密码错误

#### Scenario: 账户锁定
- **WHEN** 用户账户因多次失败被锁定
- **THEN** 系统返回"账户已锁定，请X分钟后重试"消息
- **AND** 包含解锁时间信息

#### Scenario: Token过期
- **WHEN** AccessToken 已过期且无法刷新
- **THEN** 系统返回"登录已过期，请重新登录"消息
- **AND** 清理本地会话状态

## MODIFIED Requirements

### Requirement: AccessToken有效期
系统生成的 AccessToken 有效期 SHALL 符合安全最佳实践。

#### Scenario: Token有效期配置
- **WHEN** 系统生成 AccessToken
- **THEN** 有效期设置为 15 分钟（从原 30 分钟调整）

#### Scenario: 静默刷新
- **WHEN** AccessToken 距离过期不足 2 分钟
- **THEN** 客户端自动发起静默刷新
- **AND** 用户无感知地获取新 Token

### Requirement: 全异步认证流程
所有认证操作 MUST 使用纯 async/await 模式，禁止同步阻塞。

#### Scenario: 异步登录
- **WHEN** 用户发起登录请求
- **THEN** 整个登录流程使用 async/await
- **AND** 不使用 `.Wait()`、`.Result` 或 `Task.Run` 包装同步代码

#### Scenario: 异步Token刷新
- **WHEN** 系统需要刷新 Token
- **THEN** 刷新操作完全异步
- **AND** 不阻塞 UI 线程

## REMOVED Requirements

### Requirement: LoginStateMachine
原有的 `LoginStateMachine` 类被移除，由 `AuthenticationStateMachine` 替代。

**Reason**: 与 `LoginFlowState` 职责重叠，导致状态同步问题
**Migration**: 所有依赖 `LoginStateMachine` 的代码迁移到 `AuthenticationStateMachine`

### Requirement: LoginFlowState
原有的 `LoginFlowState` 枚举被移除。

**Reason**: 被新的 `AuthState` 枚举替代
**Migration**: 使用 `AuthenticationStateMachine.AuthState` 替代
