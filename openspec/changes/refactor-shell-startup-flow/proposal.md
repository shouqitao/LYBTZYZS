# Change: 重构Shell启动到登录到主页的完整流程

## Why

当前Shell启动流程存在以下问题：

1. **职责分散且重叠**: `App.xaml.cs`、`MainWindowViewModel`、`ApplicationBootstrapper`、`NavigationManager`等多个类处理启动逻辑，职责边界不清晰
2. **MainWindowViewModel过于臃肿**: 17+依赖注入，同时处理登录流程、Token生命周期、会话管理、模块加载等多个关注点
3. **异步初始化流程混乱**: SplashScreen → 异步服务初始化 → 显示窗口 → 登录 → 模块加载，缺乏清晰的状态机
4. **错误处理分散**: 各处有不同的错误处理逻辑，难以统一管理
5. **测试困难**: 启动流程紧耦合，难以单元测试各个阶段

## What Changes

### 架构重构

- **引入`IApplicationLifecycle`状态机**: 明确定义启动阶段（Initializing → Authenticating → Ready → Running）
- **拆分MainWindowViewModel职责**:
  - `ShellViewModel`: 仅负责Shell布局和导航
  - `LoginCoordinator`: 专门处理登录流程
  - `SessionManager`: 管理会话状态和Token生命周期
- **简化App.xaml.cs**: 仅负责容器配置和生命周期启动
- **统一初始化管道**: 使用Pipeline模式处理初始化步骤

### 流程简化

- **移除ApplicationBootstrapper**: 职责合并到`IApplicationLifecycle`
- **简化NavigationManager**: 仅保留Region导航核心功能
- **统一错误处理**: 使用`IStartupErrorHandler`集中处理启动错误

### 新增能力

- **启动进度反馈**: 可配置的启动进度回调
- **启动诊断**: 记录各阶段耗时，便于性能分析
- **优雅降级**: 非关键服务失败时允许继续启动

## Impact

- **Affected specs**: 新建 `shell-startup` spec
- **Affected code**:
  - `src/Client/Shell/App.xaml.cs` - 简化
  - `src/Client/Shell/ViewModels/MainWindowViewModel.cs` - 拆分重构
  - `src/Client/Shell/Services/ApplicationBootstrapper.cs` - 移除
  - `src/Client/Shell/Services/NavigationManager.cs` - 简化
  - 新增多个服务类

## 最佳实践参考

基于WPF/Prism最佳实践研究：

1. **Microsoft官方建议**:
   - 延迟初始化：非关键服务延迟到首页显示后
   - 避免应用配置文件：使用注册表或简单INI替代
   - 优化模块加载：使用on-demand加载非核心模块

2. **Prism官方模式**:
   - `PrismApplication`生命周期钩子：`OnInitialized`、`CreateShell`、`InitializeShell`
   - 模块按需加载：`startupLoaded="false"`
   - 容器延迟初始化：`Lazy<T>`和`AsyncLazy<T>`

3. **社区最佳实践**:
   - SplashScreen与Bootstrapper分离
   - 登录流程在Shell创建前完成（Brian Lagunas模式）
   - 模块加载进度反馈

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 重构范围大 | 中 | 分Phase实施，每Phase可独立验证 |
| 回归风险 | 中 | 完善的集成测试覆盖 |
| 向后兼容 | 低 | 内部重构，对外接口不变 |
