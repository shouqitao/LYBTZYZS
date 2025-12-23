# Design: unify-event-system

## 1. 事件分类架构

### 1.1 事件层次

```
┌─────────────────────────────────────────────────────────────────┐
│                     跨模块事件 (PubSubEvent)                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │  CoreEvents     │  │ DomainEvents    │  │ SystemEvents    │  │
│  │  (Foundation)   │  │ (Infrastructure)│  │ (Shell)         │  │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤  │
│  │ AuthEvents      │  │ PatientEvents   │  │ NavigationEvents│  │
│  │ SessionEvents   │  │ CaseEvents      │  │ LifecycleEvents │  │
│  │ TokenEvents     │  │ PrescriptionEvt │  │                 │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                   组件内部事件 (EventHandler)                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ Service内部事件  │  │ ViewModel事件   │  │ 控件事件        │  │
│  │ (紧耦合通信)     │  │ (属性变更)      │  │ (UI交互)        │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 事件分类标准

| 特征 | 使用PubSubEvent | 使用EventHandler |
|------|-----------------|------------------|
| 订阅者位置 | 跨模块/跨程序集 | 同一类/紧耦合组件 |
| 生命周期 | 独立于发布者 | 依赖发布者实例 |
| 解耦程度 | 完全解耦 | 直接引用 |
| 典型场景 | 登录/登出通知 | 计算完成回调 |

## 2. 统一Payload规范

### 2.1 Payload设计模式

```csharp
/// <summary>
/// 事件载荷基础接口 (可选)
/// </summary>
public interface IEventPayload
{
    DateTime Timestamp { get; }
}

/// <summary>
/// 标准Payload模板
/// </summary>
public record SomeEventPayload : IEventPayload
{
    /// <summary>
    /// 核心业务数据
    /// </summary>
    public required SomeDto Data { get; init; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 可选：事件来源
    /// </summary>
    public string? Source { get; init; }
}
```

### 2.2 Payload命名约定

- 事件类: `{Domain}{Action}Event` (如 `PatientCreatedEvent`)
- 载荷类: `{Domain}{Action}Payload` (如 `PatientCreatedPayload`)
- 聚合类: `{Domain}Events` (如 `PatientEvents`)

### 2.3 现有事件迁移映射

| 现有事件 | 新事件位置 | Payload类型 |
|----------|-----------|-------------|
| LoginSuccessEvent | AuthEvents.LoginSucceededEvent | LoginSucceededPayload |
| LogoutEvent | AuthEvents.LogoutCompletedEvent | LogoutCompletedPayload |
| TokenLifecycleStateChangedEvent | TokenEvents.LifecycleChangedEvent | TokenLifecycleChangedPayload |
| PatientCreatedEvent | PatientEvents.CreatedEvent | PatientCreatedPayload |
| PatientUpdatedEvent | PatientEvents.UpdatedEvent | PatientUpdatedPayload |
| PatientSelectedEvent | PatientEvents.SelectedEvent | PatientSelectedPayload |
| ConsultationCompletedEvent | CaseEvents.ConsultationCompletedEvent | ConsultationCompletedPayload |
| PrescriptionCompletedEvent | CaseEvents.PrescriptionCompletedEvent | PrescriptionCompletedPayload |
| PasswordChangedEvent | AuthEvents.PasswordChangedEvent | PasswordChangedPayload |

## 3. 事件聚合类结构

### 3.1 AuthEvents (已存在，扩展)

```csharp
// 位置: LYBT.Desktop.Foundation/Security/AuthEvents.cs
public static class AuthEvents
{
    // 登录相关
    public class LoginSucceededEvent : PubSubEvent<LoginSucceededPayload> { }
    public class LoginFailedEvent : PubSubEvent<LoginFailedPayload> { }
    public class LoginStateChangedEvent : PubSubEvent<LoginStateChangedPayload> { }

    // 登出相关
    public class LogoutCompletedEvent : PubSubEvent<LogoutCompletedPayload> { }
    public class ServerLogoutFailedEvent : PubSubEvent<ServerLogoutFailedPayload> { }

    // 密码相关
    public class PasswordChangedEvent : PubSubEvent<PasswordChangedPayload> { }
}
```

### 3.2 TokenEvents (新建)

```csharp
// 位置: LYBT.Desktop.Foundation/Security/TokenEvents.cs
public static class TokenEvents
{
    public class RefreshSucceededEvent : PubSubEvent<TokenRefreshSucceededPayload> { }
    public class RefreshFailedEvent : PubSubEvent<TokenRefreshFailedPayload> { }
    public class LifecycleChangedEvent : PubSubEvent<TokenLifecycleChangedPayload> { }
    public class ExpiringEvent : PubSubEvent<SessionExpiringPayload> { }
    public class ExpiredEvent : PubSubEvent<SessionExpiredPayload> { }
}
```

### 3.3 PatientEvents (新建)

```csharp
// 位置: LYBT.Desktop.Infrastructure/Events/PatientEvents.cs
public static class PatientEvents
{
    public class CreatedEvent : PubSubEvent<PatientCreatedPayload> { }
    public class UpdatedEvent : PubSubEvent<PatientUpdatedPayload> { }
    public class SelectedEvent : PubSubEvent<PatientSelectedPayload> { }
}
```

### 3.4 CaseEvents (新建)

```csharp
// 位置: LYBT.Desktop.Infrastructure/Events/CaseEvents.cs
public static class CaseEvents
{
    public class ConsultationCompletedEvent : PubSubEvent<ConsultationCompletedPayload> { }
    public class PrescriptionCompletedEvent : PubSubEvent<PrescriptionCompletedPayload> { }
    public class WorkspaceChangedEvent : PubSubEvent<WorkspaceChangedPayload> { }
}
```

## 4. 迁移兼容策略

### 4.1 Phase式迁移

```
Phase 1: 创建新事件聚合类 + Payload定义
         ↓
Phase 2: 发布者改为只发布PubSubEvent
         ↓
Phase 3: 迁移所有订阅者到PubSubEvent
         ↓
Phase 4: 删除旧事件类 + 兼容代码
```

### 4.2 兼容模式移除检查清单

**LoginStateMachine**:
- [ ] 移除 `StateChanged` EventHandler事件
- [ ] 仅保留 `AuthEvents.LoginStateChangedEvent` 发布

**LogoutService**:
- [ ] 移除 `ServerLogoutFailed` EventHandler事件
- [ ] 移除 `PendingLogoutsCleared` EventHandler事件
- [ ] 仅保留 PubSubEvent 发布

**TokenRefreshHandler**:
- [ ] 移除 `TokenRefreshFailed` EventHandler事件
- [ ] 移除 `TokenRefreshSucceeded` EventHandler事件
- [ ] 仅保留 PubSubEvent 发布

## 5. 订阅者迁移指南

### 5.1 迁移前 (EventHandler)

```csharp
// 订阅
_loginStateMachine.StateChanged += OnStateChanged;

// 取消订阅
_loginStateMachine.StateChanged -= OnStateChanged;

private void OnStateChanged(object? sender, LoginStateChangedEventArgs e)
{
    // 处理逻辑
}
```

### 5.2 迁移后 (PubSubEvent)

```csharp
private SubscriptionToken? _stateChangedToken;

// 订阅
_stateChangedToken = _eventAggregator
    .GetEvent<AuthEvents.LoginStateChangedEvent>()
    .Subscribe(OnStateChanged, ThreadOption.UIThread);

// 取消订阅
_stateChangedToken?.Dispose();

private void OnStateChanged(LoginStateChangedPayload payload)
{
    // 处理逻辑
}
```

## 6. 测试策略

### 6.1 单元测试

- 每个事件聚合类的Payload序列化测试
- 事件发布/订阅集成测试

### 6.2 回归测试

- 登录/登出流程端到端测试
- Token刷新场景测试
- 患者CRUD事件传播测试

## 7. 性能考量

PubSubEvent vs EventHandler性能对比:

| 指标 | EventHandler | PubSubEvent |
|------|--------------|-------------|
| 调用开销 | 直接委托调用 | 反射+委托 |
| 内存 | 强引用 | 弱引用(可配置) |
| 线程切换 | 无 | 可配置(UIThread/BackgroundThread) |

**结论**: 对于UI事件频率，性能差异可忽略不计。PubSubEvent的解耦优势远大于微小的性能开销。
