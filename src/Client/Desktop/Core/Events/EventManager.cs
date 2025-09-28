using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 统一事件管理器
    /// 提供事件的集中管理、发布、订阅和监控功能
    /// </summary>
    public class EventManager : IEventManager, IDisposable
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<EventManager> _logger;
        private readonly Dictionary<Type, object> _eventSubjects;
        private readonly Dictionary<string, SubscriptionToken> _subscriptions;
        private readonly object _lock = new object();
        private bool _disposed;

        public EventManager(IEventAggregator eventAggregator, ILogger<EventManager> logger)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventSubjects = new Dictionary<Type, object>();
            _subscriptions = new Dictionary<string, SubscriptionToken>();
        }

        #region 发布事件

        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish<TEvent, TPayload>(TPayload payload)
            where TEvent : PubSubEvent<TPayload>, new()
        {
            try
            {
                _eventAggregator.GetEvent<TEvent>().Publish(payload);
                _logger.LogDebug("事件已发布 - 类型: {EventType}, 时间: {Time}",
                    typeof(TEvent).Name, DateTime.Now);

                // 通知监控
                NotifyEventPublished(typeof(TEvent), payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布事件失败 - 类型: {EventType}", typeof(TEvent).Name);
                throw;
            }
        }

        /// <summary>
        /// 异步发布事件
        /// </summary>
        public Task PublishAsync<TEvent, TPayload>(TPayload payload)
            where TEvent : PubSubEvent<TPayload>, new()
        {
            return Task.Run(() => Publish<TEvent, TPayload>(payload));
        }

        /// <summary>
        /// 批量发布事件
        /// </summary>
        public async Task PublishBatchAsync<TEvent, TPayload>(IEnumerable<TPayload> payloads)
            where TEvent : PubSubEvent<TPayload>, new()
        {
            foreach (var payload in payloads)
            {
                await PublishAsync<TEvent, TPayload>(payload);
                await Task.Delay(10); // 防止事件风暴
            }
        }

        #endregion

        #region 订阅事件

        /// <summary>
        /// 订阅事件
        /// </summary>
        public string Subscribe<TEvent, TPayload>(Action<TPayload> action,
            ThreadOption threadOption = ThreadOption.UIThread,
            bool keepSubscriberReferenceAlive = false,
            Predicate<TPayload>? filter = null)
            where TEvent : PubSubEvent<TPayload>, new()
        {
            try
            {
                var token = _eventAggregator.GetEvent<TEvent>()
                    .Subscribe(action, threadOption, keepSubscriberReferenceAlive, filter);

                var subscriptionId = Guid.NewGuid().ToString();

                lock (_lock)
                {
                    _subscriptions[subscriptionId] = token;
                }

                _logger.LogDebug("事件已订阅 - 类型: {EventType}, ID: {SubscriptionId}",
                    typeof(TEvent).Name, subscriptionId);

                return subscriptionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅事件失败 - 类型: {EventType}", typeof(TEvent).Name);
                throw;
            }
        }

        /// <summary>
        /// 订阅事件（弱引用）
        /// </summary>
        public string SubscribeWeak<TEvent, TPayload>(Action<TPayload> action,
            Predicate<TPayload>? filter = null)
            where TEvent : PubSubEvent<TPayload>, new()
        {
            return Subscribe<TEvent, TPayload>(action, ThreadOption.UIThread, false, filter);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public bool Unsubscribe(string subscriptionId)
        {
            lock (_lock)
            {
                if (_subscriptions.TryGetValue(subscriptionId, out var token))
                {
                    token.Dispose();
                    _subscriptions.Remove(subscriptionId);
                    _logger.LogDebug("事件订阅已取消 - ID: {SubscriptionId}", subscriptionId);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 事件监控

        /// <summary>
        /// 获取事件的Observable流
        /// </summary>
        public IObservable<TPayload> GetObservable<TEvent, TPayload>()
            where TEvent : PubSubEvent<TPayload>, new()
        {
            var key = typeof(TEvent);

            lock (_lock)
            {
                if (!_eventSubjects.ContainsKey(key))
                {
                    var subject = new Subject<TPayload>();
                    _eventSubjects[key] = subject;

                    // 订阅Prism事件并转发到Subject
                    _eventAggregator.GetEvent<TEvent>().Subscribe(payload =>
                    {
                        subject.OnNext(payload);
                    });
                }

                return (IObservable<TPayload>)_eventSubjects[key];
            }
        }

        /// <summary>
        /// 通知事件已发布
        /// </summary>
        private void NotifyEventPublished(Type eventType, object payload)
        {
            EventPublished?.Invoke(this, new EventPublishedEventArgs
            {
                EventType = eventType,
                Payload = payload,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 事件发布事件
        /// </summary>
        public event EventHandler<EventPublishedEventArgs> EventPublished;

        #endregion

        #region 事件统计

        private readonly Dictionary<Type, ManagerEventStatistics> _statistics = new();

        /// <summary>
        /// 获取事件统计信息
        /// </summary>
        public ManagerEventStatistics GetStatistics<TEvent>()
        {
            var type = typeof(TEvent);

            lock (_lock)
            {
                if (!_statistics.ContainsKey(type))
                {
                    _statistics[type] = new ManagerEventStatistics { EventTypeName = type.Name };
                }

                return (ManagerEventStatistics)_statistics[type];
            }
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            lock (_lock)
            {
                _statistics.Clear();
            }
        }

        #endregion

        #region 清理

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 取消所有订阅
                lock (_lock)
                {
                    foreach (var token in _subscriptions.Values)
                    {
                        token?.Dispose();
                    }
                    _subscriptions.Clear();

                    // 清理Subjects
                    foreach (var subject in _eventSubjects.Values)
                    {
                        if (subject is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                    _eventSubjects.Clear();
                }
            }

            _disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// 事件管理器接口
    /// </summary>
    public interface IEventManager
    {
        void Publish<TEvent, TPayload>(TPayload payload) where TEvent : PubSubEvent<TPayload>, new();
        Task PublishAsync<TEvent, TPayload>(TPayload payload) where TEvent : PubSubEvent<TPayload>, new();
        Task PublishBatchAsync<TEvent, TPayload>(IEnumerable<TPayload> payloads) where TEvent : PubSubEvent<TPayload>, new();

        string Subscribe<TEvent, TPayload>(Action<TPayload> action,
            ThreadOption threadOption = ThreadOption.UIThread,
            bool keepSubscriberReferenceAlive = false,
            Predicate<TPayload>? filter = null) where TEvent : PubSubEvent<TPayload>, new();

        string SubscribeWeak<TEvent, TPayload>(Action<TPayload> action,
            Predicate<TPayload>? filter = null) where TEvent : PubSubEvent<TPayload>, new();

        bool Unsubscribe(string subscriptionId);

        IObservable<TPayload> GetObservable<TEvent, TPayload>() where TEvent : PubSubEvent<TPayload>, new();

        ManagerEventStatistics GetStatistics<TEvent>();
        void ResetStatistics();

        event EventHandler<EventPublishedEventArgs> EventPublished;
    }

    /// <summary>
    /// 事件发布参数
    /// </summary>
    public class EventPublishedEventArgs : EventArgs
    {
        public Type EventType { get; set; }
        public object Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 管理器事件统计信息
    /// </summary>
    public class ManagerEventStatistics
    {
        public string EventTypeName { get; set; }
        public int PublishCount { get; set; }
        public int SubscriberCount { get; set; }
        public DateTime LastPublished { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
    }


}