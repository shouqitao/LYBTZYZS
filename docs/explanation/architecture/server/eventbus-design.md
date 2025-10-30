# Server端事件总线架构设计

> **文档版本**: 1.0.0  
> **最后更新**: 2025-10-30  
> **适用范围**: Server端事件总线与模块管理系统  
> **相关Epic**: Phase 2架构文档补全

---

## 📋 文档导航

**核心文档**:
- [Server端架构总览](README.md) - Server端三层架构整体设计
- [Server端模块化架构](README.md#模块化架构) - 8个业务模块设计
- [EventBus集成指南](../../how-to-guides/server/eventbus-integration.md) - 实践指南

**相关文档**:
- [Client端架构设计](../client/README.md) - 了解Client端事件通信（Prism EventAggregator）
- [共享架构设计](../shared/README.md) - 跨端通信机制

**快速参考**:
- [代码模式参考](../../quick-reference/code-patterns.md) - 事件定义与发布模式
- [API参考](../../quick-reference/api-reference.md) - EventBus API快速查询

---

## 1. 模块概述

### 1.1 定位与职责

**LYBT.Core.EventBus** 是Server端的核心基础设施库，提供：

1. **进程内事件总线（In-Memory Event Bus）**
   - 实现模块间松耦合通信
   - 基于发布-订阅模式（Pub-Sub Pattern）
   - 无外部依赖（符合MVP约束）

2. **模块生命周期管理系统**
   - 模块注册、启动、停止
   - 模块健康检查与监控
   - 模块依赖分析与启动顺序解析

**核心价值**:
- ✅ 解耦8个业务模块：Auth、Users、Patients、MedicalCase、Consultation、Prescriptions、Herbs、Formula
- ✅ 避免模块间直接引用（防止循环依赖）
- ✅ 支持跨模块业务协同（如患者创建 → 病案初始化）
- ✅ 简化架构（MVP原则：够用即好）

### 1.2 技术约束与决策

**MVP约束遵循**:
- ❌ **禁止使用**: Redis、RabbitMQ、MassTransit、MediatR、CQRS
- ✅ **允许使用**: 进程内事件总线（In-Memory）
- ✅ **简化原则**: 单体架构，无分布式消息队列

**设计决策记录**:
- **ADR-001**: 选择In-Memory Event Bus而非外部MQ（MVP阶段无需分布式）
- **ADR-002**: 使用ConcurrentDictionary管理订阅（线程安全+高性能）
- **ADR-003**: 并行处理事件（Task.WhenAll）提升吞吐量
- **ADR-004**: IHostedService自动订阅（简化配置）

### 1.3 依赖关系

```
LYBT.Core.EventBus (核心库)
├── 无内部项目依赖 (纯基础库)
├── Microsoft.Extensions.* (DI、Logging、Hosting抽象)
└── 被8个业务模块 + LYBT.WebAPI 依赖
```

**被依赖项目**:
1. **LYBT.Module.Auth** - 发布用户登录事件
2. **LYBT.Module.Users** - 发布用户创建事件
3. **LYBT.Module.Patients** - 发布患者创建事件
4. **LYBT.Module.MedicalCase** - 订阅患者创建事件
5. **LYBT.Module.Consultation** - 发布诊疗完成事件
6. **LYBT.Module.Prescriptions** - 订阅诊疗完成事件
7. **LYBT.Module.Herbs** - 发布药材库存变更事件
8. **LYBT.Module.Formula** - 订阅药材库存变更事件
9. **LYBT.WebAPI** - 注册EventBus + 初始化模块管理器

---

## 2. 核心接口设计

### 2.1 IEventBus - 事件总线接口

**职责**: 定义事件发布、订阅、统计的核心契约。

**完整定义**:
```csharp
/// <summary>
/// 事件总线接口 - 提供进程内事件发布订阅机制
/// </summary>
public interface IEventBus
{
    /// <summary>发布集成事件（异步）</summary>
    /// <typeparam name="TEvent">事件类型（必须实现IIntegrationEvent）</typeparam>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;

    /// <summary>订阅事件处理器（泛型方式）</summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">处理器类型（必须实现IIntegrationEventHandler&lt;TEvent&gt;）</typeparam>
    /// <returns>订阅是否成功</returns>
    bool Subscribe<TEvent, THandler>()
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>;

    /// <summary>订阅事件处理器（通过类型）</summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handlerType">处理器类型</param>
    /// <returns>订阅是否成功</returns>
    bool Subscribe(Type eventType, Type handlerType);

    /// <summary>取消订阅</summary>
    bool Unsubscribe<TEvent, THandler>()
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>;

    /// <summary>获取指定事件的订阅数量</summary>
    int GetSubscriptionCount<TEvent>() where TEvent : class, IIntegrationEvent;

    /// <summary>获取所有已注册的事件类型</summary>
    IReadOnlyCollection<Type> GetRegisteredEventTypes();

    /// <summary>清空所有订阅（通常在应用关闭时调用）</summary>
    void ClearSubscriptions();
}
```

**设计考量**:
1. **泛型约束**: `where TEvent : class, IIntegrationEvent` 确保类型安全
2. **异步发布**: `PublishAsync` 支持异步I/O（如数据库查询）
3. **多种订阅方式**: 泛型 + Type参数，适应不同场景
4. **统计信息**: `GetSubscriptionCount` 用于监控和调试

### 2.2 IIntegrationEvent - 集成事件接口

**职责**: 定义跨模块事件的元数据契约。

**完整定义**:
```csharp
/// <summary>
/// 集成事件接口 - 所有跨模块事件必须实现此接口
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>事件唯一标识（用于幂等性检查）</summary>
    Guid Id { get; }

    /// <summary>事件创建时间（UTC）</summary>
    DateTime OccurredOn { get; }

    /// <summary>事件类型名称（通常为类名）</summary>
    string EventType { get; }

    /// <summary>事件来源模块（如"Patients"、"MedicalCase"）</summary>
    string Source { get; }

    /// <summary>事件版本（用于向后兼容）</summary>
    int Version { get; }
}
```

**设计考量**:
1. **Id (Guid)**: 幂等性保证（避免重复处理）
2. **OccurredOn (DateTime)**: 审计跟踪 + 事件顺序判断
3. **EventType (string)**: 类型识别 + 日志记录
4. **Source (string)**: 追踪事件来源模块
5. **Version (int)**: 支持事件架构演进（向后兼容）

**实际使用示例**:
```csharp
// 患者创建事件
public class PatientCreatedEvent : IntegrationEventBase
{
    public int PatientId { get; set; }
    public string PatientName { get; set; }
    public DateTime CreatedAt { get; set; }

    public PatientCreatedEvent() : base("Patients") // 指定来源模块
    {
    }
}
```

### 2.3 IIntegrationEventHandler<T> - 事件处理器接口

**职责**: 定义事件处理逻辑的契约。

**完整定义**:
```csharp
/// <summary>
/// 事件处理器接口（泛型版本）
/// </summary>
/// <typeparam name="TEvent">事件类型（支持协变：in关键字）</typeparam>
public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
    where TEvent : class, IIntegrationEvent
{
    /// <summary>处理事件（异步）</summary>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// 事件处理器基接口（非泛型版本，用于反射）
/// </summary>
public interface IIntegrationEventHandler
{
    /// <summary>处理器名称（用于日志）</summary>
    string HandlerName { get; }

    /// <summary>处理的事件类型</summary>
    Type EventType { get; }
}
```

**设计考量**:
1. **协变（Contravariance）**: `in TEvent` 允许处理基类事件
2. **异步处理**: `Task HandleAsync` 支持异步业务逻辑
3. **取消令牌**: 支持优雅关闭和超时控制
4. **双接口设计**: 泛型接口 + 非泛型基接口（用于反射调用）

**实际使用示例**:
```csharp
// 患者创建事件处理器（在MedicalCase模块）
public class PatientCreatedEventHandler : IIntegrationEventHandler<PatientCreatedEvent>
{
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly ILogger<PatientCreatedEventHandler> _logger;

    public string HandlerName => nameof(PatientCreatedEventHandler);
    public Type EventType => typeof(PatientCreatedEvent);

    public PatientCreatedEventHandler(
        IMedicalCaseService medicalCaseService,
        ILogger<PatientCreatedEventHandler> logger)
    {
        _medicalCaseService = medicalCaseService;
        _logger = logger;
    }

    public async Task HandleAsync(PatientCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("患者 {PatientName}（ID: {PatientId}）已创建，初始化病案...",
            @event.PatientName, @event.PatientId);

        // 业务逻辑：为新患者创建初始病案
        await _medicalCaseService.CreateAsync(new CreateMedicalCaseRequest
        {
            PatientId = @event.PatientId,
            Status = MedicalCaseStatus.Draft,
            CreatedBy = "System" // 系统自动创建
        }, cancellationToken);
    }
}
```

### 2.4 IntegrationEventBase - 事件基类

**职责**: 提供IIntegrationEvent的通用实现，减少样板代码。

**完整定义**:
```csharp
/// <summary>
/// 集成事件基类 - 自动生成Id、OccurredOn、EventType
/// </summary>
public abstract class IntegrationEventBase : IIntegrationEvent
{
    /// <summary>默认构造函数（自动生成元数据）</summary>
    protected IntegrationEventBase()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        EventType = GetType().Name; // 反射获取类型名
        Version = 1; // 默认版本
    }

    /// <summary>带来源模块的构造函数</summary>
    /// <param name="source">来源模块名称</param>
    protected IntegrationEventBase(string source) : this()
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public Guid Id { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public string EventType { get; private set; }
    public string Source { get; private set; } = "Unknown";
    public virtual int Version { get; protected set; }

    /// <summary>获取事件描述（用于日志）</summary>
    public virtual string GetDescription()
    {
        return $"{EventType} from {Source} at {OccurredOn:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>重写Equals（基于Id比较）</summary>
    public override bool Equals(object? obj)
    {
        if (obj is not IntegrationEventBase other)
            return false;
        return Id == other.Id; // 基于唯一标识符比较
    }

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() => GetDescription();
}
```

**设计考量**:
1. **自动生成元数据**: 减少样板代码，避免遗漏字段
2. **protected构造函数**: 强制继承，不允许直接实例化
3. **virtual方法**: 允许子类覆盖（如GetDescription）
4. **Equals/GetHashCode**: 基于Id实现（用于集合去重）

---

## 3. InMemoryEventBus实现

### 3.1 核心数据结构

**订阅存储**:
```csharp
private readonly ConcurrentDictionary<Type, ConcurrentBag<Type>> _subscriptions;
```

**设计决策**:
- **ConcurrentDictionary**: 线程安全的字典（Key: 事件类型, Value: 处理器类型集合）
- **ConcurrentBag**: 线程安全的无序集合（存储同一事件的多个处理器）
- **为什么不用List**: ConcurrentBag在高并发Add/Enumerate场景性能更优

**统计信息**:
```csharp
private long _totalPublishedEvents;     // Interlocked.Increment
private long _totalProcessedEvents;     // Interlocked.Increment
private long _failedEvents;             // Interlocked.Increment
private readonly EventBusStatistics _statistics; // 封装类
```

### 3.2 PublishAsync - 事件发布

**完整实现流程**:
```csharp
public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    where TEvent : class, IIntegrationEvent
{
    var eventType = typeof(TEvent);
    
    // 1️⃣ 日志记录
    _logger.LogInformation("发布事件: {EventType}, ID: {EventId}, 来源: {Source}",
        eventType.Name, @event.Id, @event.Source);

    // 2️⃣ 更新统计信息（线程安全）
    Interlocked.Increment(ref _totalPublishedEvents);
    _statistics.LastActivityTime = DateTime.UtcNow;

    // 3️⃣ 查找订阅的处理器
    if (!_subscriptions.TryGetValue(eventType, out var handlerTypes))
    {
        _logger.LogWarning("没有找到事件 {EventType} 的处理器", eventType.Name);
        return; // 无订阅者时直接返回（不报错）
    }

    // 4️⃣ 并行处理所有订阅的处理器
    var tasks = new List<Task>();
    foreach (var handlerType in handlerTypes)
    {
        tasks.Add(ProcessEventAsync(@event, handlerType, cancellationToken));
    }

    // 5️⃣ 等待所有处理器完成（Task.WhenAll）
    await Task.WhenAll(tasks);

    _logger.LogInformation("事件 {EventType} 处理完成，共 {HandlerCount} 个处理器",
        eventType.Name, tasks.Count);
}
```

**关键设计点**:
1. **并行处理**: `Task.WhenAll` 提升吞吐量（处理器间无依赖）
2. **无订阅者不报错**: 符合Pub-Sub语义（发布者不关心是否有订阅者）
3. **Interlocked.Increment**: 线程安全的计数器更新
4. **取消令牌传递**: 支持优雅关闭

### 3.3 ProcessEventAsync - 处理器调用

**完整实现流程**:
```csharp
private async Task ProcessEventAsync(IIntegrationEvent @event, Type handlerType, CancellationToken cancellationToken)
{
    try
    {
        // 1️⃣ 从DI容器获取处理器实例
        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            _logger.LogError("无法从DI容器解析处理器: {HandlerType}", handlerType.Name);
            Interlocked.Increment(ref _failedEvents);
            return;
        }

        // 2️⃣ 反射获取HandleAsync方法
        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
        {
            _logger.LogError("处理器 {HandlerType} 没有HandleAsync方法", handlerType.Name);
            Interlocked.Increment(ref _failedEvents);
            return;
        }

        // 3️⃣ 调用处理器方法（参数：事件实例 + 取消令牌）
        var result = method.Invoke(handler, new object[] { @event, cancellationToken });

        // 4️⃣ 等待异步任务完成
        if (result is Task task)
        {
            await task;
        }

        // 5️⃣ 更新成功统计
        Interlocked.Increment(ref _totalProcessedEvents);

        _logger.LogDebug("处理器 {HandlerType} 成功处理事件 {EventType}",
            handlerType.Name, @event.EventType);
    }
    catch (Exception ex)
    {
        // 6️⃣ 异常处理（不抛出，避免影响其他处理器）
        _logger.LogError(ex, "处理器 {HandlerType} 处理事件 {EventType} 时发生异常",
            handlerType.Name, @event.EventType);
        Interlocked.Increment(ref _failedEvents);
    }
}
```

**关键设计点**:
1. **反射调用**: 支持动态类型（运行时注册处理器）
2. **异常隔离**: 单个处理器失败不影响其他处理器
3. **DI容器解析**: 支持Scoped生命周期（每次调用创建新实例）
4. **统计更新**: 成功/失败计数器

### 3.4 Subscribe - 订阅管理

**完整实现流程**:
```csharp
public bool Subscribe(Type eventType, Type handlerType)
{
    // 1️⃣ 验证事件类型（必须实现IIntegrationEvent）
    if (!typeof(IIntegrationEvent).IsAssignableFrom(eventType))
    {
        throw new ArgumentException(
            $"事件类型 {eventType.Name} 必须实现 IIntegrationEvent 接口",
            nameof(eventType));
    }

    // 2️⃣ 验证处理器类型（必须实现IIntegrationEventHandler<TEvent>）
    var expectedHandlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
    if (!expectedHandlerInterface.IsAssignableFrom(handlerType))
    {
        throw new ArgumentException(
            $"处理器类型 {handlerType.Name} 必须实现 {expectedHandlerInterface.Name} 接口",
            nameof(handlerType));
    }

    // 3️⃣ 添加订阅（或更新现有订阅）
    _subscriptions.AddOrUpdate(
        eventType,
        // 新增：创建ConcurrentBag并添加处理器
        new ConcurrentBag<Type> { handlerType },
        // 更新：添加到现有ConcurrentBag（去重）
        (key, existing) =>
        {
            if (!existing.Contains(handlerType))
            {
                existing.Add(handlerType);
            }
            return existing;
        });

    // 4️⃣ 更新统计信息
    _statistics.RegisteredHandlers = _subscriptions.Values.SelectMany(h => h).Count();
    _statistics.RegisteredEventTypes = _subscriptions.Keys.Count;

    _logger.LogInformation("已订阅事件处理器: {EventType} -> {HandlerType}",
        eventType.Name, handlerType.Name);

    return true;
}
```

**关键设计点**:
1. **类型验证**: 编译时类型安全（泛型约束）+ 运行时类型检查
2. **重复订阅去重**: `Contains` 检查避免重复添加
3. **AddOrUpdate原子性**: ConcurrentDictionary保证线程安全
4. **统计信息实时更新**: 用于监控和调试

---

## 4. 事件生命周期

### 4.1 完整流程图

```
┌─────────────────────────────────────────────────────────────────┐
│                        事件发布流程                              │
└─────────────────────────────────────────────────────────────────┘

1️⃣ 业务模块（如Patients）
   ↓
   await _eventBus.PublishAsync(new PatientCreatedEvent { ... })
   ↓
2️⃣ InMemoryEventBus.PublishAsync
   ↓
   ├── 更新统计信息（Interlocked.Increment）
   ├── 查找订阅者（_subscriptions.TryGetValue）
   └── 并行调用处理器（Task.WhenAll）
       ↓
3️⃣ ProcessEventAsync（每个处理器）
   ↓
   ├── 从DI容器获取处理器实例（_serviceProvider.GetService）
   ├── 反射调用HandleAsync方法（method.Invoke）
   └── 等待异步任务完成（await task）
       ↓
4️⃣ 业务模块（如MedicalCase）
   ↓
   PatientCreatedEventHandler.HandleAsync
   ↓
   └── 执行业务逻辑（如创建初始病案）

┌─────────────────────────────────────────────────────────────────┐
│                        订阅注册流程                              │
└─────────────────────────────────────────────────────────────────┘

方式1: 启动时自动订阅（推荐）
   ↓
1️⃣ Startup.ConfigureServices
   ↓
   services.AddEventHandlerWithSubscription<PatientCreatedEvent, PatientCreatedEventHandler>()
   ↓
2️⃣ EventBusHostedService.StartAsync
   ↓
   foreach (var (eventType, handlerType) in _subscriptionOptions.Subscriptions)
       eventBus.Subscribe(eventType, handlerType)
   ↓
3️⃣ InMemoryEventBus.Subscribe
   ↓
   _subscriptions.AddOrUpdate(eventType, handlerType)

方式2: 模块启动时手动订阅
   ↓
1️⃣ ModuleBase.OnStartAsync
   ↓
   _eventBus.Subscribe<PatientCreatedEvent, PatientCreatedEventHandler>()
   ↓
2️⃣ InMemoryEventBus.Subscribe
   ↓
   _subscriptions.AddOrUpdate(eventType, handlerType)
```

### 4.2 时序图（典型场景：患者创建 → 病案初始化）

```
PatientsModule     EventBus           MedicalCaseModule
      |                |                      |
      |--CreatePatient-|                      |
      |                |                      |
      |--PublishAsync--|                      |
      |   (PatientCreatedEvent)               |
      |                |                      |
      |                |--ProcessEventAsync---|
      |                |   (find handler)     |
      |                |                      |
      |                |--GetService--------->|
      |                |   (DI resolve)       |
      |                |                      |
      |                |--HandleAsync-------->|
      |                |                      |
      |                |                   [创建病案]
      |                |                      |
      |                |<--Task completed-----|
      |                |                      |
      |<--PublishAsync-|                      |
      |   completed    |                      |
```

### 4.3 关键时间点

| 阶段 | 时间点 | 操作 | 耗时（估算） |
|------|--------|------|-------------|
| **发布** | T0 | 调用PublishAsync | 0ms |
| **查找订阅** | T0+1ms | TryGetValue | <1ms |
| **并行启动** | T0+2ms | Task.WhenAll开始 | 0ms |
| **处理器解析** | T0+3ms | GetService（每个） | 1-5ms |
| **业务逻辑** | T0+5ms | HandleAsync执行 | 10-100ms（取决于业务） |
| **完成** | T0+105ms | Task.WhenAll完成 | 0ms |

**注意事项**:
- ✅ 并行处理缩短总耗时（N个处理器 ≈ max(handler_time)，非sum(handler_time)）
- ⚠️ 避免处理器中执行长耗时操作（应使用后台任务）
- ⚠️ 处理器失败不影响发布者（异常隔离）

---

## 5. 线程与并发模型

### 5.1 线程安全机制

**ConcurrentDictionary订阅管理**:
```csharp
// 线程安全的订阅添加
_subscriptions.AddOrUpdate(
    eventType,
    new ConcurrentBag<Type> { handlerType },
    (key, existing) => 
    {
        if (!existing.Contains(handlerType)) 
            existing.Add(handlerType);
        return existing;
    });

// 线程安全的订阅查找
_subscriptions.TryGetValue(eventType, out var handlerTypes);
```

**Interlocked计数器**:
```csharp
// 原子递增（无需lock）
Interlocked.Increment(ref _totalPublishedEvents);
Interlocked.Increment(ref _totalProcessedEvents);
Interlocked.Increment(ref _failedEvents);
```

**为什么不用lock**:
- ✅ Interlocked性能更优（CAS指令，无线程阻塞）
- ✅ ConcurrentDictionary内部已实现细粒度锁
- ⚠️ 避免过度使用lock（可能导致死锁和性能瓶颈）

### 5.2 并发场景分析

**场景1: 多个模块同时发布不同事件**
```
Thread 1: Patients.PublishAsync(PatientCreatedEvent)
Thread 2: Users.PublishAsync(UserCreatedEvent)
Thread 3: Consultation.PublishAsync(ConsultationCompletedEvent)
```
✅ **安全**: 不同事件类型，无共享资源竞争  
✅ **性能**: 完全并行，无阻塞

**场景2: 多个模块同时发布相同事件**
```
Thread 1: Module A.PublishAsync(PatientCreatedEvent { PatientId = 1 })
Thread 2: Module B.PublishAsync(PatientCreatedEvent { PatientId = 2 })
```
✅ **安全**: TryGetValue读取，无写操作  
✅ **性能**: 并行处理，共享订阅者列表（只读）

**场景3: 发布时同时订阅**
```
Thread 1: PublishAsync(PatientCreatedEvent)
Thread 2: Subscribe<PatientCreatedEvent, NewHandler>()
```
⚠️ **竞态条件**: NewHandler可能被本次事件调用或不被调用  
✅ **安全**: ConcurrentDictionary保证不会崩溃  
📌 **建议**: 启动时完成所有订阅，避免运行时动态订阅

### 5.3 并行处理优化

**Task.WhenAll并行化**:
```csharp
// ❌ 串行处理（慢）
foreach (var handlerType in handlerTypes)
{
    await ProcessEventAsync(@event, handlerType, cancellationToken);
}
// 总耗时 = 100ms + 50ms + 80ms = 230ms

// ✅ 并行处理（快）
var tasks = handlerTypes.Select(handlerType => 
    ProcessEventAsync(@event, handlerType, cancellationToken));
await Task.WhenAll(tasks);
// 总耗时 = max(100ms, 50ms, 80ms) = 100ms
```

**限制**:
- ⚠️ 处理器数量 >100时，考虑限流（避免线程池耗尽）
- ⚠️ 处理器间有依赖时，不能并行（需手动排序）

---

## 6. 统计与监控

### 6.1 EventBusStatistics类

**完整定义**:
```csharp
/// <summary>事件总线统计信息</summary>
public class EventBusStatistics
{
    /// <summary>已发布事件总数</summary>
    public long TotalPublishedEvents { get; set; }

    /// <summary>已处理事件总数</summary>
    public long TotalProcessedEvents { get; set; }

    /// <summary>失败事件总数</summary>
    public long FailedEvents { get; set; }

    /// <summary>已注册的事件类型数</summary>
    public int RegisteredEventTypes { get; set; }

    /// <summary>已注册的处理器数</summary>
    public int RegisteredHandlers { get; set; }

    /// <summary>最后活动时间</summary>
    public DateTime? LastActivityTime { get; set; }
}
```

### 6.2 监控指标

| 指标 | 计算方式 | 正常范围 | 异常阈值 |
|------|---------|---------|---------|
| **发布成功率** | TotalProcessedEvents / TotalPublishedEvents | >99% | <95% |
| **处理失败率** | FailedEvents / TotalProcessedEvents | <1% | >5% |
| **平均处理器数** | RegisteredHandlers / RegisteredEventTypes | 1-3 | >5（可能过度订阅） |
| **活动频率** | 距LastActivityTime的时间 | <1分钟 | >10分钟（可能无事件） |

### 6.3 日志记录

**推荐日志级别**:
```csharp
// Information: 事件发布/订阅
_logger.LogInformation("发布事件: {EventType}, ID: {EventId}", ...);
_logger.LogInformation("已订阅事件处理器: {EventType} -> {HandlerType}", ...);

// Debug: 处理器执行
_logger.LogDebug("处理器 {HandlerType} 成功处理事件 {EventType}", ...);

// Warning: 无订阅者
_logger.LogWarning("没有找到事件 {EventType} 的处理器", ...);

// Error: 处理失败
_logger.LogError(ex, "处理器 {HandlerType} 处理事件 {EventType} 时发生异常", ...);
```

**日志查询示例**（用于生产环境排查）:
```sql
-- 查找失败事件
SELECT * FROM Logs 
WHERE Level = 'Error' 
  AND Message LIKE '%处理事件%时发生异常%'
ORDER BY Timestamp DESC;

-- 查找无订阅事件
SELECT * FROM Logs 
WHERE Level = 'Warning' 
  AND Message LIKE '%没有找到事件%的处理器%'
ORDER BY Timestamp DESC;
```

---

## 7. 模块系统集成

### 7.1 IModule接口（简化版）

**核心定义**:
```csharp
/// <summary>模块接口</summary>
public interface IModule
{
    /// <summary>模块描述符（元数据）</summary>
    ModuleDescriptor GetDescriptor();
}

/// <summary>模块生命周期接口</summary>
public interface IModuleLifecycle
{
    /// <summary>启动模块</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>停止模块</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>模块当前状态</summary>
    ModuleState State { get; }
}

/// <summary>模块状态枚举</summary>
public enum ModuleState
{
    Uninitialized,  // 未初始化
    Registered,     // 已注册
    Starting,       // 启动中
    Running,        // 运行中
    Stopping,       // 停止中
    Stopped,        // 已停止
    Faulted         // 故障
}
```

### 7.2 IModuleManager接口（核心方法）

**完整定义**（28个方法，以下列出核心部分）:
```csharp
/// <summary>模块管理器接口</summary>
public interface IModuleManager
{
    // ========== 模块注册 ==========
    Task RegisterModuleAsync(IModule module);
    Task UnregisterModuleAsync(string moduleId);

    // ========== 生命周期管理 ==========
    Task StartAllModulesAsync();
    Task StopAllModulesAsync();
    Task StartModuleAsync(string moduleId);
    Task StopModuleAsync(string moduleId);
    Task RestartModuleAsync(string moduleId);

    // ========== 依赖分析 ==========
    (bool IsValid, string ErrorMessage) CheckDependencies();
    IReadOnlyList<string> ResolveStartupOrder();

    // ========== 健康检查 ==========
    Task<ModuleHealthStatus> GetModuleHealthAsync(string moduleId);
    IReadOnlyDictionary<string, ModuleHealthStatus> GetAllModulesHealth();

    // ========== 查询 ==========
    IModule? GetModule(string moduleId);
    IReadOnlyCollection<IModule> GetAllModules();
    IReadOnlyCollection<IModule> GetModulesByCategory(ModuleCategory category);

    // ========== 事件 ==========
    event EventHandler<ModuleStateChangedEventArgs> ModuleStateChanged;
    event EventHandler<ModuleHealthChangedEventArgs> ModuleHealthChanged;
}
```

### 7.3 模块事件（5个核心事件）

**1. ModuleRegisteredEvent - 模块注册事件**
```csharp
public class ModuleRegisteredEvent : IntegrationEventBase
{
    public string ModuleId { get; }
    public string ModuleName { get; }
    public ModuleCategory Category { get; }
    public IReadOnlyList<string> Dependencies { get; }
}
```

**2. ModuleStateChangedEvent - 模块状态变更事件**
```csharp
public class ModuleStateChangedEvent : IntegrationEventBase
{
    public string ModuleId { get; }
    public ModuleState OldState { get; }
    public ModuleState NewState { get; }
    public DateTime ChangedAt { get; }
}
```

**3. ModuleHealthChangedEvent - 模块健康变更事件**
```csharp
public class ModuleHealthChangedEvent : IntegrationEventBase
{
    public string ModuleId { get; }
    public ModuleHealthStatus OldHealth { get; }
    public ModuleHealthStatus NewHealth { get; }
    public string? Reason { get; }
}
```

**4. ModuleDependencyEvent - 模块依赖事件**
```csharp
public class ModuleDependencyEvent : IntegrationEventBase
{
    public string ModuleId { get; }
    public IReadOnlyList<string> Dependencies { get; }
    public bool IsCritical { get; } // 关键依赖失败时触发
}
```

**5. ModuleUnregisteredEvent - 模块注销事件**
```csharp
public class ModuleUnregisteredEvent : IntegrationEventBase
{
    public string ModuleId { get; }
    public ModuleState FinalState { get; }
    public DateTime UnregisteredAt { get; }
}
```

### 7.4 模块启动流程（与EventBus集成）

```
1️⃣ WebAPI启动
   ↓
   Program.cs: builder.Services.AddInMemoryEventBus()
   ↓
2️⃣ 注册所有业务模块
   ↓
   await moduleManager.RegisterModuleAsync(new AuthModule(...));
   await moduleManager.RegisterModuleAsync(new PatientsModule(...));
   // ... 其他6个模块
   ↓
3️⃣ 检查依赖关系
   ↓
   var dependencies = moduleManager.CheckDependencies();
   if (!dependencies.IsValid)
       throw new Exception(dependencies.ErrorMessage);
   ↓
4️⃣ 解析启动顺序（拓扑排序）
   ↓
   var startupOrder = moduleManager.ResolveStartupOrder();
   // 输出: ["Auth", "Users", "Patients", "MedicalCase", ...]
   ↓
5️⃣ 按依赖顺序启动模块
   ↓
   await moduleManager.StartAllModulesAsync();
   ↓
6️⃣ 每个模块启动时订阅事件
   ↓
   protected override async Task OnStartAsync()
   {
       // MedicalCase模块订阅PatientCreatedEvent
       _eventBus.Subscribe<PatientCreatedEvent, PatientCreatedEventHandler>();
   }
   ↓
7️⃣ 模块运行中
   ↓
   Patients模块: PublishAsync(PatientCreatedEvent)
   MedicalCase模块: HandleAsync(PatientCreatedEvent)
```

### 7.5 依赖关系图（8个业务模块）

```
┌──────────────────────────────────────────────────────────────┐
│                  8个业务模块依赖关系                          │
└──────────────────────────────────────────────────────────────┘

Level 1: 基础模块（无依赖）
┌─────┐  ┌─────┐
│Auth │  │Users│
└─────┘  └─────┘

Level 2: 核心数据模块
┌──────┐  ┌─────┐  ┌────────┐
│Herbs │  │Formula│ │Patients│
└──────┘  └─────┘  └────────┘
              ↑          ↑
              |          |
              |          |
Level 3: 业务逻辑模块    |
┌────────────┐           |
│MedicalCase │<──────────┘
└────────────┘
       ↑
       |
       |
Level 4: 高级业务模块
┌────────────┐  ┌───────────────┐
│Consultation│  │Prescriptions  │
└────────────┘  └───────────────┘
       ↑               ↑
       |               |
       └───────┬───────┘
               |
         需要 MedicalCase
```

**依赖约束**:
- ❌ **禁止**: 循环依赖（如Consultation ← → Prescriptions）
- ✅ **推荐**: 通过EventBus解耦（Consultation发布事件 → Prescriptions订阅）

---

## 8. DI注册模式

### 8.1 ServiceCollectionExtensions完整实现

**扩展方法1: AddInMemoryEventBus**
```csharp
/// <summary>添加内存事件总线服务（核心注册）</summary>
public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
{
    // 1️⃣ 注册事件总线为单例（全局唯一）
    services.TryAddSingleton<IEventBus, InMemoryEventBus>();

    // 2️⃣ 注册订阅配置选项（空配置，后续通过Configure添加）
    services.Configure<EventBusSubscriptionOptions>(_ => { });

    // 3️⃣ 注册托管服务（应用启动时自动订阅）
    services.AddHostedService<Services.EventBusHostedService>();

    return services;
}
```

**扩展方法2: AddEventHandler**
```csharp
/// <summary>添加事件处理器（手动订阅）</summary>
public static IServiceCollection AddEventHandler<TEvent, THandler>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
    where TEvent : class, IIntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    // 注册处理器到DI容器（支持Transient/Scoped/Singleton）
    services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));

    return services;
}
```

**扩展方法3: AddEventHandlerWithSubscription（推荐）**
```csharp
/// <summary>添加事件处理器并自动订阅（推荐）</summary>
public static IServiceCollection AddEventHandlerWithSubscription<TEvent, THandler>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
    where TEvent : class, IIntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    // 1️⃣ 注册处理器
    services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));

    // 2️⃣ 添加配置回调，在服务构建完成后自动订阅
    services.Configure<EventBusSubscriptionOptions>(options =>
    {
        options.AddSubscription<TEvent, THandler>();
    });

    return services;
}
```

### 8.2 Startup.cs完整配置示例

**推荐配置（LYBT.WebAPI）**:
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ========== Step 1: 注册EventBus核心服务 ==========
        services.AddInMemoryEventBus();

        // ========== Step 2: 注册8个业务模块的处理器 ==========
        
        // Auth模块（无订阅）
        
        // Users模块（无订阅）
        
        // Patients模块 → 发布PatientCreatedEvent
        // 无需注册处理器
        
        // MedicalCase模块 → 订阅PatientCreatedEvent
        services.AddEventHandlerWithSubscription<PatientCreatedEvent, PatientCreatedEventHandler>();
        
        // Consultation模块 → 发布ConsultationCompletedEvent
        services.AddEventHandlerWithSubscription<ConsultationCompletedEvent, ConsultationCompletedEventHandler>();
        
        // Prescriptions模块 → 订阅ConsultationCompletedEvent
        services.AddEventHandlerWithSubscription<ConsultationCompletedEvent, PrescriptionCreatedEventHandler>();
        
        // Herbs模块 → 发布HerbStockChangedEvent
        services.AddEventHandlerWithSubscription<HerbStockChangedEvent, HerbStockChangedEventHandler>();
        
        // Formula模块 → 订阅HerbStockChangedEvent
        services.AddEventHandlerWithSubscription<HerbStockChangedEvent, FormulaAvailabilityEventHandler>();

        // ========== Step 3: 注册模块管理器 ==========
        services.AddSingleton<IModuleManager, ModuleManager>();

        // ========== Step 4: 其他服务注册 ==========
        // ...
    }

    public async Task Configure(IApplicationBuilder app, IModuleManager moduleManager)
    {
        // ========== Step 5: 注册所有业务模块 ==========
        await moduleManager.RegisterModuleAsync(new AuthModule(...));
        await moduleManager.RegisterModuleAsync(new UsersModule(...));
        await moduleManager.RegisterModuleAsync(new PatientsModule(...));
        await moduleManager.RegisterModuleAsync(new MedicalCaseModule(...));
        await moduleManager.RegisterModuleAsync(new ConsultationModule(...));
        await moduleManager.RegisterModuleAsync(new PrescriptionsModule(...));
        await moduleManager.RegisterModuleAsync(new HerbsModule(...));
        await moduleManager.RegisterModuleAsync(new FormulaModule(...));

        // ========== Step 6: 检查依赖关系 ==========
        var dependencies = moduleManager.CheckDependencies();
        if (!dependencies.IsValid)
        {
            throw new InvalidOperationException($"模块依赖检查失败: {dependencies.ErrorMessage}");
        }

        // ========== Step 7: 启动所有模块（按依赖顺序）==========
        await moduleManager.StartAllModulesAsync();

        // ========== Step 8: 订阅模块状态变更事件 ==========
        moduleManager.ModuleStateChanged += (sender, args) =>
        {
            _logger.LogInformation("模块 {ModuleId} 状态变更: {OldState} → {NewState}",
                args.ModuleId, args.OldState, args.NewState);
        };
    }
}
```

### 8.3 服务生命周期选择

| 类型 | 推荐生命周期 | 原因 |
|------|-------------|------|
| **IEventBus** | Singleton | 全局唯一，管理所有订阅 |
| **EventHandler** | Scoped | 每次事件处理创建新实例（隔离状态） |
| **IModuleManager** | Singleton | 全局唯一，管理所有模块 |
| **IModule** | Singleton | 模块生命周期与应用一致 |

**注意事项**:
- ⚠️ Handler为Scoped时，PublishAsync在无HTTP请求上下文时会失败 → **解决**: 使用`IServiceScopeFactory`创建作用域
- ⚠️ Handler依赖Scoped服务（如DbContext）时必须为Scoped

---

## 9. 最佳实践

### 9.1 事件设计原则

**原则1: 事件不可变（Immutable）**
```csharp
// ✅ 好的设计（只读属性）
public class PatientCreatedEvent : IntegrationEventBase
{
    public int PatientId { get; }
    public string PatientName { get; }
    
    public PatientCreatedEvent(int patientId, string patientName)
    {
        PatientId = patientId;
        PatientName = patientName;
    }
}

// ❌ 坏的设计（可变属性）
public class PatientCreatedEvent : IntegrationEventBase
{
    public int PatientId { get; set; } // 可能被处理器修改
    public string PatientName { get; set; }
}
```

**原则2: 事件携带必要数据（避免跨模块查询）**
```csharp
// ✅ 好的设计（包含业务所需的所有数据）
public class PatientCreatedEvent : IntegrationEventBase
{
    public int PatientId { get; }
    public string PatientName { get; }
    public string IdCard { get; }
    public DateTime CreatedAt { get; }
}

// ❌ 坏的设计（只有ID，处理器需要查询数据库）
public class PatientCreatedEvent : IntegrationEventBase
{
    public int PatientId { get; } // 处理器需要再查患者详情
}
```

**原则3: 事件命名遵循过去时（表示已发生）**
```csharp
✅ PatientCreatedEvent       // 患者已创建
✅ ConsultationCompletedEvent // 诊疗已完成
✅ PrescriptionIssuedEvent   // 处方已开具

❌ CreatePatientEvent        // 动词原形（误导）
❌ PatientCreateEvent        // 名词+动词（不清晰）
```

### 9.2 处理器设计原则

**原则1: 处理器保持幂等性（Idempotent）**
```csharp
// ✅ 幂等处理器（检查是否已处理）
public async Task HandleAsync(PatientCreatedEvent @event)
{
    // 检查是否已处理（基于事件ID）
    var exists = await _repository.ExistsByEventIdAsync(@event.Id);
    if (exists)
    {
        _logger.LogInformation("事件 {EventId} 已处理，跳过", @event.Id);
        return;
    }

    // 业务逻辑
    await _medicalCaseService.CreateAsync(...);

    // 记录已处理
    await _repository.SaveEventIdAsync(@event.Id);
}

// ❌ 非幂等处理器（重复处理会创建重复数据）
public async Task HandleAsync(PatientCreatedEvent @event)
{
    await _medicalCaseService.CreateAsync(...); // 直接创建，无检查
}
```

**原则2: 处理器异常必须捕获（避免影响其他处理器）**
```csharp
// ✅ 好的处理器（异常捕获）
public async Task HandleAsync(PatientCreatedEvent @event)
{
    try
    {
        await _medicalCaseService.CreateAsync(...);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "处理PatientCreatedEvent失败: {EventId}", @event.Id);
        // 可选：重试逻辑、补偿逻辑
    }
}

// ❌ 坏的处理器（异常未捕获，会被EventBus捕获但影响调试）
public async Task HandleAsync(PatientCreatedEvent @event)
{
    await _medicalCaseService.CreateAsync(...); // 异常会抛出到EventBus
}
```

**原则3: 避免处理器中执行长耗时操作**
```csharp
// ✅ 好的处理器（使用后台任务）
public async Task HandleAsync(PatientCreatedEvent @event)
{
    // 发送到后台队列处理
    await _backgroundTaskQueue.EnqueueAsync(async ct =>
    {
        await GeneratePdfReportAsync(@event.PatientId, ct);
    });
}

// ❌ 坏的处理器（阻塞事件处理）
public async Task HandleAsync(PatientCreatedEvent @event)
{
    await GeneratePdfReportAsync(@event.PatientId); // 5秒延迟
}
```

### 9.3 订阅管理原则

**原则1: 启动时订阅（避免运行时动态订阅）**
```csharp
// ✅ 推荐：Startup时订阅
services.AddEventHandlerWithSubscription<PatientCreatedEvent, PatientCreatedEventHandler>();

// ⚠️ 不推荐：运行时动态订阅（可能错过事件）
public void SomeMethod()
{
    _eventBus.Subscribe<PatientCreatedEvent, PatientCreatedEventHandler>();
}
```

**原则2: 避免重复订阅同一处理器**
```csharp
// ✅ InMemoryEventBus已自动去重
_eventBus.Subscribe<PatientCreatedEvent, Handler1>(); // 订阅1
_eventBus.Subscribe<PatientCreatedEvent, Handler1>(); // 自动忽略
// 结果：Handler1只注册一次
```

### 9.4 性能优化建议

**优化1: 限制并行处理器数量（避免线程池耗尽）**
```csharp
// 当处理器数量 >100时，考虑分批处理
var semaphore = new SemaphoreSlim(50); // 最多50个并发
var tasks = handlerTypes.Select(async handlerType =>
{
    await semaphore.WaitAsync();
    try
    {
        await ProcessEventAsync(@event, handlerType, cancellationToken);
    }
    finally
    {
        semaphore.Release();
    }
});
await Task.WhenAll(tasks);
```

**优化2: 使用ValueTask减少内存分配（高频事件）**
```csharp
// 改进IIntegrationEventHandler接口
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : class, IIntegrationEvent
{
    ValueTask HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
```

**优化3: 异步日志记录（减少同步开销）**
```csharp
// ✅ 使用高性能日志库（如Serilog异步写入）
_logger.LogInformation("发布事件: {EventType}", eventType.Name);

// ❌ 避免同步写文件
File.AppendAllText("log.txt", $"Event: {eventType.Name}"); // 阻塞
```

### 9.5 测试策略

**单元测试: 测试处理器逻辑**
```csharp
[Fact]
public async Task HandleAsync_创建病案成功()
{
    // Arrange
    var mockService = new Mock<IMedicalCaseService>();
    var handler = new PatientCreatedEventHandler(mockService.Object, ...);
    var @event = new PatientCreatedEvent(patientId: 123, patientName: "张三");

    // Act
    await handler.HandleAsync(@event);

    // Assert
    mockService.Verify(s => s.CreateAsync(
        It.Is<CreateMedicalCaseRequest>(r => r.PatientId == 123),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

**集成测试: 测试EventBus完整流程**
```csharp
[Fact]
public async Task PublishAsync_触发多个处理器()
{
    // Arrange
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddInMemoryEventBus();
    serviceCollection.AddEventHandlerWithSubscription<TestEvent, TestHandler1>();
    serviceCollection.AddEventHandlerWithSubscription<TestEvent, TestHandler2>();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var eventBus = serviceProvider.GetRequiredService<IEventBus>();

    // Act
    await eventBus.PublishAsync(new TestEvent());

    // Assert
    Assert.Equal(2, TestHandler1.CallCount); // 验证Handler1被调用
    Assert.Equal(2, TestHandler2.CallCount); // 验证Handler2被调用
}
```

---

## 10. MVP约束遵循

### 10.1 技术选型约束

| 技术 | MVP阶段 | 原因 | 未来演进 |
|------|---------|------|----------|
| **Redis** | ❌ 禁止 | 增加外部依赖，MVP不需要分布式缓存 | Phase 2考虑（性能优化） |
| **RabbitMQ** | ❌ 禁止 | 增加运维复杂度，单体架构无需外部MQ | Phase 3考虑（微服务化） |
| **MassTransit** | ❌ 禁止 | 过度设计，简单场景不需要复杂框架 | Phase 3考虑（分布式事务） |
| **MediatR** | ❌ 禁止 | 增加抽象层，MVP优先简单直接 | Phase 2考虑（CQRS需求） |
| **CQRS** | ❌ 禁止 | 过度设计，当前读写分离需求不明确 | Phase 3考虑（高并发优化） |
| **In-Memory EventBus** | ✅ 允许 | 简单、无外部依赖、够用 | 当前阶段 |
| **ConcurrentDictionary** | ✅ 允许 | .NET标准库，线程安全，性能优秀 | 持续使用 |

### 10.2 架构约束遵循

**约束1: 单体架构（Monolithic）**
- ✅ 所有模块运行在同一进程
- ✅ EventBus仅支持进程内通信
- ❌ 不支持跨进程/跨服务器通信

**约束2: 简化设计（KISS原则）**
- ✅ 代码简洁（InMemoryEventBus仅264行）
- ✅ 无复杂配置（3个扩展方法即可完成DI）
- ✅ 易于理解（清晰的接口设计）

**约束3: 够用即好（YAGNI原则）**
- ✅ 当前满足8个模块间通信需求
- ❌ 不实现分布式事务（当前无需求）
- ❌ 不实现事件溯源（当前无需求）

### 10.3 未来演进路径

**Phase 1 (当前MVP)**: In-Memory EventBus
- ✅ 进程内通信
- ✅ 简单、稳定、无外部依赖

**Phase 2 (性能优化)**: 引入Redis缓存 + 持久化事件日志
- 考虑场景：事件审计、失败重试
- 技术选型：Redis Streams（轻量级MQ功能）
- 架构变化：EventBus写入Redis，后台Worker消费

**Phase 3 (微服务化)**: 引入RabbitMQ + MassTransit
- 考虑场景：拆分为多个服务（如Patient Service、MedicalCase Service）
- 技术选型：RabbitMQ（成熟稳定）+ MassTransit（简化开发）
- 架构变化：跨服务事件通信、分布式事务（Saga）

---

## 11. 总结

### 11.1 核心优势

1. **简单易用**
   - ✅ 3个核心接口（IEventBus、IIntegrationEvent、IIntegrationEventHandler）
   - ✅ 1个基类（IntegrationEventBase）
   - ✅ 3个扩展方法（AddInMemoryEventBus、AddEventHandler、AddEventHandlerWithSubscription）

2. **高性能**
   - ✅ ConcurrentDictionary线程安全管理（无锁竞争）
   - ✅ Task.WhenAll并行处理（N个处理器耗时 ≈ max(handler_time)）
   - ✅ Interlocked原子操作（无线程阻塞）

3. **可观测性**
   - ✅ 完整的统计信息（发布/处理/失败计数）
   - ✅ 详细的日志记录（Info/Debug/Warning/Error）
   - ✅ 事件元数据（Id、OccurredOn、Source、Version）

4. **模块化支持**
   - ✅ 28个模块管理方法（注册/启动/停止/健康检查）
   - ✅ 5个模块事件（注册/状态变更/健康变更/依赖/注销）
   - ✅ 依赖分析与启动顺序解析

5. **MVP约束遵循**
   - ✅ 无外部依赖（Redis/RabbitMQ/MassTransit）
   - ✅ 简化设计（KISS原则）
   - ✅ 够用即好（YAGNI原则）

### 11.2 适用场景

**适合**:
- ✅ 单体架构（Monolithic）的模块间通信
- ✅ 松耦合业务协同（如患者创建 → 病案初始化）
- ✅ 异步事件驱动架构（Event-Driven Architecture）
- ✅ 8个业务模块的解耦通信

**不适合**:
- ❌ 微服务架构（需要RabbitMQ/Kafka）
- ❌ 分布式事务（需要Saga模式 + MassTransit）
- ❌ 跨进程/跨服务器通信
- ❌ 事件溯源（Event Sourcing）架构

### 11.3 关键文件速查

| 文件路径 | 作用 | 行数 |
|---------|------|------|
| `Abstractions/IEventBus.cs` | 事件总线核心接口 | 101 |
| `Abstractions/IIntegrationEvent.cs` | 集成事件接口 | 34 |
| `Abstractions/IIntegrationEventHandler.cs` | 事件处理器接口 | 34 |
| `Events/IntegrationEventBase.cs` | 事件基类实现 | 95 |
| `Implementation/InMemoryEventBus.cs` | 事件总线实现 | 264 |
| `Services/EventBusHostedService.cs` | 自动订阅托管服务 | 115 |
| `Extensions/ServiceCollectionExtensions.cs` | DI注册扩展 | 118 |
| `Module/Events/ModuleRegisteredEvent.cs` | 模块注册事件示例 | 81 |

### 11.4 延伸阅读

**核心文档**:
- [Server端架构总览](README.md) - 三层架构整体设计
- [EventBus集成指南](../../how-to-guides/server/eventbus-integration.md) - 实践指南
- [模块化架构](README.md#模块化架构) - 8个业务模块设计

**参考文档**:
- [Client端架构设计](../client/README.md) - Prism EventAggregator对比
- [共享架构设计](../shared/README.md) - 跨端通信机制

**代码参考**:
- `src/Server/Core/LYBT.Core.EventBus/README.md` - 项目说明
- `docs/quick-reference/code-patterns.md` - 事件模式速查

---

**文档版本**: 1.0.0  
**最后更新**: 2025-10-30  
**审阅状态**: ✅ Phase 2架构文档 - 待审核  
**下一步**: 完成剩余8个Phase 2文档
