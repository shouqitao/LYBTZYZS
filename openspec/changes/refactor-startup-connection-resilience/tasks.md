# refactor-startup-connection-resilience Tasks

## Overview

- **变更类型**: Refactor
- **风险等级**: Medium (影响启动流程和登录页，不影响核心业务)
- **设计文档**: [design.md](./design.md)

## Phase 1: 清理旧架构 + 增强状态服务 + 重构UI

### 1.1 修改 ApiHealthCheckStartupStep.IsRequired 为 false
- **文件**: `src/Client/Desktop/Shell/Services/Startup/Steps/ApiHealthCheckStartupStep.cs`
- **变更**: `public bool IsRequired => true;` → `public bool IsRequired => false;`
- **验证**: 编译通过，启动管道不再阻塞于API不可达

### 1.2 简化 App.xaml.cs 启动流程
- **文件**: `src/Client/Desktop/Shell/App.xaml.cs`
- **变更**:
  - 移除 `while(true)` 循环重试逻辑
  - 移除 `HandleApiConnectionFailureAsync()` 方法
  - 移除对 `IApiConnectionRecoveryService` 的引用
  - 移除 `RegisterTypes` 中的 `ApiConnectionFailedDialog` 对话框注册
  - 简化为线性流程: ExecuteAsync → ShowMainWindow
- **验证**: App.xaml.cs 不再包含 while循环、RecoveryService引用、Dialog注册

### 1.3 新增 ApiStatusChangedEventArgs
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Application/ApiStatusChangedEventArgs.cs` (新建)
- **变更**: 创建事件参数类，包含 IsHealthy, ConnectionStatus, LastError, CheckTime 属性
- **验证**: 文件存在，编译通过

### 1.4 增强 IApplicationStateService 接口
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Application/IApplicationStateService.cs`
- **变更**:
  - 新增 `event EventHandler<ApiStatusChangedEventArgs>? StatusChanged;`
  - 新增 `string? LastError { get; set; }`
- **验证**: 接口包含新成员

### 1.5 增强 ApplicationStateService 实现
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Application/ApplicationStateService.cs`
- **变更**:
  - 实现 StatusChanged 事件 + LastError 属性
  - 在 `CheckApiHealthAsync()` 每个分支中: 状态变化时触发 StatusChanged
- **验证**: StatusChanged 在每次健康检查后触发

### 1.6 增强 HealthCheckCoordinator -- 同步到 ApplicationStateService
- **文件**: `src/Client/Desktop/Shell/Services/HealthCheck/HealthCheckCoordinator.cs`
- **变更**:
  - 构造函数新增 `IApplicationStateService` 参数
  - `CheckNowAsync()` 完成后同步状态到 ApplicationStateService
  - 设置 IsApiHealthy/ConnectionStatus/LastError → 触发 StatusChanged
- **依赖**: 1.4+1.5 完成后执行
- **验证**: HealthCheckCoordinator 每次检查后同步状态到 ApplicationStateService

### 1.7 重构 LoginViewModel -- 移除ConnectionMode，改为事件驱动
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`
- **变更**:
  - 移除: `_connectionSettingsService`, `_connectionMode` 字段
  - 移除: `ConnectionMode`, `IsRemoteModeSelected`, `IsLocalModeSelected`, `ConnectionModeDisplay` 属性
  - 移除: `UpdateConnectionStatus()` 方法
  - 移除构造函数参数: `IConnectionSettingsService?`
  - 移除 `ExecuteLoginAsync()` 中 `ConnectionMode.Local` 分支
  - 新增: 构造函数中订阅 `_applicationStateService.StatusChanged` 事件
  - 新增: `OnApiStatusChanged()` 处理器，通过Dispatcher更新UI
- **依赖**: 1.4+1.5 完成后执行
- **验证**: LoginViewModel 无 ConnectionMode 引用，订阅 StatusChanged

### 1.8 重构 LoginView.xaml -- Banner替换ConnectionMode
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Views/LoginView.xaml`
- **变更**:
  - 删除: 连接模式RadioButton区域
  - 替换为: API连接状态Banner (三色: 绿Healthy/蓝Checking/橙Unhealthy)
  - 保留: 状态文本 + 重试按钮(仅Unhealthy可见)
- **依赖**: 1.7 完成后执行
- **验证**: LoginView 无 RadioButton，Banner 正确展示三色状态

### 1.9 删除8个废弃文件
- **文件列表**:
  1. `src/Client/Desktop/Shell/Dialogs/Views/ApiConnectionFailedDialog.xaml`
  2. `src/Client/Desktop/Shell/Dialogs/Views/ApiConnectionFailedDialog.xaml.cs`
  3. `src/Client/Desktop/Shell/Dialogs/ViewModels/ApiConnectionFailedDialogViewModel.cs`
  4. `src/Client/Desktop/Shell/Services/ApiConnectionRecoveryService.cs`
  5. `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IApiConnectionRecoveryService.cs`
  6. `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/RecoveryAction.cs`
  7. `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Models/ConnectionMode.cs`
  8. `src/Client/Desktop/Modules/LYBT.Desktop.Auth/Services/ConnectionSettingsService.cs`
- **依赖**: 1.2+1.7 完成后执行（确保无引用）
- **验证**: 文件已删除，Grep确认无残留引用

### 1.10 清理 ServiceCollectionExtensions.cs 注册
- **文件**: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- **变更**:
  - 移除 IApiConnectionRecoveryService 注册
  - 移除 IConnectionSettingsService 注册（如存在）
  - 移除 ApiConnectionRecoveryService Logger注册（如存在）
- **依赖**: 1.9 完成后执行
- **验证**: ServiceCollectionExtensions 无已删除服务的注册

### 1.11 编译验证
- 运行 `dotnet build` Desktop解决方案
- 确保零编译错误
- Grep确认无对已删除类型的引用

## Phase 2: 更新OpenSpec规范

### 2.1 更新 dialog-patterns/spec.md
- **文件**: `openspec/specs/dialog-patterns/spec.md`
- **变更**:
  - DLG-006: 重写为"启动时API连接失败直接进入登录页，登录页内联显示连接状态"
  - DLG-007: 更新分层信息展示应用于LoginView内联区域
- **验证**: 规范文件已更新

### 2.2 最终编译验证 + 功能验收

## Dependencies

```
1.1 ─┐
1.2 ─┤
1.3 ─┤
1.4 ─┼──> 1.6 ──┐
1.5 ─┘    1.7 ──┼──> 1.9 ──> 1.10 ──> 1.11
          1.8 ──┘
```

关键依赖:
- 1.3-1.5 (ApplicationStateService增强) 必须在 1.6 和 1.7 之前
- 1.7 (LoginViewModel) 必须在 1.8 (LoginView) 之前
- 1.9 (删除文件) 必须在 1.2+1.7 之后（确保无引用）
- 1.10 (清理注册) 必须在 1.9 之后

## Validation Checklist

- [ ] Desktop解决方案编译通过
- [ ] 启动流程：API可用时正常启动进入登录页
- [ ] 启动流程：API不可用时仍能进入登录页，Banner显示Unhealthy
- [ ] 登录页：连接状态Banner正确显示三种状态
- [ ] 登录页：重试按钮可触发重新检查
- [ ] 登录页：API恢复后Banner自动更新(10秒内)
- [ ] 登录页：API不可达时登录给出友好提示
- [ ] 无残留ConnectionMode UI或逻辑
- [ ] Grep确认无对已删除类型的引用

---

**生成时间**: 2026-01-30
**状态**: 完整版 (已完成设计阶段细化)
