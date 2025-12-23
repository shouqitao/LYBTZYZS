# login-state-machine Specification

## Purpose

定义登录状态机规范，使用有限状态机模式管理登录生命周期，提高可测试性和可维护性。

## ADDED Requirements

### Requirement: LSM-001 登录状态定义

系统 **SHALL** 使用有限状态机管理登录状态，定义清晰的状态和转换规则。

#### Scenario: 状态枚举定义

- **GIVEN** 系统需要管理登录状态
- **WHEN** 定义状态机
- **THEN** 包含以下状态：
  - Idle: 未登录，等待用户操作
  - Validating: 正在验证凭据
  - Refreshing: 正在刷新Token
  - Active: 已登录，会话活跃
  - Expiring: 会话即将过期
  - Expired: 会话已过期

#### Scenario: 初始状态

- **GIVEN** 应用程序启动
- **WHEN** 状态机初始化
- **THEN** 初始状态为Idle

---

### Requirement: LSM-002 状态转换规则

系统 **SHALL** 定义明确的状态转换规则，防止非法状态转换。

#### Scenario: Idle到Validating

- **GIVEN** 当前状态为Idle
- **WHEN** 用户提交登录凭据
- **THEN** 状态转换为Validating

#### Scenario: Validating到Active

- **GIVEN** 当前状态为Validating
- **WHEN** 登录验证成功
- **THEN** 状态转换为Active

#### Scenario: Validating到Idle（失败）

- **GIVEN** 当前状态为Validating
- **WHEN** 登录验证失败
- **THEN** 状态转换为Idle
- **AND** 触发LoginFailed事件

#### Scenario: Active到Expiring

- **GIVEN** 当前状态为Active
- **AND** 配置的警告提前时间为2分钟
- **WHEN** 会话将在2分钟内过期
- **THEN** 状态转换为Expiring
- **AND** 触发SessionExpiring事件

#### Scenario: Active到Refreshing

- **GIVEN** 当前状态为Active
- **WHEN** Token即将过期需要刷新
- **THEN** 状态转换为Refreshing

#### Scenario: Refreshing到Active

- **GIVEN** 当前状态为Refreshing
- **WHEN** Token刷新成功
- **THEN** 状态转换为Active

#### Scenario: Expiring到Expired

- **GIVEN** 当前状态为Expiring
- **AND** 用户未响应保持登录
- **WHEN** 会话超时
- **THEN** 状态转换为Expired
- **AND** 触发SessionExpired事件

#### Scenario: Expired到Idle

- **GIVEN** 当前状态为Expired
- **WHEN** 系统清理会话
- **THEN** 状态转换为Idle
- **AND** 导航到登录页面

#### Scenario: 任意状态到Idle（登出）

- **GIVEN** 当前状态为Active/Expiring/Refreshing
- **WHEN** 用户执行登出
- **THEN** 状态转换为Idle
- **AND** 触发LogoutCompleted事件

---

### Requirement: LSM-003 状态变更通知

系统 **SHALL** 在状态变更时发送通知，允许其他组件响应。

#### Scenario: 状态变更事件

- **GIVEN** 状态机发生状态转换
- **WHEN** 从StateA转换到StateB
- **THEN** 触发StateChanged事件
- **AND** 事件包含PreviousState和CurrentState
- **AND** 事件包含转换时间戳

#### Scenario: 订阅状态变更

- **GIVEN** 组件需要响应登录状态变化
- **WHEN** 组件订阅StateChanged事件
- **THEN** 每次状态变更都收到通知
- **AND** 可以根据新状态执行相应逻辑

---

### Requirement: LSM-004 可靠登出

系统 **SHALL** 确保登出操作可靠执行，即使网络不可用也能完成本地登出。

#### Scenario: 本地登出优先

- **GIVEN** 用户执行登出操作
- **WHEN** 系统处理登出
- **THEN** 立即清除本地Token
- **AND** 立即清除本地会话状态
- **AND** 状态机转换到Idle

#### Scenario: 服务端登出尝试

- **GIVEN** 本地登出完成
- **WHEN** 系统尝试通知服务端
- **THEN** 调用服务端logout API
- **AND** 撤销RefreshToken

#### Scenario: 服务端登出失败处理

- **GIVEN** 服务端登出请求失败
- **AND** 失败原因是网络问题
- **WHEN** 系统处理失败
- **THEN** 将登出请求加入待处理队列
- **AND** 下次网络恢复时重试
- **AND** 用户已完成本地登出（不阻塞）

#### Scenario: 离线登出队列处理

- **GIVEN** 存在待处理的服务端登出请求
- **WHEN** 网络恢复且用户重新登录
- **THEN** 在后台处理队列中的登出请求
- **AND** 不影响当前登录会话

---

## Related Specs

- authentication (AUTH-002 不活跃自动登出)
- authentication (AUTH-003 登出前警告)
- authentication (AUTH-009 Logout后强制重新登录)
