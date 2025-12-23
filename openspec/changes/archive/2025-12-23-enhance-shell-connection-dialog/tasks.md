# Tasks: enhance-shell-connection-dialog

## Phase 1: 基础设施 (Infrastructure)

### Task 1.1: 创建RecoveryAction枚举
- **File**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/RecoveryAction.cs`
- **Description**: 定义API连接恢复操作类型枚举
- **Validation**: 编译通过

### Task 1.2: 创建IApiConnectionRecoveryService接口
- **File**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IApiConnectionRecoveryService.cs`
- **Description**: 定义连接恢复服务接口
- **Validation**: 编译通过

### Task 1.3: 扩展IStartupPipeline接口
- **File**: `src/Client/Desktop/Shell/Services/Startup/IStartupPipeline.cs`
- **Description**: 添加Reset()方法定义
- **Validation**: 编译通过

### Task 1.4: 实现StartupPipeline.Reset()
- **File**: `src/Client/Desktop/Shell/Services/Startup/StartupPipeline.cs`
- **Description**: 实现管道重置逻辑，清除已执行步骤状态
- **Validation**: 编译通过

---

## Phase 2: 对话框实现 (Dialog Implementation)

### Task 2.1: 创建ApiConnectionFailedDialogViewModel
- **File**: `src/Client/Desktop/Shell/Dialogs/ViewModels/ApiConnectionFailedDialogViewModel.cs`
- **Description**: 实现对话框ViewModel，包含:
  - ErrorSummary, PossibleReasons, TechnicalDetails属性
  - IsDetailsExpanded, IsOfflineModeEnabled属性
  - RetryCommand, OfflineModeCommand, ViewLogsCommand, ExitCommand命令
  - IDialogAware接口实现
- **Dependencies**: Task 1.1
- **Validation**: 编译通过

### Task 2.2: 创建ApiConnectionFailedDialog视图
- **File**: `src/Client/Desktop/Shell/Dialogs/Views/ApiConnectionFailedDialog.xaml`
- **File**: `src/Client/Desktop/Shell/Dialogs/Views/ApiConnectionFailedDialog.xaml.cs`
- **Description**: 实现对话框UI:
  - 警告图标+标题
  - 错误摘要文本
  - 可能原因列表(ItemsControl)
  - 可展开的技术详情(Expander)
  - 按钮区([离线模式] [查看日志] [重试] [退出])
- **Dependencies**: Task 2.1
- **Validation**: 设计器无错误

### Task 2.3: 注册对话框到容器
- **File**: `src/Client/Desktop/Shell/App.xaml.cs`
- **Description**: 在RegisterTypes中注册对话框
- **Dependencies**: Task 2.2
- **Validation**: 编译通过

---

## Phase 3: 服务实现 (Service Implementation)

### Task 3.1: 实现ApiConnectionRecoveryService
- **File**: `src/Client/Desktop/Shell/Services/ApiConnectionRecoveryService.cs`
- **Description**: 实现IApiConnectionRecoveryService:
  - 注入IDialogService
  - 构建DialogParameters(errorMessage, exception, apiEndpoint)
  - 调用ShowDialogAsync显示对话框
  - 从对话框结果提取RecoveryAction
- **Dependencies**: Task 2.3
- **Validation**: 编译通过

### Task 3.2: 注册服务到容器
- **File**: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- **Description**: 注册IApiConnectionRecoveryService
- **Dependencies**: Task 3.1
- **Validation**: 编译通过

---

## Phase 4: 启动流程集成 (Startup Integration)

### Task 4.1: 修改App.InitializeApplicationAsync
- **File**: `src/Client/Desktop/Shell/App.xaml.cs`
- **Description**: 修改启动流程:
  - 检测API健康检查失败
  - 调用IApiConnectionRecoveryService
  - 根据RecoveryAction执行重试/退出
  - 支持循环重试
- **Dependencies**: Task 3.2
- **Validation**: 编译通过

### Task 4.2: 更新StartupStepResult
- **File**: `src/Client/Desktop/Shell/Services/Startup/StartupStepResult.cs`
- **Description**: 确保Exception属性可用于传递详细错误信息
- **Validation**: 编译通过

---

## Phase 5: 测试验证 (Testing & Validation)

### Task 5.1: 手动测试 - 连接失败场景
- **Steps**:
  1. 关闭WebAPI服务
  2. 启动Desktop应用
  3. 验证显示ApiConnectionFailedDialog
  4. 验证对话框内容(标题、错误信息、可能原因)
- **Validation**: 对话框正确显示

### Task 5.2: 手动测试 - 重试成功场景
- **Steps**:
  1. 关闭WebAPI后启动应用(显示对话框)
  2. 启动WebAPI服务
  3. 点击[重试]按钮
  4. 验证成功进入主界面
- **Validation**: 重试后正常启动

### Task 5.3: 手动测试 - 退出场景
- **Steps**:
  1. 关闭WebAPI后启动应用(显示对话框)
  2. 点击[退出]按钮
  3. 验证应用正常退出
- **Validation**: 应用退出码为1

### Task 5.4: 手动测试 - 查看日志
- **Steps**:
  1. 显示对话框状态
  2. 点击[查看日志]按钮
  3. 验证打开logs文件夹
- **Validation**: 文件资源管理器打开正确目录

### Task 5.5: 手动测试 - 离线模式按钮状态
- **Steps**:
  1. 显示对话框
  2. 验证[离线模式(v2.0)]按钮为禁用状态
  3. 验证ToolTip显示"离线模式将在v2.0版本中启用"
- **Validation**: 按钮禁用，ToolTip正确

### Task 5.6: 手动测试 - 展开详情
- **Steps**:
  1. 显示对话框
  2. 点击"展开详情"
  3. 验证显示服务地址、错误类型、详细信息
- **Validation**: 详情内容正确

---

## Phase 6: 文档更新 (Documentation)

### Task 6.1: 更新Shell README
- **File**: `src/Client/Desktop/Shell/README.md`
- **Description**: 添加ApiConnectionRecoveryService和对话框说明
- **Validation**: 文档格式正确

### Task 6.2: 更新CHANGELOG
- **File**: `CHANGELOG.md`
- **Description**: 添加变更记录
- **Validation**: 格式符合规范

---

## Dependency Graph

```
Phase 1 (基础设施)
  Task 1.1 ──┬──▶ Task 2.1
  Task 1.2 ──┤
  Task 1.3 ──┼──▶ Task 1.4 ──▶ Task 4.1
  Task 1.4 ──┘

Phase 2 (对话框)
  Task 2.1 ──▶ Task 2.2 ──▶ Task 2.3

Phase 3 (服务)
  Task 2.3 ──▶ Task 3.1 ──▶ Task 3.2

Phase 4 (集成)
  Task 3.2 ──┬──▶ Task 4.1
  Task 1.4 ──┘
  Task 4.2 (独立)

Phase 5 (测试)
  Task 4.1 ──▶ Task 5.1 ~ 5.6 (可并行)

Phase 6 (文档)
  Task 5.x ──▶ Task 6.1, 6.2 (可并行)
```

## Parallelization Opportunities

| 可并行任务组 | 任务 |
|-------------|------|
| Phase 1 | Task 1.1, 1.2, 1.3 可并行 |
| Phase 5 | Task 5.1 ~ 5.6 可并行执行 |
| Phase 6 | Task 6.1, 6.2 可并行 |

## Estimated Effort

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| Phase 1 | 4 | - |
| Phase 2 | 3 | - |
| Phase 3 | 2 | - |
| Phase 4 | 2 | - |
| Phase 5 | 6 | - |
| Phase 6 | 2 | - |
| **Total** | **19** | - |
