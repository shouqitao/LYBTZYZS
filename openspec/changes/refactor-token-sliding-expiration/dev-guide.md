# Token滑动过期与自动登出 - 开发者指南

## 概述

本文档说明如何使用Token滑动过期机制相关的服务组件，包括统一定时任务调度服务(`IApplicationTickService`)、用户活动追踪服务(`IUserActivityTracker`)以及新的会话管理行为。

## 核心组件

### 1. IApplicationTickService - 统一定时任务调度服务

统一的应用级定时器服务，取代分散的`DispatcherTimer`实例，提供一致的心跳事件。

#### 接口定义

```csharp
public interface IApplicationTickService
{
    /// <summary>
    /// 定时Tick事件，每秒触发一次
    /// </summary>
    event EventHandler<ApplicationTickEventArgs>? Tick;

    /// <summary>
    /// 累计Tick次数（自服务启动以来）
    /// </summary>
    long TickCount { get; }

    /// <summary>
    /// 服务是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 启动定时器
    /// </summary>
    void Start();

    /// <summary>
    /// 停止定时器
    /// </summary>
    void Stop();
}
```

#### 使用方式

```csharp
public class MyService : IDisposable
{
    private readonly IApplicationTickService _tickService;
    private const int CheckIntervalTicks = 10; // 每10秒执行一次

    public MyService(IApplicationTickService tickService)
    {
        _tickService = tickService;
        _tickService.Tick += OnTick;
    }

    private void OnTick(object? sender, ApplicationTickEventArgs e)
    {
        // 每秒执行的逻辑
        UpdateClock(e.Timestamp);

        // 每N秒执行的逻辑（使用TickCount计数）
        if (e.TickCount % CheckIntervalTicks == 0)
        {
            PerformPeriodicCheck();
        }
    }

    public void Dispose()
    {
        _tickService.Tick -= OnTick;
    }
}
```

#### DI注册

`IApplicationTickService`已在Shell层注册为Singleton:
```csharp
// ServiceCollectionExtensions.cs
containerRegistry.RegisterSingleton<IApplicationTickService, ApplicationTickService>();
```

### 2. IUserActivityTracker - 用户活动追踪服务

追踪用户输入活动（鼠标/键盘），实现会话超时检测。

#### 接口定义

```csharp
public interface IUserActivityTracker
{
    /// <summary>
    /// 最后一次用户活动时间
    /// </summary>
    DateTime LastActivityTime { get; }

    /// <summary>
    /// 用户当前是否活跃（未超时）
    /// </summary>
    bool IsUserActive { get; }

    /// <summary>
    /// 距离会话过期的剩余时间
    /// </summary>
    TimeSpan TimeUntilInactive { get; }

    /// <summary>
    /// 是否正在追踪
    /// </summary>
    bool IsTracking { get; }

    /// <summary>
    /// 会话即将过期事件（提前警告）
    /// </summary>
    event EventHandler<SessionExpiringEventArgs>? SessionExpiring;

    /// <summary>
    /// 会话已过期事件
    /// </summary>
    event EventHandler? SessionExpired;

    void StartTracking();
    void StopTracking();
    void ResetActivity();
}
```

#### 使用方式

```csharp
public class MainWindowViewModel
{
    private readonly IUserActivityTracker _activityTracker;

    public MainWindowViewModel(IUserActivityTracker activityTracker)
    {
        _activityTracker = activityTracker;
        _activityTracker.SessionExpiring += OnSessionExpiring;
        _activityTracker.SessionExpired += OnSessionExpired;
    }

    private void OnSessionExpiring(object? sender, SessionExpiringEventArgs e)
    {
        // 显示警告对话框
        var result = MessageBox.Show(
            $"会话即将在 {e.TimeRemaining.TotalMinutes:F0} 分钟后过期，是否继续?",
            "会话提醒",
            MessageBoxButton.YesNo);

        if (result == MessageBoxResult.Yes)
        {
            _activityTracker.ResetActivity(); // 重置活动计时器
        }
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        // 执行自动登出
        PerformLogoutAsync();
    }
}
```

### 3. IUserActivityState - Foundation层接口

`IUserActivityState`是一个轻量级接口，定义在`LYBT.Desktop.Contracts`层，供Foundation层组件（如`TokenRefreshHandler`）查询用户活跃状态，避免循环依赖。

```csharp
public interface IUserActivityState
{
    /// <summary>
    /// 用户当前是否活跃
    /// </summary>
    bool IsUserActive { get; }

    /// <summary>
    /// 重置活动计时器
    /// </summary>
    void ResetActivity();
}
```

`UserActivityTracker`同时实现`IUserActivityTracker`和`IUserActivityState`接口。

## 配置选项

会话管理相关配置位于`LybtOptions`:

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `InactivityTimeoutMinutes` | 15 | 用户不活跃超时时间（分钟） |
| `WarningBeforeTimeoutMinutes` | 2 | 超时前警告提前量（分钟） |
| `ActivityCheckIntervalSeconds` | 60 | 活动检查间隔（秒） |

## Token滑动过期机制

### 工作原理

1. **用户活跃检测**: `UserActivityTracker`通过`InputManager.Current.PreProcessInput`监听所有WPF输入事件
2. **定时检查**: 每60秒检查一次用户是否在`InactivityTimeoutMinutes`内有活动
3. **Token刷新决策**: `TokenRefreshHandler`在Token即将过期时，仅在用户活跃状态下才执行刷新
4. **刷新后重置**: Token刷新成功后，调用`ResetActivity()`重置活动计时器

### 流程图

```
[用户操作] --> [InputManager捕获] --> [LastActivityTime更新]
                                           |
[Tick事件(每秒)] --> [检查IsUserActive] --> |
                                           v
                            [Token即将过期?] -- 是 --> [IsUserActive?]
                                                          |
                                               是 --------+-------- 否
                                               |                    |
                                        [刷新Token]            [跳过刷新]
                                               |
                                        [ResetActivity]
                                               |
                                     [继续原始请求]
```

## 迁移指南

### 从独立DispatcherTimer迁移

**之前**:
```csharp
private DispatcherTimer _timer;

public void Initialize()
{
    _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
    _timer.Tick += OnTimer;
    _timer.Start();
}
```

**之后**:
```csharp
private readonly IApplicationTickService _tickService;

public MyService(IApplicationTickService tickService)
{
    _tickService = tickService;
    _tickService.Tick += OnTick;
}

private void OnTick(object? sender, ApplicationTickEventArgs e)
{
    // 处理逻辑
}
```

### 注意事项

1. **Tick间隔**: `IApplicationTickService`固定为1秒间隔，如需不同间隔，使用`TickCount`计数
2. **UI线程**: Tick事件在UI线程触发，可直接操作UI元素
3. **异常处理**: 事件处理器异常会被记录但不会中断服务运行
4. **生命周期**: 服务在Shell启动时自动Start，关闭时自动Stop

## 测试

相关测试用例位于:
- `tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Services/ApplicationTickServiceTests.cs`
- `tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Services/UserActivityTrackerTests.cs`
- `tests/IntegrationTests/Client/Desktop/LYBT.Desktop.Foundation.IntegrationTests/Http/TokenRefreshHandlerIntegrationTests.cs`

运行测试:
```bash
dotnet test --filter "FullyQualifiedName~ApplicationTickServiceTests or FullyQualifiedName~UserActivityTrackerTests or FullyQualifiedName~TokenRefreshHandlerIntegrationTests"
```
