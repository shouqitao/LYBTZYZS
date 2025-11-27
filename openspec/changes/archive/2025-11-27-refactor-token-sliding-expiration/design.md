# 技术设计: Token滑动过期与自动登出

## Context
当前系统使用JWT Bearer认证,Access Token 15分钟过期,Refresh Token 7天过期。`TokenRefreshHandler`在API调用时自动刷新Token(过期前5分钟触发)。问题是这种机制无法区分:
- 用户实际在操作应用
- 后台定时任务/心跳调用

需要实现真正的"用户不活跃则自动登出"语义。

**现有Timer分散问题**: 代码库中存在多个独立的DispatcherTimer(时钟更新、健康检查、状态消息清除等),缺乏统一管理。本次重构同时引入统一的定时任务调度服务。

## Goals
- 用户不操作超过配置时间(默认15分钟)后自动登出
- 用户持续操作时Token保持有效(滑动过期)
- 登出前提供警告,给用户保存工作的机会
- 最小化性能开销,不影响UI响应
- **统一管理应用内所有定时任务**

## Non-Goals
- 服务端强制Token失效(本次仅实现客户端自动登出)
- Refresh Token轮换(可作为后续增强)
- 跨设备会话管理

## Decisions

### D0: 统一定时任务调度 (新增)
**Decision**: 创建`IApplicationTickService`统一管理所有定时任务,使用单一DispatcherTimer

**Rationale**:
- 资源优化: 多个Timer合并为一个,减少系统资源占用
- 统一生命周期: 集中管理启动/停止,避免Timer泄漏
- 可测试: Mock单一服务即可测试所有定时逻辑
- 可扩展: 新功能只需订阅Tick事件

**Current scattered Timers to consolidate**:
| 位置 | 用途 | 当前间隔 |
|------|------|---------|
| MainWindowViewModel._clockTimer | 时钟更新 | 1秒 |
| MainWindowViewModel._healthCheckTimer | API健康检查 | 10秒 |
| UserExperienceService._feedbackTimer | 状态消息清除 | 3秒 |
| GlobalStatusBar.SystemTimeProvider | 时钟更新 | 1秒 |
| (新增) UserActivityTracker | 不活跃检测 | 60秒 |

**统一方案**:
- 基础Tick间隔: 1秒(满足时钟精度需求)
- 各订阅者内部维护自己的计数器,按需执行(如健康检查每10次Tick执行一次)

### D1: 用户活动追踪方式
**Decision**: 使用WPF的`InputManager.Current.PreProcessInput`事件监听所有输入事件

**Rationale**:
- 低开销: 只记录最后活动时间戳,不处理具体事件内容
- 全覆盖: 捕获所有键盘、鼠标、触摸输入
- WPF原生: 无需额外依赖

**Alternatives considered**:
- 全局Hook(user32.dll): 侵入性强,跨进程监听不必要
- 每个控件单独监听: 代码侵入大,容易遗漏
- Timer轮询UI状态: 无法检测用户实际输入

### D2: 不活跃检测机制
**Decision**: 订阅IApplicationTickService,每60个Tick(60秒)检查一次用户活动状态

**Rationale**:
- 粒度适中: 60秒检查足够精确,开销可忽略
- 与UI线程兼容: DispatcherTimer在UI线程执行,可安全操作UI
- 统一管理: 不再单独创建Timer

### D3: 与TokenRefreshHandler的协作
**Decision**: UserActivityTracker提供`IsUserActive`属性,TokenRefreshHandler在刷新前检查

**Rationale**:
- 解耦: 活动追踪与Token刷新职责分离
- 可测试: 可Mock IUserActivityTracker进行单元测试
- 向后兼容: 现有刷新逻辑基本不变,只增加活跃检查

### D4: 警告对话框时机
**Decision**: 在不活跃时间达到(InactivityTimeout - 2分钟)时显示警告

**Rationale**:
- 给用户2分钟时间响应
- 用户任意操作即重置计时器

## Component Design

```
┌─────────────────────────────────────────────────────────────┐
│                        Shell                                 │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              MainWindowViewModel                      │   │
│  │  - 注入IUserActivityTracker                          │   │
│  │  - 注入IApplicationTickService                       │   │
│  │  - 订阅Tick事件(时钟更新、健康检查)                  │   │
│  │  - 处理SessionExpiring/SessionExpired事件            │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure                             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              ApplicationTickService (核心)              │ │
│  │  - 单一DispatcherTimer (1秒间隔)                       │ │
│  │  - 触发Tick事件供订阅者使用                            │ │
│  │  - 统一生命周期管理                                    │ │
│  └────────────────────────────────────────────────────────┘ │
│                              │                               │
│         ┌────────────────────┼────────────────────┐         │
│         ▼                    ▼                    ▼         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │UserActivity  │  │HealthCheck  │  │UserExperience    │   │
│  │Tracker       │  │Subscriber   │  │Service           │   │
│  │- 监听Input   │  │- 每10Tick   │  │- 每3Tick清除     │   │
│  │- 每60Tick检查│  │- 检查API    │  │- 状态消息        │   │
│  └──────────────┘  └──────────────┘  └──────────────────┘   │
│         │                                                    │
│         ▼                                                    │
│  ┌────────────────────────────┐                             │
│  │    TokenRefreshHandler     │                             │
│  │  - 检查IsUserActive        │                             │
│  │  - 仅活跃时刷新Token       │                             │
│  └────────────────────────────┘                             │
│                                                              │
│  Events:                                                     │
│  - Tick: 每秒触发,所有定时任务订阅                          │
│  - SessionExpiring: 即将过期警告                            │
│  - SessionExpired: 已过期,需登出                            │
└─────────────────────────────────────────────────────────────┘
```

## Interface Design

### IApplicationTickService (统一定时器)

```csharp
/// <summary>
/// 应用级别的统一定时任务调度服务
/// 使用单一DispatcherTimer,每秒触发Tick事件
/// </summary>
public interface IApplicationTickService
{
    /// <summary>
    /// 每秒触发的Tick事件
    /// 订阅者应在回调中根据自身需求决定是否执行(如每10次执行一次)
    /// </summary>
    event EventHandler<ApplicationTickEventArgs>? Tick;

    /// <summary>
    /// 当前Tick计数(从启动开始累计)
    /// </summary>
    long TickCount { get; }

    /// <summary>
    /// 启动定时器
    /// </summary>
    void Start();

    /// <summary>
    /// 停止定时器
    /// </summary>
    void Stop();
}

public class ApplicationTickEventArgs : EventArgs
{
    /// <summary>
    /// 当前Tick计数
    /// </summary>
    public long TickCount { get; init; }

    /// <summary>
    /// Tick时间戳
    /// </summary>
    public DateTime Timestamp { get; init; }
}
```

### IUserActivityTracker (用户活动追踪)

```csharp
public interface IUserActivityTracker
{
    /// <summary>
    /// 用户最后活动时间
    /// </summary>
    DateTime LastActivityTime { get; }

    /// <summary>
    /// 用户是否活跃(在配置的超时时间内有活动)
    /// </summary>
    bool IsUserActive { get; }

    /// <summary>
    /// 距离不活跃超时的剩余时间
    /// </summary>
    TimeSpan TimeUntilInactive { get; }

    /// <summary>
    /// 会话即将过期事件(提前警告)
    /// </summary>
    event EventHandler<SessionExpiringEventArgs>? SessionExpiring;

    /// <summary>
    /// 会话已过期事件(需要登出)
    /// </summary>
    event EventHandler? SessionExpired;

    /// <summary>
    /// 开始追踪用户活动
    /// </summary>
    void StartTracking();

    /// <summary>
    /// 停止追踪
    /// </summary>
    void StopTracking();

    /// <summary>
    /// 重置活动计时器(用户操作或刷新Token成功后调用)
    /// </summary>
    void ResetActivity();
}
```

## Configuration

```json
{
  "Lybt": {
    "Session": {
      "InactivityTimeoutMinutes": 15,
      "WarningBeforeTimeoutMinutes": 2,
      "ActivityCheckIntervalSeconds": 60
    }
  }
}
```

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| InputManager事件在某些场景不触发(如对话框) | 确保在App级别注册,对话框继承事件传播 |
| Timer精度问题导致提前/延迟登出 | 使用DateTime比较而非累加,容忍1分钟误差 |
| 用户看到警告但未操作导致数据丢失 | 警告对话框提供"保持登录"按钮,点击即重置 |
| 多窗口应用活动追踪不完整 | 当前为单窗口应用,暂不考虑 |

## Migration Plan

1. **Phase 1**: 添加UserActivityTracker服务(不影响现有行为)
2. **Phase 2**: 修改TokenRefreshHandler增加活跃检查
3. **Phase 3**: 添加警告对话框UI
4. **Phase 4**: 配置化并测试

回滚: 移除对IUserActivityTracker的依赖即可恢复原有行为

## Open Questions

- [x] 是否需要服务端配合强制Token失效? -> 本次仅客户端实现
- [ ] 是否需要在警告对话框中显示剩余时间倒计时?
