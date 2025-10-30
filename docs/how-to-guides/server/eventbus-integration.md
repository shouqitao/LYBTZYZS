# Server端事件总线集成指南

## 📌 概述

本指南介绍如何在Server端集成和使用事件总线（EventBus）实现模块间异步通信。事件总线是LYBTZYZS项目中实现模块解耦的关键机制，遵循MVP原则采用内存实现，避免引入Redis、RabbitMQ等外部依赖。

### 架构图

```
┌────────────────────────────────────────────────────────┐
│              Server端模块通信架构                         │
├────────────────────────────────────────────────────────┤
│                                                          │
│  Module A (发布者)                                        │
│  ├─ Service                                              │
│  │  └─ await _eventBus.PublishAsync(event)             │
│  │                                                       │
│  │         ↓                                             │
│  │                                                       │
│  ├─────►  IEventBus (单例)                               │
│  │        ├─ InMemoryEventBus                            │
│  │        ├─ ConcurrentDictionary订阅管理                │
│  │        └─ 并行处理所有Handler                          │
│  │                                                       │
│  │         ↓                                             │
│  │                                                       │
│  └─────►  Module B (订阅者)                              │
│           ├─ EventHandler实现                           │
│           │  └─ HandleAsync(event)                      │
│           └─ ServiceCollection注册                       │
│                                                          │
│  EventBusHostedService (后台服务)                         │
│  └─ 应用启动时自动订阅所有Handler                          │
│                                                          │
└────────────────────────────────────────────────────────┘
```

### 技术栈

- **.NET 8**：基础框架
- **IHostedService**：后台服务托管
- **ConcurrentDictionary**：线程安全的订阅管理
- **IServiceProvider**：服务解析和依赖注入
- **ILogger**：日志记录
- **Task.WhenAll**：并行事件处理

### MVP约束遵循

✅ **符合Constitution约束**：
- 内存实现（避免Redis、RabbitMQ等外部依赖）
- 简化的发布订阅模式（避免CQRS、MediatR等复杂框架）
- 单例EventBus（避免分布式复杂性）
- 异步处理（性能优化）

---

## 1. 核心接口设计

### 1.1 IEventBus接口

```csharp
/// <summary>
/// 事件总线接口
/// 负责事件的发布、订阅和路由
/// </summary>
public interface IEventBus
{
    // 发布事件
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;

    // 订阅事件处理器（泛型）
    bool Subscribe<TEvent, THandler>()
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>;

    // 订阅事件处理器（类型）
    bool Subscribe(Type eventType, Type handlerType);

    // 取消订阅
    bool Unsubscribe<TEvent, THandler>()
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>;

    // 获取订阅数量
    int GetSubscriptionCount<TEvent>() where TEvent : class, IIntegrationEvent;

    // 获取所有已注册事件类型
    IReadOnlyCollection<Type> GetRegisteredEventTypes();

    // 清空所有订阅
    void ClearSubscriptions();
}
```

**关键设计**：
- ✅ 泛型约束确保类型安全
- ✅ 异步发布避免阻塞调用者
- ✅ 订阅管理支持动态添加/移除
- ✅ 统计信息支持监控和调试

### 1.2 IIntegrationEvent接口

```csharp
/// <summary>
/// 集成事件基础接口
/// 用于模块间异步通信的事件标记
/// </summary>
public interface IIntegrationEvent
{
    Guid Id { get; }                  // 事件唯一标识
    DateTime OccurredOn { get; }       // 事件创建时间
    string EventType { get; }          // 事件类型名称
    string Source { get; }             // 事件来源模块
    int Version { get; }               // 事件版本（向后兼容）
}
```

**设计考量**：
- **Id**：幂等性处理和事件追踪
- **OccurredOn**：事件时序和审计
- **EventType**：类型名称便于日志和监控
- **Source**：来源模块识别（如"Module.Users"）
- **Version**：版本控制支持演进

### 1.3 IIntegrationEventHandler接口

```csharp
/// <summary>
/// 泛型事件处理器接口
/// </summary>
public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
    where TEvent : class, IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// 事件处理器基础接口
/// </summary>
public interface IIntegrationEventHandler
{
    string HandlerName { get; }    // 处理器名称
    Type EventType { get; }        // 支持的事件类型
}
```

**Handler设计原则**：
- ✅ 异步处理避免阻塞事件总线
- ✅ 支持CancellationToken优雅取消
- ✅ 逆变泛型（in TEvent）支持协变
- ✅ HandlerName用于日志和调试

---

## 2. IntegrationEventBase基类

所有集成事件应继承 `IntegrationEventBase`，提供通用实现：

```csharp
/// <summary>
/// 集成事件基础类
/// </summary>
public abstract class IntegrationEventBase : IIntegrationEvent
{
    protected IntegrationEventBase()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        EventType = GetType().Name;
        Version = 1;
    }

    protected IntegrationEventBase(string source) : this()
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public Guid Id { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public string EventType { get; private set; }
    public string Source { get; private set; } = "Unknown";
    public virtual int Version { get; protected set; }

    // 获取事件描述（用于日志）
    public virtual string GetDescription()
    {
        return $"{EventType} from {Source} at {OccurredOn:yyyy-MM-dd HH:mm:ss}";
    }

    // 相等性比较（基于ID）
    public override bool Equals(object? obj)
    {
        if (obj is not IntegrationEventBase other)
            return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
    {
        return $"[{EventType}] {Id} ({Source}) - {OccurredOn:yyyy-MM-dd HH:mm:ss}";
    }
}
```

**使用示例**：

```csharp
/// <summary>
/// 用户创建事件
/// </summary>
public class UserCreatedEvent : IntegrationEventBase
{
    public Guid UserId { get; }
    public string UserName { get; }
    public DateTime CreatedAt { get; }

    public UserCreatedEvent(Guid userId, string userName, string source = "Module.Users")
        : base(source)
    {
        UserId = userId;
        UserName = userName ?? throw new ArgumentNullException(nameof(userName));
        CreatedAt = DateTime.UtcNow;
    }

    public override string GetDescription()
    {
        return $"用户 '{UserName}' (ID: {UserId}) 已创建于 {CreatedAt:yyyy-MM-dd HH:mm:ss}";
    }
}
```

---

## 3. InMemoryEventBus实现

### 3.1 核心实现

```csharp
/// <summary>
/// 内存事件总线实现
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Type>> _subscriptions;
    private long _totalPublishedEvents;
    private long _totalProcessedEvents;
    private long _failedEvents;

    public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subscriptions = new ConcurrentDictionary<Type, ConcurrentBag<Type>>();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        var eventType = typeof(TEvent);
        _logger.LogInformation("发布事件: {EventType}, ID: {EventId}", eventType.Name, @event.Id);

        try
        {
            Interlocked.Increment(ref _totalPublishedEvents);

            // 获取订阅的处理器
            if (!_subscriptions.TryGetValue(eventType, out var handlerTypes))
            {
                _logger.LogWarning("没有找到事件 {EventType} 的处理器", eventType.Name);
                return;
            }

            // 并行处理所有订阅的处理器
            var tasks = handlerTypes.Select(ht => ProcessEventAsync(@event, ht, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation("事件 {EventType} 处理完成，共处理 {HandlerCount} 个处理器",
                eventType.Name, handlerTypes.Count);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedEvents);
            _logger.LogError(ex, "发布事件 {EventType} 时发生异常", eventType.Name);
            throw;
        }
    }

    private async Task ProcessEventAsync(IIntegrationEvent @event, Type handlerType, CancellationToken ct)
    {
        try
        {
            // 从服务容器获取处理器实例
            var handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
            {
                _logger.LogWarning("无法从服务容器获取处理器实例: {HandlerType}", handlerType.Name);
                return;
            }

            // 反射调用HandleAsync方法
            var method = handlerType.GetMethod("HandleAsync");
            if (method == null)
            {
                _logger.LogWarning("处理器 {HandlerType} 没有HandleAsync方法", handlerType.Name);
                return;
            }

            var result = method.Invoke(handler, new object[] { @event, ct });
            if (result is Task task)
            {
                await task;
            }

            Interlocked.Increment(ref _totalProcessedEvents);
            _logger.LogDebug("事件处理器 {HandlerType} 成功处理事件", handlerType.Name);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedEvents);
            _logger.LogError(ex, "事件处理器 {HandlerType} 处理事件时发生异常", handlerType.Name);
            // 不重新抛出异常，避免影响其他处理器
        }
    }
}
```

**关键实现要点**：
1. **ConcurrentDictionary**：线程安全的订阅管理（支持并发发布）
2. **Task.WhenAll**：并行处理所有Handler（提升吞吐量）
3. **Interlocked**：原子操作保证统计准确性
4. **反射调用**：动态解析和调用HandleAsync方法
5. **异常隔离**：单个Handler异常不影响其他Handler

### 3.2 订阅管理

```csharp
public bool Subscribe<TEvent, THandler>()
    where TEvent : class, IIntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    return Subscribe(typeof(TEvent), typeof(THandler));
}

public bool Subscribe(Type eventType, Type handlerType)
{
    // 验证事件类型
    if (!typeof(IIntegrationEvent).IsAssignableFrom(eventType))
        throw new ArgumentException($"事件类型 {eventType.Name} 必须实现 IIntegrationEvent 接口");

    // 验证处理器类型
    var expectedHandlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
    if (!expectedHandlerInterface.IsAssignableFrom(handlerType))
        throw new ArgumentException($"处理器类型 {handlerType.Name} 必须实现 {expectedHandlerInterface.Name} 接口");

    try
    {
        _subscriptions.AddOrUpdate(
            eventType,
            new ConcurrentBag<Type> { handlerType },
            (key, existing) =>
            {
                if (!existing.Contains(handlerType))
                    existing.Add(handlerType);
                return existing;
            });

        _logger.LogInformation("成功订阅事件处理器: {EventType} -> {HandlerType}",
            eventType.Name, handlerType.Name);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "订阅事件处理器失败");
        return false;
    }
}
```

---

## 4. 依赖注入注册

### 4.1 注册事件总线

在 `Program.cs` 或模块注册类中：

```csharp
using LYBT.Core.EventBus.Extensions;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 注册事件总线（单例 + HostedService）
        builder.Services.AddInMemoryEventBus();

        // 注册其他服务...
        var app = builder.Build();
        app.Run();
    }
}
```

**AddInMemoryEventBus扩展方法**：

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        // 1. 注册事件总线为单例
        services.TryAddSingleton<IEventBus, InMemoryEventBus>();

        // 2. 注册订阅配置选项
        services.Configure<EventBusSubscriptionOptions>(_ => { });

        // 3. 注册托管服务（自动订阅）
        services.AddHostedService<EventBusHostedService>();

        return services;
    }
}
```

### 4.2 注册事件处理器

**方式1：手动注册Handler并自动订阅**

```csharp
public class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        // 注册Service
        services.AddScoped<IUserService, UserService>();

        // 注册事件处理器（自动订阅）
        services.AddEventHandlerWithSubscription<UserCreatedEvent, UserCreatedEventHandler>(
            ServiceLifetime.Scoped);

        return services;
    }
}
```

**方式2：分离注册和订阅**

```csharp
public class PatientsModule
{
    public static IServiceCollection AddPatientsModule(this IServiceCollection services)
    {
        // 1. 注册Handler
        services.AddEventHandler<UserCreatedEvent, PatientCreatedFromUserHandler>(
            ServiceLifetime.Scoped);

        // 2. 手动配置订阅（可选）
        services.Configure<EventBusSubscriptionOptions>(options =>
        {
            options.AddSubscription<UserCreatedEvent, PatientCreatedFromUserHandler>();
        });

        return services;
    }
}
```

**AddEventHandler扩展方法**：

```csharp
public static IServiceCollection AddEventHandler<TEvent, THandler>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
    where TEvent : class, IIntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    // 注册处理器到DI容器
    services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
    return services;
}

public static IServiceCollection AddEventHandlerWithSubscription<TEvent, THandler>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
    where TEvent : class, IIntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    // 1. 注册处理器
    services.AddEventHandler<TEvent, THandler>(lifetime);

    // 2. 添加配置回调，在服务构建完成后自动订阅
    services.Configure<EventBusSubscriptionOptions>(options =>
    {
        options.AddSubscription<TEvent, THandler>();
    });

    return services;
}
```

---

## 5. EventBusHostedService后台服务

### 5.1 自动订阅机制

```csharp
/// <summary>
/// 事件总线托管服务
/// 负责在应用程序启动时自动订阅配置的事件处理器
/// </summary>
public class EventBusHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusHostedService> _logger;
    private readonly EventBusSubscriptionOptions _subscriptionOptions;

    public EventBusHostedService(
        IServiceProvider serviceProvider,
        ILogger<EventBusHostedService> logger,
        IOptions<EventBusSubscriptionOptions> subscriptionOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _subscriptionOptions = subscriptionOptions.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始启动事件总线托管服务");

        try
        {
            var eventBus = _serviceProvider.GetRequiredService<IEventBus>();
            var subscriptionCount = 0;

            // 订阅所有配置的事件处理器
            foreach (var (eventType, handlerType) in _subscriptionOptions.Subscriptions)
            {
                var success = eventBus.Subscribe(eventType, handlerType);
                if (success)
                {
                    subscriptionCount++;
                    _logger.LogDebug("成功订阅事件处理器: {EventType} -> {HandlerType}",
                        eventType.Name, handlerType.Name);
                }
            }

            _logger.LogInformation("事件总线托管服务启动完成，共订阅 {SubscriptionCount} 个事件处理器",
                subscriptionCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动事件总线托管服务时发生异常");
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始停止事件总线托管服务");

        var eventBus = _serviceProvider.GetService<IEventBus>();
        if (eventBus != null)
        {
            eventBus.ClearSubscriptions();
            _logger.LogInformation("已清空所有事件订阅");
        }

        await Task.CompletedTask;
    }
}
```

**工作流程**：
1. ✅ **StartAsync**：应用启动时自动订阅所有配置的Handler
2. ✅ **StopAsync**：应用停止时清空所有订阅
3. ✅ **异常处理**：订阅失败记录日志但不中断应用启动
4. ✅ **日志记录**：详细记录订阅过程便于调试

---

## 6. 发布事件（Publisher）

### 6.1 Service层发布事件

```csharp
/// <summary>
/// 用户服务
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IEventBus eventBus,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        // 1. 创建用户实体
        var user = new UserModel
        {
            Id = Guid.NewGuid(),
            UserName = dto.UserName,
            Email = dto.Email,
            CreatedAt = DateTime.UtcNow
        };

        // 2. 保存到数据库
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("用户创建成功: {UserId} - {UserName}", user.Id, user.UserName);

        // 3. 发布用户创建事件
        var userCreatedEvent = new UserCreatedEvent(
            user.Id,
            user.UserName,
            source: "Module.Users");

        try
        {
            await _eventBus.PublishAsync(userCreatedEvent);
            _logger.LogInformation("用户创建事件已发布: {EventId}", userCreatedEvent.Id);
        }
        catch (Exception ex)
        {
            // 事件发布失败不影响业务逻辑
            _logger.LogError(ex, "发布用户创建事件失败: {UserId}", user.Id);
        }

        // 4. 返回DTO
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email
        };
    }
}
```

**发布事件最佳实践**：
1. ✅ **业务逻辑优先**：先完成核心业务（保存数据库），再发布事件
2. ✅ **异常隔离**：事件发布失败不影响业务逻辑执行
3. ✅ **日志记录**：记录事件发布成功/失败便于追踪
4. ✅ **Source指定**：明确事件来源模块（如"Module.Users"）

### 6.2 Controller层发布事件（❌ 不推荐）

```csharp
// ❌ 错误示例：Controller直接发布事件
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IEventBus _eventBus; // ❌ Controller不应依赖EventBus

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var user = await _userService.CreateAsync(dto);

        // ❌ Controller不应直接发布事件
        await _eventBus.PublishAsync(new UserCreatedEvent(user.Id, user.UserName));

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
}
```

```csharp
// ✅ 正确示例：Service层发布事件
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // Service内部会发布UserCreatedEvent
        var user = await _userService.CreateAsync(dto);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
}
```

---

## 7. 处理事件（Subscriber）

### 7.1 实现事件处理器

```csharp
/// <summary>
/// 用户创建事件处理器（患者模块订阅）
/// </summary>
public class PatientCreatedFromUserHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientCreatedFromUserHandler> _logger;

    public string HandlerName => "PatientCreatedFromUserHandler";
    public Type EventType => typeof(UserCreatedEvent);

    public PatientCreatedFromUserHandler(
        IPatientRepository patientRepository,
        ILogger<PatientCreatedFromUserHandler> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始处理用户创建事件: {EventId} - User: {UserName}",
            @event.Id, @event.UserName);

        try
        {
            // 1. 检查是否已存在对应患者（幂等性）
            var existingPatient = await _patientRepository.GetByUserIdAsync(@event.UserId);
            if (existingPatient != null)
            {
                _logger.LogWarning("患者已存在，跳过创建: UserId={UserId}", @event.UserId);
                return;
            }

            // 2. 创建患者档案
            var patient = new PatientModel
            {
                Id = Guid.NewGuid(),
                UserId = @event.UserId,
                Name = @event.UserName,
                CreatedAt = DateTime.UtcNow,
                Source = "UserCreatedEvent"
            };

            await _patientRepository.AddAsync(patient);
            await _patientRepository.SaveChangesAsync();

            _logger.LogInformation("患者档案创建成功: PatientId={PatientId}, UserId={UserId}",
                patient.Id, @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理用户创建事件失败: EventId={EventId}, UserId={UserId}",
                @event.Id, @event.UserId);
            // ❌ 不重新抛出异常，避免影响其他处理器
        }
    }
}
```

**Handler实现最佳实践**：
1. ✅ **幂等性处理**：检查是否已处理过该事件（基于Event.Id或业务唯一键）
2. ✅ **异常捕获**：捕获并记录异常，不重新抛出影响其他Handler
3. ✅ **日志记录**：详细记录处理过程便于追踪和调试
4. ✅ **CancellationToken**：支持优雅取消
5. ✅ **业务验证**：验证事件数据完整性和合法性

### 7.2 处理多个事件类型

```csharp
/// <summary>
/// 用户事件聚合处理器
/// </summary>
public class UserEventAggregateHandler :
    IIntegrationEventHandler<UserCreatedEvent>,
    IIntegrationEventHandler<UserUpdatedEvent>,
    IIntegrationEventHandler<UserDeletedEvent>
{
    private readonly ILogger<UserEventAggregateHandler> _logger;

    public string HandlerName => "UserEventAggregateHandler";
    public Type EventType => typeof(object); // 处理多种事件类型

    public UserEventAggregateHandler(ILogger<UserEventAggregateHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation("处理用户创建事件: {UserId}", @event.UserId);
        // 处理逻辑...
        await Task.CompletedTask;
    }

    public async Task HandleAsync(UserUpdatedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation("处理用户更新事件: {UserId}", @event.UserId);
        // 处理逻辑...
        await Task.CompletedTask;
    }

    public async Task HandleAsync(UserDeletedEvent @event, CancellationToken ct = default)
    {
        _logger.LogInformation("处理用户删除事件: {UserId}", @event.UserId);
        // 处理逻辑...
        await Task.CompletedTask;
    }
}
```

**注册聚合处理器**：

```csharp
public static IServiceCollection AddUserEventHandlers(this IServiceCollection services)
{
    // 注册Handler一次
    services.AddScoped<UserEventAggregateHandler>();

    // 订阅三个事件类型
    services.Configure<EventBusSubscriptionOptions>(options =>
    {
        options.AddSubscription<UserCreatedEvent, UserEventAggregateHandler>();
        options.AddSubscription<UserUpdatedEvent, UserEventAggregateHandler>();
        options.AddSubscription<UserDeletedEvent, UserEventAggregateHandler>();
    });

    return services;
}
```

---

## 8. 常见问题与反模式

### 8.1 问题1：Handler未被调用

**现象**：事件发布成功，但Handler的HandleAsync方法未执行。

**❌ 错误原因1：Handler未注册到DI容器**

```csharp
// ❌ 错误：只订阅但未注册Handler
services.Configure<EventBusSubscriptionOptions>(options =>
{
    options.AddSubscription<UserCreatedEvent, UserCreatedEventHandler>();
});
// Handler无法从IServiceProvider解析
```

**✅ 正确做法**：

```csharp
// ✅ 正确：先注册Handler到DI
services.AddScoped<UserCreatedEventHandler>();

// 然后订阅
services.Configure<EventBusSubscriptionOptions>(options =>
{
    options.AddSubscription<UserCreatedEvent, UserCreatedEventHandler>();
});

// 或使用便捷方法
services.AddEventHandlerWithSubscription<UserCreatedEvent, UserCreatedEventHandler>();
```

**❌ 错误原因2：EventBusHostedService未启动**

```csharp
// ❌ 错误：未调用AddInMemoryEventBus
services.AddSingleton<IEventBus, InMemoryEventBus>();
// EventBusHostedService未注册，订阅未执行
```

**✅ 正确做法**：

```csharp
// ✅ 正确：使用AddInMemoryEventBus（自动注册HostedService）
services.AddInMemoryEventBus();
```

### 8.2 问题2：事件发布阻塞调用者

**现象**：PublishAsync调用导致调用者线程长时间等待。

**❌ 错误示例：Handler中执行同步阻塞操作**

```csharp
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
{
    // ❌ 错误：同步阻塞操作
    Thread.Sleep(5000);

    // ❌ 错误：阻塞式HTTP调用
    var client = new HttpClient();
    var response = client.GetAsync("https://api.example.com/notify").Result;
}
```

**✅ 正确示例：Handler使用异步操作**

```csharp
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
{
    // ✅ 正确：异步延迟
    await Task.Delay(5000, ct);

    // ✅ 正确：异步HTTP调用
    using var client = new HttpClient();
    var response = await client.GetAsync("https://api.example.com/notify", ct);
}
```

### 8.3 问题3：单个Handler异常导致其他Handler不执行

**现象**：Handler A抛出异常后，Handler B未执行。

**❌ 错误示例：Handler重新抛出异常**

```csharp
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
{
    try
    {
        // 业务逻辑...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "处理事件失败");
        throw; // ❌ 重新抛出异常会传播到EventBus
    }
}
```

**✅ 正确示例：Handler捕获异常不重新抛出**

```csharp
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
{
    try
    {
        // 业务逻辑...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "处理事件失败: {EventId}", @event.Id);
        // ✅ 不重新抛出，记录日志即可
    }
}
```

**InMemoryEventBus已做异常隔离**：

```csharp
private async Task ProcessEventAsync(IIntegrationEvent @event, Type handlerType, CancellationToken ct)
{
    try
    {
        // 调用Handler...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "事件处理器异常");
        // ✅ EventBus不重新抛出，确保其他Handler继续执行
    }
}
```

### 8.4 问题4：订阅顺序依赖导致执行失败

**现象**：期望Handler A先执行再执行Handler B，但实际顺序不确定。

**❌ 错误示例：依赖Handler执行顺序**

```csharp
// ❌ 错误：Handler B依赖Handler A的执行结果
public class HandlerA : IIntegrationEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // 创建患者档案
        await _patientRepository.AddAsync(patient);
    }
}

public class HandlerB : IIntegrationEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // ❌ 假设患者档案已存在（可能Handler A还未执行完）
        var patient = await _patientRepository.GetByUserIdAsync(@event.UserId);
        // ...
    }
}
```

**✅ 正确示例1：Handler独立性（推荐）**

```csharp
// ✅ 正确：每个Handler独立完成逻辑，不依赖其他Handler
public class HandlerA : IIntegrationEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // 创建患者档案
        await _patientRepository.AddAsync(patient);
    }
}

public class HandlerB : IIntegrationEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // ✅ 不依赖HandlerA，独立处理业务逻辑
        var notification = CreateNotification(@event.UserId, @event.UserName);
        await _notificationService.SendAsync(notification);
    }
}
```

**✅ 正确示例2：使用链式事件（复杂场景）**

```csharp
// Handler A发布新事件
public class HandlerA : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly IEventBus _eventBus;

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // 创建患者档案
        var patient = await _patientRepository.AddAsync(patient);

        // ✅ 发布新事件
        await _eventBus.PublishAsync(new PatientCreatedEvent(patient.Id, @event.UserId));
    }
}

// Handler B订阅PatientCreatedEvent
public class HandlerB : IIntegrationEventHandler<PatientCreatedEvent>
{
    public async Task HandleAsync(PatientCreatedEvent @event, CancellationToken ct)
    {
        // ✅ 明确依赖患者档案已创建
        var patient = await _patientRepository.GetByIdAsync(@event.PatientId);
        // ...
    }
}
```

### 8.5 问题5：EventBus内存泄漏

**现象**：长时间运行后，EventBus占用内存持续增长。

**❌ 错误原因1：Handler未正确释放资源**

```csharp
public class LeakyHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly List<string> _cache = new(); // ❌ 静态或实例字段累积数据

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        _cache.Add(@event.Id.ToString()); // ❌ 数据持续累积
        // ...
    }
}
```

**✅ 正确做法**：

```csharp
public class ProperHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly IMemoryCache _cache; // ✅ 使用IMemoryCache（有过期策略）

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        _cache.Set(@event.Id, @event, TimeSpan.FromMinutes(10)); // ✅ 设置过期时间
        // ...
    }
}
```

**❌ 错误原因2：订阅未清理**

```csharp
// ❌ 错误：动态订阅但从不取消订阅
for (int i = 0; i < 1000; i++)
{
    eventBus.Subscribe<UserCreatedEvent, DynamicHandler>();
}
// 订阅数持续增长
```

**✅ 正确做法**：

```csharp
// ✅ 正确：使用EventBusHostedService管理订阅生命周期
// 应用停止时自动清理
services.AddInMemoryEventBus();
```

### 8.6 问题6：并发发布导致Handler处理混乱

**现象**：多个事件并发发布时，Handler处理状态不一致。

**❌ 错误示例：Handler使用非线程安全的共享状态**

```csharp
public class StatefulHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private int _processedCount = 0; // ❌ 实例字段在并发场景下不安全

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        _processedCount++; // ❌ 非原子操作，并发时可能丢失计数
        _logger.LogInformation("已处理 {Count} 个事件", _processedCount);
    }
}
```

**✅ 正确示例1：无状态Handler（推荐）**

```csharp
public class StatelessHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // ✅ 完全无状态，每次处理独立
        var patient = await _patientRepository.AddAsync(patient);
    }
}
```

**✅ 正确示例2：使用线程安全的共享状态**

```csharp
public class ThreadSafeHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private int _processedCount = 0;

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // ✅ 使用Interlocked保证原子性
        Interlocked.Increment(ref _processedCount);
        _logger.LogInformation("已处理 {Count} 个事件", _processedCount);
    }
}
```

### 8.7 问题7：事件版本不兼容

**现象**：事件结构变更后，旧版Handler处理新版事件失败。

**❌ 错误示例：直接修改事件结构**

```csharp
// V1版本
public class UserCreatedEvent : IntegrationEventBase
{
    public Guid UserId { get; }
    public string UserName { get; }
}

// ❌ V2版本：直接删除属性
public class UserCreatedEvent : IntegrationEventBase
{
    public Guid UserId { get; }
    // public string UserName { get; } // ❌ 删除属性破坏向后兼容
    public string FullName { get; } // 新增属性
}
```

**✅ 正确示例：保持向后兼容**

```csharp
// V2版本：保持UserName，添加FullName
public class UserCreatedEvent : IntegrationEventBase
{
    public Guid UserId { get; }
    public string UserName { get; } // ✅ 保留旧属性

    // ✅ 新增可选属性
    public string? FullName { get; }

    // ✅ 提供多个构造函数兼容旧代码
    public UserCreatedEvent(Guid userId, string userName, string source = "Module.Users")
        : base(source)
    {
        UserId = userId;
        UserName = userName;
        Version = 1; // V1事件
    }

    public UserCreatedEvent(Guid userId, string userName, string fullName, string source = "Module.Users")
        : base(source)
    {
        UserId = userId;
        UserName = userName;
        FullName = fullName;
        Version = 2; // V2事件
    }
}
```

**✅ Handler处理多版本事件**：

```csharp
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
{
    // ✅ 根据版本号处理不同逻辑
    if (@event.Version == 1)
    {
        // V1逻辑
        _logger.LogInformation("处理V1事件: UserName={UserName}", @event.UserName);
    }
    else if (@event.Version == 2)
    {
        // V2逻辑
        _logger.LogInformation("处理V2事件: FullName={FullName}", @event.FullName ?? @event.UserName);
    }
}
```

---

## 9. 完整集成检查清单

### 9.1 事件定义检查清单

- [ ] **事件类继承IntegrationEventBase**
- [ ] **Source参数明确指定来源模块**
- [ ] **事件属性为只读（get-only或private set）**
- [ ] **事件类包含GetDescription方法便于日志**
- [ ] **复杂事件提供ToString重写**
- [ ] **事件版本号明确定义（Version属性）**
- [ ] **构造函数进行参数验证（非空检查）**

### 9.2 Handler实现检查清单

- [ ] **实现IIntegrationEventHandler<TEvent>接口**
- [ ] **HandleAsync方法为异步（无阻塞操作）**
- [ ] **捕获异常不重新抛出**
- [ ] **实现幂等性处理逻辑**
- [ ] **使用ILogger记录详细日志**
- [ ] **支持CancellationToken优雅取消**
- [ ] **HandlerName属性返回有意义的名称**
- [ ] **无状态设计或使用线程安全的共享状态**

### 9.3 DI注册检查清单

- [ ] **调用AddInMemoryEventBus注册事件总线**
- [ ] **使用AddEventHandler注册Handler到DI**
- [ ] **使用AddEventHandlerWithSubscription自动订阅**
- [ ] **或手动Configure<EventBusSubscriptionOptions>订阅**
- [ ] **Handler生命周期正确设置（Scoped/Singleton）**
- [ ] **EventBusHostedService已注册为HostedService**

### 9.4 发布事件检查清单

- [ ] **在Service层发布事件（不在Controller层）**
- [ ] **业务逻辑完成后再发布事件**
- [ ] **使用try-catch捕获发布异常**
- [ ] **事件发布失败不影响业务逻辑**
- [ ] **记录事件发布成功/失败日志**
- [ ] **Source参数明确指定**

### 9.5 测试检查清单

- [ ] **单元测试：验证Handler逻辑正确性**
- [ ] **单元测试：验证Handler幂等性**
- [ ] **单元测试：验证Handler异常处理**
- [ ] **集成测试：验证事件发布到Handler流程**
- [ ] **集成测试：验证多Handler并行执行**
- [ ] **集成测试：验证EventBusHostedService自动订阅**
- [ ] **性能测试：验证高并发发布场景**

---

## 10. 最佳实践

### 10.1 事件设计最佳实践

**1. 事件名称使用过去式**

```csharp
// ✅ 正确：UserCreatedEvent, UserUpdatedEvent（已发生的事实）
public class UserCreatedEvent : IntegrationEventBase { }

// ❌ 错误：CreateUserEvent（命令，不是事件）
public class CreateUserEvent : IntegrationEventBase { }
```

**2. 事件包含足够的上下文信息**

```csharp
// ✅ 正确：包含完整上下文
public class OrderPlacedEvent : IntegrationEventBase
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }
    public List<OrderItemDto> Items { get; }
    public DateTime OrderTime { get; }
}

// ❌ 错误：信息不足，Handler需要额外查询
public class OrderPlacedEvent : IntegrationEventBase
{
    public Guid OrderId { get; } // 只有ID，Handler需查询订单详情
}
```

**3. 避免事件携带过多数据**

```csharp
// ❌ 错误：事件携带大量数据（如完整实体、二进制数据）
public class UserCreatedEvent : IntegrationEventBase
{
    public UserModel User { get; } // ❌ 完整实体（可能包含导航属性）
    public byte[] ProfilePicture { get; } // ❌ 二进制数据
}

// ✅ 正确：事件携带必要信息，Handler按需查询
public class UserCreatedEvent : IntegrationEventBase
{
    public Guid UserId { get; }
    public string UserName { get; }
    public string Email { get; }
    // Handler需要ProfilePicture时自行查询
}
```

### 10.2 Handler设计最佳实践

**1. Handler单一职责**

```csharp
// ✅ 正确：每个Handler专注单一职责
public class CreatePatientFromUserHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // 只负责创建患者档案
        await _patientRepository.AddAsync(patient);
    }
}

public class SendWelcomeEmailHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // 只负责发送欢迎邮件
        await _emailService.SendWelcomeEmailAsync(@event.UserName, @event.Email);
    }
}

// ❌ 错误：Handler承担多个职责
public class UserCreatedHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // ❌ 同时做多件事
        await _patientRepository.AddAsync(patient);
        await _emailService.SendWelcomeEmailAsync(@event.UserName, @event.Email);
        await _notificationService.SendAsync(notification);
    }
}
```

**2. Handler避免复杂业务逻辑**

```csharp
// ❌ 错误：Handler包含复杂业务逻辑
public class ComplexHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // ❌ 复杂的业务规则计算
        var discount = CalculateDiscountBasedOnBusinessRules(@event.UserId);
        var creditScore = await _creditService.CalculateCreditScoreAsync(@event.UserId);
        // ...大量业务逻辑...
    }
}

// ✅ 正确：Handler委托给Service处理业务逻辑
public class SimplifiedHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly IUserOnboardingService _onboardingService;

    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // ✅ 委托给专门的Service
        await _onboardingService.OnboardNewUserAsync(@event.UserId);
    }
}
```

**3. Handler处理幂等性**

```csharp
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
{
    // ✅ 幂等性检查：基于事件ID
    var processed = await _eventLogRepository.IsProcessedAsync(@event.Id);
    if (processed)
    {
        _logger.LogInformation("事件已处理，跳过: {EventId}", @event.Id);
        return;
    }

    // 执行业务逻辑
    await _patientRepository.AddAsync(patient);

    // ✅ 记录事件已处理
    await _eventLogRepository.MarkAsProcessedAsync(@event.Id);
}
```

### 10.3 性能优化最佳实践

**1. 使用批量操作减少Handler数量**

```csharp
// ❌ 错误：每次发布一个事件
foreach (var user in users)
{
    await _eventBus.PublishAsync(new UserCreatedEvent(user.Id, user.UserName));
}
// Handler被调用N次

// ✅ 正确：批量事件
public class UsersCreatedBatchEvent : IntegrationEventBase
{
    public List<UserDto> Users { get; }
}

await _eventBus.PublishAsync(new UsersCreatedBatchEvent(users));
// Handler只被调用1次
```

**2. Handler避免阻塞操作**

```csharp
// ❌ 错误：Handler中有阻塞操作
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
{
    Thread.Sleep(5000); // ❌ 阻塞5秒
    var response = _httpClient.GetAsync("...").Result; // ❌ 阻塞式HTTP调用
}

// ✅ 正确：完全异步
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
{
    await Task.Delay(5000, ct); // ✅ 异步延迟
    var response = await _httpClient.GetAsync("...", ct); // ✅ 异步HTTP调用
}
```

**3. 避免Handler中的循环依赖**

```csharp
// ❌ 错误：Handler A和Handler B互相发布事件导致循环
public class HandlerA : IIntegrationEventHandler<EventA>
{
    public async Task HandleAsync(EventA @event, CancellationToken ct)
    {
        await _eventBus.PublishAsync(new EventB()); // 触发HandlerB
    }
}

public class HandlerB : IIntegrationEventHandler<EventB>
{
    public async Task HandleAsync(EventB @event, CancellationToken ct)
    {
        await _eventBus.PublishAsync(new EventA()); // ❌ 循环触发HandlerA
    }
}

// ✅ 正确：避免循环依赖，使用事件标记或条件判断
public class HandlerA : IIntegrationEventHandler<EventA>
{
    public async Task HandleAsync(EventA @event, CancellationToken ct)
    {
        if (!@event.IsTriggeredByHandlerB)
        {
            await _eventBus.PublishAsync(new EventB { TriggeredByHandlerA = true });
        }
    }
}
```

### 10.4 监控和日志最佳实践

**1. 记录详细的事件发布日志**

```csharp
public async Task<UserDto> CreateAsync(CreateUserDto dto)
{
    var user = await _userRepository.AddAsync(userModel);

    var userCreatedEvent = new UserCreatedEvent(user.Id, user.UserName);

    // ✅ 发布前记录日志
    _logger.LogInformation("准备发布用户创建事件: EventId={EventId}, UserId={UserId}, UserName={UserName}",
        userCreatedEvent.Id, user.Id, user.UserName);

    try
    {
        await _eventBus.PublishAsync(userCreatedEvent);

        // ✅ 发布后记录成功日志
        _logger.LogInformation("用户创建事件发布成功: EventId={EventId}", userCreatedEvent.Id);
    }
    catch (Exception ex)
    {
        // ✅ 发布失败记录错误日志
        _logger.LogError(ex, "用户创建事件发布失败: EventId={EventId}, UserId={UserId}",
            userCreatedEvent.Id, user.Id);
    }

    return userDto;
}
```

**2. Handler记录处理开始和结束**

```csharp
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
{
    // ✅ 记录处理开始
    _logger.LogInformation("开始处理用户创建事件: EventId={EventId}, UserId={UserId}",
        @event.Id, @event.UserId);

    var stopwatch = Stopwatch.StartNew();

    try
    {
        await _patientRepository.AddAsync(patient);

        stopwatch.Stop();

        // ✅ 记录处理成功和耗时
        _logger.LogInformation("用户创建事件处理成功: EventId={EventId}, 耗时={ElapsedMs}ms",
            @event.Id, stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();

        // ✅ 记录处理失败和耗时
        _logger.LogError(ex, "用户创建事件处理失败: EventId={EventId}, 耗时={ElapsedMs}ms",
            @event.Id, stopwatch.ElapsedMilliseconds);
    }
}
```

**3. 定期输出EventBus统计信息**

```csharp
/// <summary>
/// EventBus健康检查
/// </summary>
public class EventBusHealthCheck : IHealthCheck
{
    private readonly IEventBus _eventBus;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = ((InMemoryEventBus)_eventBus).GetStatistics();

            var data = new Dictionary<string, object>
            {
                { "TotalPublished", stats.TotalPublishedEvents },
                { "TotalProcessed", stats.TotalProcessedEvents },
                { "FailedEvents", stats.FailedEvents },
                { "RegisteredEventTypes", stats.RegisteredEventTypes },
                { "RegisteredHandlers", stats.RegisteredHandlers },
                { "LastActivityTime", stats.LastActivityTime }
            };

            // 失败率超过5%则不健康
            var failureRate = stats.TotalPublishedEvents > 0
                ? (double)stats.FailedEvents / stats.TotalPublishedEvents
                : 0;

            if (failureRate > 0.05)
            {
                return HealthCheckResult.Degraded("事件失败率过高", data: data);
            }

            return HealthCheckResult.Healthy("事件总线运行正常", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("事件总线检查失败", ex);
        }
    }
}
```

---

## 11. 参考资料

### 11.1 项目文档

- **架构文档**：`docs/architecture/server/README.md` - Server端三层架构概览
- **模块文档**：`docs/modules/core/eventbus.md` - EventBus模块详细文档 *(待创建)*
- **API文档**：`docs/api/eventbus-api.md` - EventBus API参考 *(待创建)*

### 11.2 技术文档

- **IHostedService**：[Microsoft文档](https://docs.microsoft.com/en-us/dotnet/core/extensions/hosted-services)
- **ConcurrentDictionary**：[Microsoft文档](https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2)
- **异步编程最佳实践**：[Microsoft文档](https://docs.microsoft.com/en-us/dotnet/csharp/async)

### 11.3 Issue参考

- **Issue #1234**：EventBus基础实现 *(示例)*
- **Issue #1235**：模块通信标准化 *(示例)*

---

**最后更新**：2025-10-30
**维护负责**：Server端开发组
