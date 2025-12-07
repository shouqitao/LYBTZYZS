# Change: Shell层架构整合与优化

## Why

近期Shell层经历了多个独立变更(refactor-shell-startup-flow、remove-titlebar-add-close-button、remove-statusbar-relocate-status)，导致:
1. 规范分散且部分Purpose仍为TBD，缺乏统一的Shell层架构规范
2. MainWindowViewModel职责过重，健康检查逻辑与核心UI逻辑耦合
3. ApplicationBootstrapper与StartupPipeline存在职责重叠

需要系统性整合这些变更，形成一致的架构规范和更清晰的代码组织。

## What Changes

### 规范整合
- 更新`login-ui`规范Purpose(当前为TBD)
- 更新`shell-layout`规范Purpose(当前为TBD)
- 添加Shell层架构概述规范，整合生命周期、登录、会话管理

### 代码优化
- 提取`HealthCheckCoordinator`从`MainWindowViewModel`，减少约50行代码
- 清理`ApplicationBootstrapper`中的废弃方法标记
- 统一`StartupDiagnostics`诊断日志收集

## Impact

- Affected specs: `login-ui`, `shell-layout`
- Affected code:
  - `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
  - `src/Client/Desktop/Shell/Services/Bootstrap/ApplicationBootstrapper.cs`
  - `src/Client/Desktop/Shell/Services/Diagnostics/StartupDiagnostics.cs`
  - 新增: `src/Client/Desktop/Shell/Services/HealthCheck/HealthCheckCoordinator.cs`

## 非破坏性变更

本提案不引入任何破坏性变更:
- 不改变任何公共API
- 不改变用户界面行为
- 仅内部代码重组和规范文档完善
