## ADDED Requirements

### Requirement: REQ-STARTUP-001 应用生命周期状态机

系统 SHALL 通过`IApplicationLifecycle`状态机管理应用启动流程，定义明确的状态转换。

**Acceptance Criteria:**
- 定义5个状态：NotStarted, Initializing, Authenticating, Ready, Running
- 状态转换必须按顺序进行，不允许跳跃
- 提供状态变化的可观察接口
- 状态转换失败时保持当前状态

#### Scenario: 正常启动状态转换
- **GIVEN** 应用处于NotStarted状态
- **WHEN** 启动流程开始
- **THEN** 状态依次转换为 Initializing → Authenticating → Ready → Running
- **AND** 每次状态变化触发StateChanges事件

#### Scenario: 状态转换失败
- **GIVEN** 应用处于Initializing状态
- **AND** 初始化过程发生错误
- **WHEN** 尝试转换到Authenticating
- **THEN** 状态保持Initializing
- **AND** 返回失败结果

---

### Requirement: REQ-STARTUP-002 启动管道步骤化

系统 SHALL 使用管道模式执行启动步骤，每个步骤可配置优先级和必要性。

**Acceptance Criteria:**
- 启动步骤实现`IStartupStep`接口
- 步骤按Order属性排序执行
- 必要步骤（IsRequired=true）失败时终止启动
- 非必要步骤失败时记录警告并继续

#### Scenario: 按顺序执行启动步骤
- **GIVEN** 注册了多个启动步骤
- **AND** 步骤Order分别为10, 20, 30
- **WHEN** 启动管道执行
- **THEN** 步骤按Order升序依次执行

#### Scenario: 必要步骤失败终止启动
- **GIVEN** 存在一个IsRequired=true的步骤
- **WHEN** 该步骤执行失败
- **THEN** 启动流程终止
- **AND** 显示错误信息给用户

#### Scenario: 非必要步骤失败继续启动
- **GIVEN** 存在一个IsRequired=false的步骤
- **WHEN** 该步骤执行失败
- **THEN** 记录警告日志
- **AND** 启动流程继续执行后续步骤

---

### Requirement: REQ-STARTUP-003 会话管理服务

系统 SHALL 通过`ISessionManager`集中管理用户会话状态和Token生命周期。

**Acceptance Criteria:**
- 提供当前会话状态查询
- 管理AccessToken和RefreshToken生命周期
- Token过期前发出警告
- Token刷新失败时触发重新登录

#### Scenario: Token即将过期警告
- **GIVEN** 用户已登录
- **AND** AccessToken将在5分钟内过期
- **WHEN** Token监控检测到即将过期
- **THEN** 自动刷新Token
- **AND** 用户无感知

#### Scenario: Token刷新失败
- **GIVEN** 用户已登录
- **AND** AccessToken已过期
- **WHEN** RefreshToken刷新失败
- **THEN** 会话状态变为已过期
- **AND** 导航到登录界面

---

### Requirement: REQ-STARTUP-004 登录流程协调

系统 SHALL 通过`ILoginCoordinator`协调登录流程，包括凭证验证、模块加载和导航。

**Acceptance Criteria:**
- 协调登录成功后的后续操作
- 根据用户角色加载对应模块
- 登录成功后导航到默认首页
- 处理登录失败场景

#### Scenario: 登录成功流程
- **GIVEN** 用户在登录界面
- **AND** 输入有效凭证
- **WHEN** 登录验证成功
- **THEN** 保存会话信息
- **AND** 根据角色加载模块
- **AND** 导航到工作台首页

#### Scenario: 角色模块加载
- **GIVEN** 用户登录成功
- **AND** 用户角色为临床医生
- **WHEN** 加载用户模块
- **THEN** 加载Clinical模块
- **AND** 不加载Admin模块

---

### Requirement: REQ-STARTUP-005 启动诊断

系统 SHALL 记录各启动阶段的耗时，便于性能分析和问题定位。

**Acceptance Criteria:**
- 记录每个启动步骤的开始和结束时间
- 记录状态转换的时间戳
- 诊断信息通过日志输出
- 支持启动性能报告生成

#### Scenario: 启动耗时日志
- **GIVEN** 应用启动
- **WHEN** 启动完成
- **THEN** 日志包含各步骤耗时
- **AND** 日志包含总启动时间

#### Scenario: 慢启动检测
- **GIVEN** 某启动步骤执行超过3秒
- **WHEN** 步骤完成
- **THEN** 记录警告日志
- **AND** 标记该步骤为慢步骤

---

### Requirement: REQ-STARTUP-006 Shell职责单一化

MainWindowViewModel SHALL 仅负责Shell布局和顶部工具栏，不包含登录和会话管理逻辑。

**Acceptance Criteria:**
- MainWindowViewModel仅包含布局相关属性
- 登录逻辑由LoginCoordinator处理
- 会话管理由SessionManager处理
- ViewModel依赖数量不超过8个

#### Scenario: ViewModel职责边界
- **GIVEN** MainWindowViewModel实例化
- **WHEN** 检查其依赖
- **THEN** 不包含IAuthService
- **AND** 不包含Token相关服务
- **AND** 依赖数量不超过8个

#### Scenario: 顶部工具栏更新
- **GIVEN** 用户已登录
- **WHEN** 会话状态变化
- **THEN** MainWindowViewModel通过事件接收通知
- **AND** 更新顶部工具栏显示
