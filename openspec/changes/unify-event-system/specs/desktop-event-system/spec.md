# Desktop Event System Spec Delta

## MODIFIED Requirements

### Requirement: EVENT-001 跨模块事件通信必须使用PubSubEvent

Desktop层跨模块事件通信MUST统一使用Prism PubSubEvent模式，禁止使用EventHandler进行跨模块通信。

#### Scenario: 登录状态变更通知
- Given: 用户完成登录操作
- When: LoginStateMachine状态变更为LoggedIn
- Then: 通过AuthEvents.LoginStateChangedEvent发布状态变更
- And: 所有订阅者通过IEventAggregator接收通知

#### Scenario: Token刷新结果通知
- Given: TokenRefreshHandler执行Token刷新
- When: 刷新成功或失败
- Then: 通过TokenEvents发布对应事件
- And: 不再发布EventHandler事件

### Requirement: EVENT-002 事件Payload必须使用record类型

所有PubSubEvent的Payload MUST定义为record类型，并且MUST包含Timestamp属性。

#### Scenario: 创建新事件Payload
- Given: 需要定义新的事件载荷
- When: 创建Payload类型
- Then: 必须使用record关键字定义
- And: 必须包含DateTime Timestamp属性
- And: 核心数据属性使用required修饰符

### Requirement: EVENT-003 相关事件必须聚合到静态类

相关领域的事件MUST聚合到命名的静态类中，而非独立定义。

#### Scenario: 认证相关事件
- Given: 需要发布登录/登出/密码变更事件
- When: 定义事件类
- Then: 必须定义在AuthEvents静态类中
- And: 事件类命名为{Action}Event

#### Scenario: 患者相关事件
- Given: 需要发布患者创建/更新/选择事件
- When: 定义事件类
- Then: 必须定义在PatientEvents静态类中

## REMOVED Requirements

### Requirement: EVENT-004 禁止双轨事件发布

组件MUST NOT同时发布EventHandler事件和PubSubEvent事件（兼容模式）。

#### Scenario: 移除LoginStateMachine兼容模式
- Given: LoginStateMachine当前同时发布StateChanged(EventHandler)和AuthEvents.LoginStateChangedEvent
- When: 完成事件统一
- Then: 移除StateChanged EventHandler事件
- And: 仅保留PubSubEvent发布

#### Scenario: 移除LogoutService兼容模式
- Given: LogoutService当前同时发布EventHandler和PubSubEvent
- When: 完成事件统一
- Then: 移除ServerLogoutFailed和PendingLogoutsCleared EventHandler事件

## ADDED Requirements

### Requirement: EVENT-005 TokenEvents事件聚合类

系统MUST提供TokenEvents静态类聚合所有Token相关事件。

#### Scenario: Token刷新成功事件
- Given: TokenRefreshHandler刷新Token成功
- When: 需要通知订阅者
- Then: 发布TokenEvents.RefreshSucceededEvent
- And: Payload包含NewExpiresAt时间

#### Scenario: Token生命周期变更事件
- Given: TokenLifecycleService检测到Token状态变化
- When: 状态从Active变为Warning或Expired
- Then: 发布TokenEvents.LifecycleChangedEvent
- And: Payload包含PreviousState和CurrentState

### Requirement: EVENT-006 PatientEvents事件聚合类

系统MUST提供PatientEvents静态类聚合所有患者相关事件。

#### Scenario: 患者创建事件
- Given: 用户成功创建新患者
- When: 需要通知其他模块刷新
- Then: 发布PatientEvents.CreatedEvent
- And: Payload包含PatientDetailDto

### Requirement: EVENT-007 CaseEvents事件聚合类

系统MUST提供CaseEvents静态类聚合所有医案相关事件。

#### Scenario: 会诊完成事件
- Given: 医生完成会诊记录
- When: 保存成功
- Then: 发布CaseEvents.ConsultationCompletedEvent
- And: Payload包含MedicalCaseId和ConsultationId
