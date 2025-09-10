# Issue #561 Technical Analysis: Reactive Session State Management Enhancement

## 概述
增强现有SessionManager的响应式能力，集成Prism EventAggregator实现实时状态同步，支持多窗口、多用户场景下的状态一致性管理。

## 现状分析

### 现有SessionManager限制
- **事件机制简单**: 仅支持基础PropertyChanged，缺乏复杂事件流管理
- **状态同步滞后**: 多窗口场景下状态更新不及时
- **缺乏状态历史**: 无法追踪状态变更历史和回滚
- **内存泄漏风险**: 事件订阅管理不当可能导致内存泄漏

### 目标架构
响应式状态管理系统：
```
SessionManager (Enhanced) -> EventAggregator -> ViewModels
     |                            |                |
     ├─ State History            ├─ Typed Events   ├─ Auto Subscription  
     ├─ Change Tracking          ├─ Event Filters   ├─ State Binding
     └─ Rollback Support         └─ Async Events   └─ UI Synchronization
```

## 4-Stream并行工作流设计

### Stream 1: 响应式SessionManager核心
**负责人**: 响应式编程专家
**文件范围**: `Core/Session/`
**工作内容**:
- 增强现有SessionManager为响应式版本
- 集成System.Reactive进行事件流管理
- 实现状态变更的连续追踪和流式处理
- 添加状态验证和约束检查机制

**响应式SessionManager设计**:
```csharp
public class ReactiveSessionManager : ISessionManager, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly Subject<SessionStateChange> _stateChangeSubject = new();
    private readonly BehaviorSubject<PatientDto> _currentPatientSubject = new(null);
    private readonly BehaviorSubject<UserDto> _currentUserSubject = new(null);
    private readonly CompositeDisposable _subscriptions = new();
    
    public IObservable<PatientDto> CurrentPatientObservable => _currentPatientSubject.AsObservable();
    public IObservable<UserDto> CurrentUserObservable => _currentUserSubject.AsObservable();
    public IObservable<SessionStateChange> StateChanges => _stateChangeSubject.AsObservable();
    
    public PatientDto CurrentPatient
    {
        get => _currentPatientSubject.Value;
        set
        {
            if (_currentPatientSubject.Value?.Id != value?.Id)
            {
                var oldPatient = _currentPatientSubject.Value;
                _currentPatientSubject.OnNext(value);
                
                // 发布强类型事件
                _eventAggregator.GetEvent<PatientSelectedEvent>()
                    .Publish(new PatientSelectedEventArgs
                    {
                        Patient = value,
                        PreviousPatient = oldPatient,
                        Timestamp = DateTime.UtcNow,
                        Source = "SessionManager"
                    });
                
                // 记录状态变更
                RecordStateChange(SessionStateType.CurrentPatient, oldPatient, value);
                OnPropertyChanged();
            }
        }
    }
    
    public void Initialize()
    {
        // 设置响应式管道
        SetupReactiveStreams();
        
        // 启动状态持久化
        SetupStatePersistence();
        
        // 配置错误处理
        SetupErrorHandling();
    }
    
    private void SetupReactiveStreams()
    {
        // 患者状态变更流
        _currentPatientSubject
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(100)) // 防抖
            .Subscribe(patient => 
            {
                // 触发相关业务逻辑
                if (patient != null)
                {
                    LoadPatientRelatedData(patient);
                }
            })
            .DisposeWith(_subscriptions);
            
        // 用户状态变更流
        _currentUserSubject
            .DistinctUntilChanged()
            .Subscribe(user =>
            {
                // 更新权限和界面状态
                UpdateUserPermissions(user);
            })
            .DisposeWith(_subscriptions);
    }
}
```

### Stream 2: EventAggregator类型安全集成
**负责人**: 事件系统专家  
**文件范围**: `Core/Events/`
**工作内容**:
- 创建强类型事件定义体系
- 实现EventAggregator的类型安全包装
- 添加事件优先级和过滤机制
- 提供事件调试和监控功能

**强类型事件系统**:
```csharp
// 强类型事件基类
public abstract class TypedPubSubEvent<T> : PubSubEvent<T>
{
    public string EventName => GetType().Name;
    public DateTime LastPublished { get; private set; }
    public int PublishCount { get; private set; }
    
    protected override void InternalPublish(params T[] arguments)
    {
        LastPublished = DateTime.UtcNow;
        PublishCount++;
        base.InternalPublish(arguments);
    }
}

// 会话相关事件定义
public class PatientSelectedEvent : TypedPubSubEvent<PatientSelectedEventArgs> { }
public class ConsultationStartedEvent : TypedPubSubEvent<ConsultationStartedEventArgs> { }
public class SessionTimeoutWarningEvent : TypedPubSubEvent<SessionTimeoutEventArgs> { }
public class UserPermissionsChangedEvent : TypedPubSubEvent<UserPermissionsEventArgs> { }

// 类型安全的EventAggregator包装
public class TypedEventAggregator : ITypedEventAggregator
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ConcurrentDictionary<Type, object> _eventCache = new();
    
    public TEvent GetEvent<TEvent>() where TEvent : class, new()
    {
        return (TEvent)_eventCache.GetOrAdd(typeof(TEvent), _ => new TEvent());
    }
    
    public void Publish<TEvent, TArgs>(TArgs args) 
        where TEvent : TypedPubSubEvent<TArgs>, new()
    {
        var eventInstance = GetEvent<TEvent>();
        eventInstance.Publish(args);
    }
    
    public SubscriptionToken Subscribe<TEvent, TArgs>(
        Action<TArgs> handler,
        ThreadOption threadOption = ThreadOption.PublisherThread,
        bool keepSubscriberReferenceAlive = false,
        Predicate<TArgs> filter = null)
        where TEvent : TypedPubSubEvent<TArgs>, new()
    {
        var eventInstance = GetEvent<TEvent>();
        return eventInstance.Subscribe(handler, threadOption, keepSubscriberReferenceAlive, filter);
    }
}
```

### Stream 3: 状态历史和回滚系统
**负责人**: 状态管理专家
**文件范围**: `Core/History/`
**工作内容**:
- 实现状态变更历史记录
- 创建状态快照和差异计算
- 提供状态回滚和前进功能
- 添加状态压缩和清理机制

**状态历史系统**:
```csharp
public class SessionStateHistory : ISessionStateHistory
{
    private readonly CircularBuffer<SessionStateSnapshot> _stateHistory;
    private readonly object _lock = new object();
    private int _currentIndex = -1;
    
    public SessionStateHistory(int maxHistorySize = 100)
    {
        _stateHistory = new CircularBuffer<SessionStateSnapshot>(maxHistorySize);
    }
    
    public void RecordStateChange<T>(string propertyName, T oldValue, T newValue)
    {
        lock (_lock)
        {
            var snapshot = new SessionStateSnapshot
            {
                Timestamp = DateTime.UtcNow,
                PropertyName = propertyName,
                OldValue = oldValue,
                NewValue = newValue,
                ChangeId = Guid.NewGuid()
            };
            
            // 如果当前不在历史末尾，清除后续历史
            if (_currentIndex < _stateHistory.Count - 1)
            {
                _stateHistory.RemoveRange(_currentIndex + 1);
            }
            
            _stateHistory.Add(snapshot);
            _currentIndex = _stateHistory.Count - 1;
        }
    }
    
    public bool CanUndo => _currentIndex > 0;
    public bool CanRedo => _currentIndex < _stateHistory.Count - 1;
    
    public SessionStateSnapshot Undo()
    {
        lock (_lock)
        {
            if (!CanUndo) return null;
            
            _currentIndex--;
            return _stateHistory[_currentIndex];
        }
    }
    
    public SessionStateSnapshot Redo()
    {
        lock (_lock)
        {
            if (!CanRedo) return null;
            
            _currentIndex++;
            return _stateHistory[_currentIndex];
        }
    }
    
    public IEnumerable<SessionStateSnapshot> GetRecentChanges(int count = 10)
    {
        lock (_lock)
        {
            return _stateHistory.TakeLast(count).ToList();
        }
    }
}

public class SessionStateSnapshot
{
    public DateTime Timestamp { get; set; }
    public string PropertyName { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public Guid ChangeId { get; set; }
    public string UserName { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### Stream 4: 多窗口状态同步
**负责人**: UI同步专家
**文件范围**: `UI/Synchronization/`
**工作内容**:
- 实现多窗口间状态实时同步
- 创建窗口状态协调机制
- 添加冲突检测和解决策略
- 提供窗口状态独立性配置

**多窗口同步系统**:
```csharp
public class MultiWindowSynchronizer : IMultiWindowSynchronizer
{
    private readonly ITypedEventAggregator _eventAggregator;
    private readonly ConcurrentDictionary<string, WindowStateManager> _windowManagers = new();
    
    public void RegisterWindow(string windowId, WindowStateManager stateManager)
    {
        _windowManagers[windowId] = stateManager;
        
        // 订阅窗口状态变更事件
        _eventAggregator.Subscribe<WindowStateChangedEvent, WindowStateChangedEventArgs>(
            args => SynchronizeWindowState(windowId, args),
            ThreadOption.UIThread);
    }
    
    public void UnregisterWindow(string windowId)
    {
        if (_windowManagers.TryRemove(windowId, out var manager))
        {
            manager.Dispose();
        }
    }
    
    private void SynchronizeWindowState(string sourceWindowId, WindowStateChangedEventArgs args)
    {
        // 向其他窗口广播状态变更
        foreach (var kvp in _windowManagers)
        {
            if (kvp.Key != sourceWindowId)
            {
                kvp.Value.ApplyExternalStateChange(args);
            }
        }
    }
}

public class WindowStateManager : IDisposable
{
    private readonly string _windowId;
    private readonly ReactiveSessionManager _sessionManager;
    private readonly CompositeDisposable _subscriptions = new();
    
    public WindowStateManager(string windowId, ReactiveSessionManager sessionManager)
    {
        _windowId = windowId;
        _sessionManager = sessionManager;
        
        SetupStateBindings();
    }
    
    private void SetupStateBindings()
    {
        // 绑定本地状态到全局会话状态
        _sessionManager.CurrentPatientObservable
            .Subscribe(patient => UpdateLocalPatientState(patient))
            .DisposeWith(_subscriptions);
            
        _sessionManager.CurrentUserObservable
            .Subscribe(user => UpdateLocalUserState(user))
            .DisposeWith(_subscriptions);
    }
    
    public void ApplyExternalStateChange(WindowStateChangedEventArgs args)
    {
        // 从其他窗口接收状态变更并应用到本窗口
        switch (args.StateType)
        {
            case WindowStateType.CurrentPatient:
                // 更新患者状态，但不触发事件（避免循环）
                UpdatePatientStateSilently((PatientDto)args.NewValue);
                break;
                
            case WindowStateType.ConsultationMode:
                // 更新诊疗模式
                UpdateConsultationModeSilently((ConsultationMode)args.NewValue);
                break;
        }
    }
}
```

## 技术实施细节

### 响应式扩展集成
```csharp
public static class ReactiveExtensions
{
    public static IObservable<T> FromSessionProperty<T>(
        this ISessionManager sessionManager,
        Expression<Func<ISessionManager, T>> propertySelector)
    {
        var propertyName = GetPropertyName(propertySelector);
        
        return Observable.Create<T>(observer =>
        {
            // 立即发出当前值
            var currentValue = propertySelector.Compile()(sessionManager);
            observer.OnNext(currentValue);
            
            // 订阅属性变更
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == propertyName)
                {
                    var newValue = propertySelector.Compile()(sessionManager);
                    observer.OnNext(newValue);
                }
            };
            
            sessionManager.PropertyChanged += handler;
            
            return Disposable.Create(() => sessionManager.PropertyChanged -= handler);
        });
    }
    
    public static IObservable<T> ThrottleDistinct<T>(
        this IObservable<T> source,
        TimeSpan throttle)
    {
        return source
            .Throttle(throttle)
            .DistinctUntilChanged();
    }
}
```

### 内存泄漏防护
```csharp
public class WeakEventSubscription : IDisposable
{
    private readonly WeakReference _targetRef;
    private readonly WeakReference _eventAggregatorRef;
    private readonly string _eventTypeName;
    private SubscriptionToken _token;
    
    public WeakEventSubscription(
        object target, 
        IEventAggregator eventAggregator, 
        Type eventType, 
        SubscriptionToken token)
    {
        _targetRef = new WeakReference(target);
        _eventAggregatorRef = new WeakReference(eventAggregator);
        _eventTypeName = eventType.Name;
        _token = token;
    }
    
    public void Dispose()
    {
        if (_token != null && _eventAggregatorRef.IsAlive)
        {
            // 清理订阅
            _token.Dispose();
            _token = null;
        }
    }
    
    ~WeakEventSubscription()
    {
        Dispose();
    }
}
```

## 风险评估与缓解

### 高风险项
1. **内存泄漏**: 响应式订阅管理不当可能导致内存泄漏
   - **缓解**: 实施WeakReference模式和自动清理机制

2. **性能影响**: 大量响应式流可能影响UI性能
   - **缓解**: 合理使用Throttle、Debounce和后台线程处理

### 中风险项
1. **状态一致性**: 多窗口环境下状态同步复杂性
   - **缓解**: 实施事务性状态变更和冲突检测

2. **调试困难**: 响应式编程的调试复杂性
   - **缓解**: 提供丰富的日志和调试工具

## 验收标准

### 功能完成度
- [ ] 响应式SessionManager完全实现
- [ ] 强类型事件系统集成完成
- [ ] 状态历史和回滚功能正常
- [ ] 多窗口状态同步无延迟

### 性能指标
- [ ] 状态变更响应时间 < 50ms
- [ ] 内存占用增加 < 20MB
- [ ] 支持10个以上并发窗口

### 质量标准
- [ ] 无内存泄漏
- [ ] 所有4个Stream完美集成
- [ ] 向后兼容性100%保持

## 预估工期
- **总工期**: 3.5周
- **并行开发**: 4个Stream同时进行，2.5周主要开发
- **集成测试**: 0.5周
- **性能优化**: 0.5周

## 依赖项目
- System.Reactive
- Prism.Core EventAggregator
- 现有SessionManager基础架构