# Issue #565 Technical Analysis: ViewModel Communication Standardization

## 概述
标准化ViewModel间的通信机制，建立基于Prism EventAggregator的类型安全通信体系，替代现有的松散耦合方案，提供强类型事件定义和自动订阅管理。

## 现状分析

### 当前通信问题
1. **事件定义分散**: 各模块独立定义事件，缺乏统一规范
2. **类型安全缺失**: 使用弱类型事件参数，运行时错误风险高
3. **订阅管理混乱**: 手动订阅/取消订阅，容易造成内存泄漏
4. **调试困难**: 事件流难以追踪和调试
5. **重复代码**: 相似的事件处理模式在各处重复

### 目标架构
```
Communication/
├── Events/                 # 强类型事件定义
│   ├── Core/              # 核心系统事件
│   ├── Business/          # 业务领域事件
│   └── UI/                # 界面交互事件
├── Handlers/              # 事件处理器基类
├── Attributes/            # 标注和配置
└── Extensions/            # EventAggregator扩展
```

## 并行工作流设计

### Stream 1: 强类型事件系统设计
**负责人**: 架构设计师
**工作内容**:
- 设计强类型事件基类和接口体系
- 定义事件生命周期管理标准
- 创建事件优先级和过滤机制
- 建立事件序列化和持久化规范

**交付物**:
```csharp
// 强类型事件基类
public abstract class DomainEvent<TPayload> : PubSubEvent<TPayload>
{
    public DateTime Timestamp { get; }
    public string EventId { get; }
    public int Priority { get; protected set; }
    public string Source { get; protected set; }
}

// 业务事件示例
public class PatientSelectedEvent : DomainEvent<PatientSelectedEventArgs>
{
    public PatientSelectedEvent() { Priority = EventPriority.High; }
}

public class ConsultationStartedEvent : DomainEvent<ConsultationStartedEventArgs>
{
    public ConsultationStartedEvent() { Priority = EventPriority.Critical; }
}
```

### Stream 2: 自动订阅管理系统
**负责人**: 框架专家
**工作内容**:
- 实现基于Attribute的自动订阅机制
- 创建ViewModel生命周期集成
- 开发订阅追踪和诊断工具
- 建立弱引用和内存泄漏防护

**交付物**:
```csharp
// 自动订阅属性标注
[EventSubscriber(typeof(PatientSelectedEvent), ThreadOption.UIThread)]
public class PatientDetailsViewModel : SessionAwareViewModel
{
    // 自动发现和注册事件处理方法
    private void Handle(PatientSelectedEventArgs args)
    {
        CurrentPatient = args.Patient;
        RefreshUI();
    }
}

// 订阅管理器
public class AutoSubscriptionManager
{
    public void RegisterViewModel(object viewModel);
    public void UnregisterViewModel(object viewModel);
    public SubscriptionDiagnostics GetDiagnostics();
}
```

### Stream 3: 核心业务事件重构
**负责人**: 业务逻辑专家
**工作内容**:
- 重构现有SessionManager事件为强类型版本
- 标准化患者、诊疗、用户状态变化事件
- 实现事件版本管理和兼容性处理
- 创建业务事件的聚合和派生机制

**交付物**:
```csharp
// 核心业务事件定义
namespace LYBT.Desktop.Core.Events.Business
{
    // 患者相关事件
    public class PatientSelectedEvent : DomainEvent<PatientSelectedEventArgs> { }
    public class PatientUpdatedEvent : DomainEvent<PatientUpdatedEventArgs> { }
    
    // 诊疗相关事件
    public class ConsultationStartedEvent : DomainEvent<ConsultationStartedEventArgs> { }
    public class ConsultationCompletedEvent : DomainEvent<ConsultationCompletedEventArgs> { }
    
    // 用户会话事件
    public class UserLoggedInEvent : DomainEvent<UserLoggedInEventArgs> { }
    public class UserLoggedOutEvent : DomainEvent<UserLoggedOutEventArgs> { }
    
    // 系统状态事件
    public class SystemStatusChangedEvent : DomainEvent<SystemStatusEventArgs> { }
    public class ErrorOccurredEvent : DomainEvent<ErrorEventArgs> { }
}
```

### Stream 4: UI交互事件标准化
**负责人**: UI专家
**工作内容**:
- 标准化窗口间导航和数据传递事件
- 重构模态对话框和消息通知事件
- 实现UI状态同步和刷新事件
- 创建键盘快捷键和手势事件系统

**交付物**:
```csharp
// UI交互事件
namespace LYBT.Desktop.Core.Events.UI
{
    // 导航事件
    public class NavigateToViewEvent : DomainEvent<NavigateToViewEventArgs> { }
    public class ViewActivatedEvent : DomainEvent<ViewActivatedEventArgs> { }
    
    // 数据操作事件
    public class DataRefreshRequestedEvent : DomainEvent<DataRefreshEventArgs> { }
    public class BulkOperationEvent : DomainEvent<BulkOperationEventArgs> { }
    
    // 用户交互事件
    public class KeyboardShortcutEvent : DomainEvent<KeyboardShortcutEventArgs> { }
    public class WindowStateChangedEvent : DomainEvent<WindowStateEventArgs> { }
}
```

### Stream 5: 调试和监控工具
**负责人**: 开发工具专家
**工作内容**:
- 创建事件流可视化和调试工具
- 实现性能监控和瓶颈识别
- 开发单元测试和集成测试框架
- 建立事件重放和时间旅行调试

**交付物**:
- EventAggregator调试面板
- 事件流性能分析工具
- Mock事件系统用于测试
- 事件序列记录和回放机制

## 技术实施细节

### 增强型EventAggregator
```csharp
public class EnhancedEventAggregator : IEnhancedEventAggregator
{
    private readonly IEventAggregator _baseAggregator;
    private readonly ILogger<EnhancedEventAggregator> _logger;
    private readonly ConcurrentDictionary<Type, List<WeakReference>> _subscribers;
    private readonly EventMetrics _metrics;

    // 强类型事件发布
    public void Publish<TEvent, TArgs>(TArgs args) 
        where TEvent : DomainEvent<TArgs>, new()
    {
        var eventInstance = new TEvent();
        eventInstance.Publish(args);
        
        _metrics.RecordEventPublished<TEvent>();
        _logger.LogDebug("Event published: {EventType}", typeof(TEvent).Name);
    }

    // 自动订阅管理
    public SubscriptionToken Subscribe<TEvent, TArgs>(
        object subscriber,
        Action<TArgs> handler,
        ThreadOption threadOption = ThreadOption.PublisherThread,
        bool keepSubscriberReferenceAlive = false)
        where TEvent : DomainEvent<TArgs>
    {
        // 实现弱引用订阅和自动清理
    }
}
```

### 事件参数强类型化
```csharp
// 强类型事件参数基类
public abstract class DomainEventArgs
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string EventId { get; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; set; }
    public Dictionary<string, object> Metadata { get; } = new();
}

// 具体事件参数
public class PatientSelectedEventArgs : DomainEventArgs
{
    public PatientDto Patient { get; set; }
    public PatientDto PreviousPatient { get; set; }
    public SelectionSource Source { get; set; }
}

public class ConsultationStartedEventArgs : DomainEventArgs
{
    public ConsultationDto Consultation { get; set; }
    public PatientDto Patient { get; set; }
    public UserDto Doctor { get; set; }
    public DateTime StartTime { get; set; }
}
```

### 自动订阅机制
```csharp
// 属性标注方式
[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute
{
    public Type EventType { get; }
    public ThreadOption ThreadOption { get; set; } = ThreadOption.UIThread;
    public bool KeepAlive { get; set; } = false;
    public int Priority { get; set; } = 0;

    public EventHandlerAttribute(Type eventType)
    {
        EventType = eventType;
    }
}

// 使用方式
public class ExampleViewModel : SessionAwareViewModel
{
    [EventHandler(typeof(PatientSelectedEvent), Priority = 1)]
    private void OnPatientSelected(PatientSelectedEventArgs args)
    {
        // 处理患者选择事件
    }

    [EventHandler(typeof(ConsultationStartedEvent), ThreadOption = ThreadOption.BackgroundThread)]
    private async void OnConsultationStarted(ConsultationStartedEventArgs args)
    {
        // 异步处理诊疗开始事件
    }
}
```

## 与现有系统集成

### SessionManager集成
```csharp
// 升级现有SessionManager事件发布
public class SessionManager : ISessionManager
{
    private readonly IEnhancedEventAggregator _eventAggregator;

    public PatientDto CurrentPatient
    {
        get => _currentPatient;
        set
        {
            var oldPatient = _currentPatient;
            if (SetProperty(ref _currentPatient, value))
            {
                // 发布强类型事件
                _eventAggregator.Publish<PatientSelectedEvent, PatientSelectedEventArgs>(
                    new PatientSelectedEventArgs
                    {
                        Patient = value,
                        PreviousPatient = oldPatient,
                        Source = SelectionSource.SessionManager
                    });
            }
        }
    }
}
```

### SessionAwareViewModel集成
```csharp
public abstract class SessionAwareViewModel : BindableBase, IAutoSubscriptionTarget
{
    private readonly AutoSubscriptionManager _subscriptionManager;

    protected SessionAwareViewModel(
        ISessionManager sessionManager,
        INotificationService notificationService,
        ILogger logger,
        IEnhancedEventAggregator eventAggregator)
    {
        _subscriptionManager = new AutoSubscriptionManager(eventAggregator);
        _subscriptionManager.RegisterViewModel(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscriptionManager?.UnregisterViewModel(this);
        }
        base.Dispose(disposing);
    }
}
```

## 风险评估与缓解

### 高风险项
1. **性能影响**: 大量事件订阅可能影响应用性能
   - **缓解**: 实施弱引用、延迟初始化和订阅池管理

2. **向后兼容性**: 现有事件系统的大规模重构风险
   - **缓解**: 分阶段迁移，保持双轨运行期间的兼容

### 中风险项
1. **内存泄漏**: 事件订阅管理不当导致的内存问题
   - **缓解**: 完善的弱引用机制和自动清理

2. **调试复杂性**: 强类型系统可能增加调试难度
   - **缓解**: 丰富的调试工具和日志机制

## 验收标准

### 功能完成度
- [ ] 所有核心业务事件强类型化完成
- [ ] 自动订阅管理系统正常工作
- [ ] 现有SessionManager事件完全迁移
- [ ] UI交互事件标准化实施
- [ ] 调试和监控工具可用

### 性能指标
- [ ] 事件发布延迟<5ms
- [ ] 订阅/取消订阅性能不下降
- [ ] 内存占用增加<10MB
- [ ] 应用启动时间影响<5%

### 质量标准
- [ ] 事件处理代码覆盖率≥85%
- [ ] 所有事件类型遵循命名规范
- [ ] 向后兼容性100%保持
- [ ] 无内存泄漏和性能退化

## 预估工期
- **总工期**: 3-4周
- **并行开发**: 2.5周
- **集成测试**: 0.5周
- **性能优化**: 1周

## 依赖项目
- Issue #561: Reactive Session State Management (事件系统基础)
- 现有SessionManager和EnhancedEventAggregator
- Prism.DryIoc EventAggregator基础设施