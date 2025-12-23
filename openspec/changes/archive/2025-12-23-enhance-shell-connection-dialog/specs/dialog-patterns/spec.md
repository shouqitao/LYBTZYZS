# Spec Delta: dialog-patterns

## ADDED Requirements

### Requirement: DLG-006 Startup Connection Recovery Dialog

启动时API连接失败 MUST 通过专用恢复对话框处理，而非直接退出。

**规范**:
- 使用`ApiConnectionFailedDialog`显示连接错误
- 提供[重试]、[查看日志]、[退出]操作按钮
- 预留[离线模式]入口(v2.0启用)
- 错误信息采用简洁模式，技术详情可展开

#### Scenario: API health check fails during startup
- **GIVEN** 应用启动时执行API健康检查
- **WHEN** 健康检查失败(连接超时、服务不可用等)
- **THEN** 显示`ApiConnectionFailedDialog`
- **AND** 对话框标题为"无法连接到服务器"
- **AND** 显示友好的错误摘要和可能原因列表
- **AND** 提供[重试]按钮(IsDefault=true)
- **AND** 提供[退出]按钮(IsCancel=true)
- **AND** NOT 使用`MessageBox.Show()`

#### Scenario: User clicks Retry button
- **GIVEN** 显示ApiConnectionFailedDialog
- **WHEN** 用户点击[重试]按钮
- **THEN** 对话框关闭并返回`RecoveryAction.Retry`
- **AND** 启动管道重新执行API健康检查步骤
- **AND** 如果成功则继续正常启动流程

#### Scenario: User clicks Exit button
- **GIVEN** 显示ApiConnectionFailedDialog
- **WHEN** 用户点击[退出]按钮
- **THEN** 对话框关闭并返回`RecoveryAction.Exit`
- **AND** 应用程序以退出码1终止

#### Scenario: User clicks View Logs button
- **GIVEN** 显示ApiConnectionFailedDialog
- **WHEN** 用户点击[查看日志]按钮
- **THEN** 打开logs文件夹(文件资源管理器)
- **AND** 对话框保持显示状态

#### Scenario: Offline mode button state in v1.0
- **GIVEN** 当前版本为v1.0
- **WHEN** 显示ApiConnectionFailedDialog
- **THEN** [离线模式(v2.0)]按钮为禁用状态(IsEnabled=false)
- **AND** 按钮ToolTip显示"离线模式将在v2.0版本中启用"

### Requirement: DLG-007 Error Information Display Pattern

连接错误对话框 MUST 采用分层信息展示模式。

**规范**:
- L1标题层：简洁的错误标题
- L2摘要层：用户友好的错误描述
- L3原因层：可能原因列表(3-4项)
- L4详情层：技术详情(可展开)

#### Scenario: Display layered error information
- **GIVEN** 需要显示连接错误
- **WHEN** 构建对话框内容
- **THEN** 标题显示"无法连接到服务器"
- **AND** 摘要显示"无法连接到凌隐宝堂服务，请检查："
- **AND** 原因列表显示:
  - WebAPI服务是否已启动
  - 网络连接是否正常
  - 防火墙是否阻止连接
- **AND** 详情区包含服务地址、错误类型、详细信息
- **AND** 详情区默认折叠(Expander.IsExpanded=false)

#### Scenario: Expand technical details
- **GIVEN** 对话框已显示
- **WHEN** 用户点击"展开详情"
- **THEN** 显示技术详情区域
- **AND** 内容使用等宽字体(Consolas)
- **AND** 包含:
  - 服务地址: {apiEndpoint}
  - 错误类型: {exception.GetType().Name}
  - 详细信息: {exception.Message}

## MODIFIED Requirements

### Requirement: DLG-005 No Direct MessageBox

ViewModel中 MUST NOT 直接使用MessageBox.Show。

**规范**:
- MessageBox.Show 在ViewModel中禁止使用
- 使用 `IDialogCoordinator` 或 `IUserNotification` 替代
- View code-behind中也应避免

**新增规范** (enhance-shell-connection-dialog):
- 应用启动时API连接失败使用`ApiConnectionFailedDialog`处理
- DI容器初始化之前的致命错误仍允许使用MessageBox

#### Scenario: Replace MessageBox with DialogCoordinator
- **GIVEN** 现有代码使用 `MessageBox.Show`
- **WHEN** 重构代码
- **THEN** 替换为 `_dialogCoordinator.ShowConfirmationAsync` 或类似方法
- **AND** 确保异步调用模式

#### Scenario: Startup API connection failure
- **GIVEN** 应用启动时发生API连接错误
- **WHEN** API健康检查失败
- **THEN** 使用`ApiConnectionFailedDialog`处理
- **AND** NOT 使用`MessageBox.Show()`
- **EXCEPTION** DI容器初始化之前的致命错误仍允许使用MessageBox
