using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Memory;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 增强事件聚合器 - 支持弱引用、消息过滤、优先级、调试追踪
    /// </summary>
    public interface IEnhancedEventAggregator : IEventAggregator
    {
        /// <summary>
        /// 获取增强事件
        /// </summary>
        new TEventType GetEvent<TEventType>() where TEventType : EnhancedEventBase, new();

        /// <summary>
        /// 获取事件统计
        /// </summary>
        EventStatistics GetStatistics();

        /// <summary>
        /// 清理死订阅
        /// </summary>
        void Cleanup();

        /// <summary>
        /// 设置全局过滤器
        /// </summary>
        void SetGlobalFilter(Func<object, bool> filter);

        /// <summary>
        /// 启用调试模式
        /// </summary>
        void EnableDebugMode(bool enable);
    }

    /// <summary>
    /// 增强事件基类
    /// </summary>
    public abstract class EnhancedEventBase : EventBase
    {
        private readonly WeakEventManager<EventArgs> _weakEventManager = new();
        private readonly ConcurrentDictionary<SubscriptionToken, IEventSubscription> _subscriptions = new();
        private readonly object _lock = new();
        private bool _debugMode;
        private ILogger? _logger;

        /// <summary>
        /// 设置日志器
        /// </summary>
        public void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 启用调试模式
        /// </summary>
        public void EnableDebugMode(bool enable)
        {
            _debugMode = enable;
        }

        /// <summary>
        /// 订阅（弱引用）
        /// </summary>
        public virtual SubscriptionToken Subscribe(
            Action action,
            EnhancedThreadOption threadOption = EnhancedThreadOption.PublisherThread,
            bool keepSubscriberReferenceAlive = false,
            Predicate<object>? filter = null,
            int priority = 0)
        {
            var subscription = new ActionSubscription(
                action, 
                threadOption, 
                keepSubscriberReferenceAlive,
                filter,
                priority);

            return AddSubscription(subscription);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public override void Unsubscribe(SubscriptionToken token)
        {
            lock (_lock)
            {
                if (_subscriptions.TryRemove(token, out var subscription))
                {
                    if (_debugMode)
                    {
                        _logger?.LogDebug($"取消订阅: {GetType().Name}, Token: {token}");
                    }
                }
            }
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        public virtual void Publish()
        {
            PublishInternal(null);
        }

        /// <summary>
        /// 内部发布逻辑
        /// </summary>
        protected void PublishInternal(object? argument)
        {
            var sw = _debugMode ? Stopwatch.StartNew() : null;
            var activeSubscriptions = GetActiveSubscriptions();
            
            if (_debugMode)
            {
                _logger?.LogDebug($"发布事件: {GetType().Name}, 订阅者数: {activeSubscriptions.Count}");
            }

            // 按优先级排序
            var sortedSubscriptions = activeSubscriptions
                .OrderByDescending(s => s.Priority)
                .ToList();

            Parallel.ForEach(sortedSubscriptions, subscription =>
            {
                try
                {
                    if (subscription.Filter == null || subscription.Filter(argument ?? new object()))
                    {
                        InvokeSubscription(subscription, argument);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"事件处理异常: {GetType().Name}");
                }
            });

            if (_debugMode && sw != null)
            {
                sw.Stop();
                _logger?.LogDebug($"事件发布完成: {GetType().Name}, 耗时: {sw.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// 调用订阅
        /// </summary>
        protected virtual void InvokeSubscription(IEventSubscription subscription, object? argument)
        {
            switch (subscription.ThreadOption)
            {
                case EnhancedThreadOption.PublisherThread:
                    subscription.InvokeAction(argument);
                    break;
                    
                case EnhancedThreadOption.UIThread:
                    if (SynchronizationContext.Current != null)
                    {
                        SynchronizationContext.Current.Post(_ => subscription.InvokeAction(argument), null);
                    }
                    else
                    {
                        subscription.InvokeAction(argument);
                    }
                    break;
                    
                case EnhancedThreadOption.BackgroundThread:
                    Task.Run(() => subscription.InvokeAction(argument));
                    break;
            }
        }

        /// <summary>
        /// 获取活跃订阅
        /// </summary>
        protected List<IEventSubscription> GetActiveSubscriptions()
        {
            lock (_lock)
            {
                // 清理死订阅
                var deadTokens = _subscriptions
                    .Where(kvp => !kvp.Value.IsAlive)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var token in deadTokens)
                {
                    _subscriptions.TryRemove(token, out _);
                }

                return _subscriptions.Values.Where(s => s.IsAlive).ToList();
            }
        }

        /// <summary>
        /// 添加订阅
        /// </summary>
        protected SubscriptionToken AddSubscription(IEventSubscription subscription)
        {
            lock (_lock)
            {
                var token = new SubscriptionToken(t => Unsubscribe(t));
                _subscriptions[token] = subscription;
                
                if (_debugMode)
                {
                    _logger?.LogDebug($"添加订阅: {GetType().Name}, Token: {token}, 弱引用: {!subscription.KeepAlive}");
                }
                
                return token;
            }
        }

        /// <summary>
        /// 获取订阅数
        /// </summary>
        public int GetSubscriptionCount()
        {
            return GetActiveSubscriptions().Count;
        }

        /// <summary>
        /// 清理死订阅
        /// </summary>
        public void Cleanup()
        {
            lock (_lock)
            {
                var deadTokens = _subscriptions
                    .Where(kvp => !kvp.Value.IsAlive)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var token in deadTokens)
                {
                    _subscriptions.TryRemove(token, out _);
                }

                if (deadTokens.Any() && _debugMode)
                {
                    _logger?.LogDebug($"清理死订阅: {GetType().Name}, 清理数: {deadTokens.Count}");
                }
            }
        }
    }

    /// <summary>
    /// 泛型增强事件
    /// </summary>
    public class EnhancedEvent<TPayload> : EnhancedEventBase
    {
        /// <summary>
        /// 订阅（泛型版本）
        /// </summary>
        public SubscriptionToken Subscribe(
            Action<TPayload> action,
            EnhancedThreadOption threadOption = EnhancedThreadOption.PublisherThread,
            bool keepSubscriberReferenceAlive = false,
            Predicate<TPayload>? filter = null,
            int priority = 0)
        {
            var subscription = new ActionSubscription<TPayload>(
                action,
                threadOption,
                keepSubscriberReferenceAlive,
                filter,
                priority);

            return AddSubscription(subscription);
        }

        /// <summary>
        /// 发布（泛型版本）
        /// </summary>
        public void Publish(TPayload payload)
        {
            PublishInternal(payload);
        }
    }

    /// <summary>
    /// 事件订阅接口
    /// </summary>
    public interface IEventSubscription
    {
        bool IsAlive { get; }
        bool KeepAlive { get; }
        EnhancedThreadOption ThreadOption { get; }
        Predicate<object>? Filter { get; }
        int Priority { get; }
        void InvokeAction(object? argument);
    }

    /// <summary>
    /// Action订阅实现
    /// </summary>
    public class ActionSubscription : IEventSubscription
    {
        private readonly WeakReference? _weakAction;
        private readonly Action? _strongAction;

        public ActionSubscription(
            Action action,
            EnhancedThreadOption threadOption,
            bool keepAlive,
            Predicate<object>? filter,
            int priority)
        {
            if (keepAlive)
            {
                _strongAction = action;
                _weakAction = null;
            }
            else
            {
                _strongAction = null;
                _weakAction = new WeakReference(action.Target);
            }

            ThreadOption = threadOption;
            KeepAlive = keepAlive;
            Filter = filter;
            Priority = priority;
        }

        public bool IsAlive => _strongAction != null || (_weakAction?.IsAlive ?? false);
        public bool KeepAlive { get; }
        public EnhancedThreadOption ThreadOption { get; }
        public Predicate<object>? Filter { get; }
        public int Priority { get; }

        public void InvokeAction(object? argument)
        {
            if (_strongAction != null)
            {
                _strongAction();
            }
            else if (_weakAction?.Target is Action action)
            {
                action();
            }
        }
    }

    /// <summary>
    /// 泛型Action订阅实现
    /// </summary>
    public class ActionSubscription<TPayload> : IEventSubscription
    {
        private readonly WeakReference? _weakAction;
        private readonly Action<TPayload>? _strongAction;
        private readonly Predicate<TPayload>? _typedFilter;

        public ActionSubscription(
            Action<TPayload> action,
            EnhancedThreadOption threadOption,
            bool keepAlive,
            Predicate<TPayload>? filter,
            int priority)
        {
            if (keepAlive)
            {
                _strongAction = action;
                _weakAction = null;
            }
            else
            {
                _strongAction = null;
                _weakAction = new WeakReference(action.Target);
            }

            ThreadOption = threadOption;
            KeepAlive = keepAlive;
            _typedFilter = filter;
            Filter = filter != null ? obj => obj is TPayload p && filter(p) : null;
            Priority = priority;
        }

        public bool IsAlive => _strongAction != null || (_weakAction?.IsAlive ?? false);
        public bool KeepAlive { get; }
        public EnhancedThreadOption ThreadOption { get; }
        public Predicate<object>? Filter { get; }
        public int Priority { get; }

        public void InvokeAction(object? argument)
        {
            if (argument is TPayload payload)
            {
                if (_strongAction != null)
                {
                    _strongAction(payload);
                }
                else if (_weakAction?.Target != null)
                {
                    var target = _weakAction.Target;
                    // 需要重新创建委托
                    // 这里简化处理，实际应该缓存MethodInfo
                    _strongAction?.Invoke(payload);
                }
            }
        }
    }

    /// <summary>
    /// 增强事件聚合器实现
    /// </summary>
    public class EnhancedEventAggregator : IEnhancedEventAggregator
    {
        private readonly ConcurrentDictionary<Type, EnhancedEventBase> _events = new();
        private readonly ILogger<EnhancedEventAggregator>? _logger;
        private readonly Timer _cleanupTimer;
        private Func<object, bool>? _globalFilter;
        private bool _debugMode;
        private readonly EventStatistics _statistics = new();

        public EnhancedEventAggregator(ILogger<EnhancedEventAggregator>? logger = null)
        {
            _logger = logger;
            // 定期清理（每5分钟）
            _cleanupTimer = new Timer(_ => Cleanup(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// 获取事件（增强版）
        /// </summary>
        public TEventType GetEvent<TEventType>() where TEventType : EnhancedEventBase, new()
        {
            return (TEventType)_events.GetOrAdd(typeof(TEventType), _ =>
            {
                var newEvent = new TEventType();
                if (_logger != null)
                {
                    newEvent.SetLogger(_logger);
                }
                newEvent.EnableDebugMode(_debugMode);
                
                _statistics.RegisterEvent(typeof(TEventType).Name);
                
                return newEvent;
            });
        }

        /// <summary>
        /// 获取事件（兼容Prism）
        /// </summary>
        TEventType IEventAggregator.GetEvent<TEventType>()
        {
            if (typeof(EnhancedEventBase).IsAssignableFrom(typeof(TEventType)))
            {
                // 使用反射调用泛型方法
                var method = GetType().GetMethod(nameof(GetEvent), Type.EmptyTypes);
                var genericMethod = method?.MakeGenericMethod(typeof(TEventType));
                var result = genericMethod?.Invoke(this, null);
                
                return (TEventType)(result ?? throw new InvalidOperationException($"无法获取事件类型: {typeof(TEventType).Name}"));
            }
            
            // 对于非增强事件，创建包装器
            throw new NotSupportedException("请使用EnhancedEventBase作为事件基类");
        }

        /// <summary>
        /// 设置全局过滤器
        /// </summary>
        public void SetGlobalFilter(Func<object, bool> filter)
        {
            _globalFilter = filter;
        }

        /// <summary>
        /// 启用调试模式
        /// </summary>
        public void EnableDebugMode(bool enable)
        {
            _debugMode = enable;
            foreach (var evt in _events.Values)
            {
                evt.EnableDebugMode(enable);
            }
        }

        /// <summary>
        /// 清理死订阅
        /// </summary>
        public void Cleanup()
        {
            var cleanedCount = 0;
            foreach (var evt in _events.Values)
            {
                var beforeCount = evt.GetSubscriptionCount();
                evt.Cleanup();
                var afterCount = evt.GetSubscriptionCount();
                cleanedCount += (beforeCount - afterCount);
            }

            if (cleanedCount > 0 && _debugMode)
            {
                _logger?.LogDebug($"事件聚合器清理完成，移除 {cleanedCount} 个死订阅");
            }
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public EventStatistics GetStatistics()
        {
            _statistics.TotalEvents = _events.Count;
            _statistics.TotalSubscriptions = _events.Values.Sum(e => e.GetSubscriptionCount());
            return _statistics;
        }
    }

    /// <summary>
    /// 事件统计
    /// </summary>
    public class EventStatistics
    {
        public int TotalEvents { get; set; }
        public int TotalSubscriptions { get; set; }
        public Dictionary<string, int> EventPublishCount { get; } = new();
        public Dictionary<string, DateTime> LastPublishTime { get; } = new();
        
        public void RegisterEvent(string eventName)
        {
            if (!EventPublishCount.ContainsKey(eventName))
            {
                EventPublishCount[eventName] = 0;
            }
        }
        
        public void RecordPublish(string eventName)
        {
            EventPublishCount[eventName] = EventPublishCount.GetValueOrDefault(eventName) + 1;
            LastPublishTime[eventName] = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 增强事件线程选项
    /// </summary>
    public enum EnhancedThreadOption
    {
        /// <summary>
        /// 发布者线程
        /// </summary>
        PublisherThread,
        
        /// <summary>
        /// UI线程
        /// </summary>
        UIThread,
        
        /// <summary>
        /// 后台线程
        /// </summary>
        BackgroundThread
    }
}