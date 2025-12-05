# Shell启动流程重构 - 技术设计

## Context

当前启动流程涉及多个类，职责边界模糊：

```
当前流程:
App.OnStartup
  → SplashScreen.Show
  → base.OnStartup (Prism)
    → CreateShell → MainWindow
    → InitializeShell → Hide Window
    → OnInitialized
      → InitializeApplicationAsync
        → ModuleCoordinator
        → CoreServices
        → ApiHealthCheck
        → Warmup
      → Show MainWindow
      → MainWindowViewModel.Initialize
        → Show LoginView
        → LoginSuccess Event
        → LoadModules
        → Navigate to Home
```

问题：
1. `MainWindowViewModel`承担过多职责（登录、Token、会话、导航）
2. `ApplicationBootstrapper`与`App.xaml.cs`职责重叠
3. 缺乏明确的状态机定义
4. 错误处理分散

## Goals / Non-Goals

### Goals
- 清晰的启动状态机，每个状态职责明确
- 单一职责原则：每个类只做一件事
- 可测试性：各阶段可独立测试
- 启动性能可观测：各阶段耗时可测量

### Non-Goals
- 不改变用户可见的UI流程
- 不改变模块加载机制（Prism IModuleCatalog）
- 不改变Token验证逻辑（保持现有安全机制）

## Decisions

### 1. 引入ApplicationLifecycle状态机

```
状态转换:
[NotStarted]
    → [Initializing] (容器、核心服务)
    → [Authenticating] (显示登录界面、等待登录)
    → [Ready] (登录成功、加载模块)
    → [Running] (正常运行)
    → [ShuttingDown] (退出)
```

```csharp
public interface IApplicationLifecycle
{
    ApplicationState CurrentState { get; }
    IObservable<ApplicationState> StateChanges { get; }

    Task<bool> TransitionToAsync(ApplicationState targetState);
    void RegisterStateHandler(ApplicationState state, Func<Task> handler);
}

public enum ApplicationState
{
    NotStarted,
    Initializing,
    Authenticating,
    Ready,
    Running,
    ShuttingDown
}
```

**理由**: 状态机模式使启动流程可预测、可测试、易于调试

### 2. 拆分MainWindowViewModel职责

| 新类 | 职责 | 从MainWindowViewModel提取 |
|------|------|--------------------------|
| `ShellViewModel` | Shell布局、Region管理、顶部栏 | 保留布局相关属性和命令 |
| `LoginCoordinator` | 登录流程编排 | `OnLoginSuccessAsync`等 |
| `SessionManager` | 会话状态、Token生命周期 | Token监控、会话过期处理 |
| `ModuleLoadingService` | 模块加载进度 | 角色模块加载逻辑 |

**理由**: 单一职责原则，便于测试和维护

### 3. 简化启动入口

```csharp
// 重构后的App.xaml.cs
public partial class App : PrismApplication
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 仅显示SplashScreen
        var splash = new SplashScreen("Resources/splash.png");
        splash.Show(false);

        base.OnStartup(e);
    }

    protected override async void OnInitialized()
    {
        // 启动生命周期状态机
        var lifecycle = Container.Resolve<IApplicationLifecycle>();
        await lifecycle.TransitionToAsync(ApplicationState.Running);
    }

    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册核心服务
        containerRegistry.RegisterSingleton<IApplicationLifecycle, ApplicationLifecycle>();
        containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
        containerRegistry.RegisterSingleton<ILoginCoordinator, LoginCoordinator>();
        // ... 其他注册
    }
}
```

**理由**: App.xaml.cs仅负责Prism启动和容器配置

### 4. 初始化管道模式

```csharp
public interface IStartupStep
{
    string Name { get; }
    int Order { get; }
    bool IsRequired { get; }
    Task ExecuteAsync(IProgress<string> progress);
}

// 实现示例
public class CoreServicesStartupStep : IStartupStep
{
    public string Name => "初始化核心服务";
    public int Order => 10;
    public bool IsRequired => true;

    public async Task ExecuteAsync(IProgress<string> progress)
    {
        progress.Report("正在初始化日志服务...");
        // ...
    }
}
```

**理由**: Pipeline模式使初始化步骤可配置、可扩展

## Risks / Trade-offs

| 风险 | 影响 | 缓解 |
|------|------|------|
| 重构范围大 | 高 | 分Phase实施，每Phase独立可验证 |
| 临时并存两套代码 | 中 | 使用Feature Flag控制新旧流程 |
| 状态机复杂度 | 低 | 状态数量有限（5个），转换规则简单 |

## Migration Plan

### Phase 1: 基础设施（不改变现有行为）
1. 实现`IApplicationLifecycle`状态机
2. 实现`ISessionManager`（从MainWindowViewModel提取）
3. 添加启动诊断日志

### Phase 2: 登录流程重构
1. 实现`ILoginCoordinator`
2. 重构`LoginViewModel`使用`ILoginCoordinator`
3. 移除MainWindowViewModel中的登录逻辑

### Phase 3: 启动管道重构
1. 实现`IStartupStep`管道
2. 迁移ApplicationBootstrapper逻辑
3. 移除ApplicationBootstrapper

### Phase 4: ShellViewModel精简
1. 创建精简版`ShellViewModel`
2. 移除MainWindowViewModel冗余代码
3. 更新Shell XAML绑定

### 回滚计划
- 每Phase完成后进行集成测试
- 保留旧代码直到Phase完成
- 使用Git tag标记每个Phase完成点

## Open Questions

1. **SplashScreen是否需要自定义WPF窗口？**
   - 当前使用.NET内置SplashScreen（简单图片）
   - 如需进度条，需要自定义窗口
   - 建议：保持简单，使用内置方案

2. **状态机是否需要持久化？**
   - 当前场景不需要
   - 如需断点恢复，可考虑持久化

3. **是否引入Rx.NET管理状态变化？**
   - 优点：统一的事件流处理
   - 缺点：增加依赖
   - 建议：仅在需要复杂事件组合时引入
