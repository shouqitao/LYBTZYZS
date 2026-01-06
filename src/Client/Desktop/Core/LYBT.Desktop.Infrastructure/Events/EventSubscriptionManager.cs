using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 事件订阅管理器 - 自动管理Prism事件订阅的生命周期
    /// OpenSpec: standardize-viewmodel-framework
    /// 
    /// 用途:
    /// - 自动跟踪所有事件订阅
    /// - 在Dispose时自动取消所有订阅
    /// - 避免手动管理SubscriptionToken导致的内存泄漏
    /// 
    /// 使用方式:
    /// Events.Subscribe&lt;MyEvent, MyPayload&gt;(OnMyEventReceived);
    /// // Dispose时自动清理，无需手动Unsubscribe
    /// </summary>
    public class EventSubscriptionManager : IDisposable
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly List<SubscriptionToken> _tokens = new();
        private bool _disposed;

        /// <summary>
        /// 创建事件订阅管理器
        /// </summary>
        /// <param name="eventAggregator">Prism事件聚合器</param>
        public EventSubscriptionManager(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        /// <summary>
        /// 订阅事件 (UI线程)
        /// </summary>
        /// <typeparam name="TEvent">事件类型，必须继承PubSubEvent&lt;TPayload&gt;</typeparam>
        /// <typeparam name="TPayload">事件负载类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Subscribe<TEvent, TPayload>(Action<TPayload> handler)
            where TEvent : PubSubEvent<TPayload>, new()
        {
            ThrowIfDisposed();

            var token = _eventAggregator
                .GetEvent<TEvent>()
                .Subscribe(handler, ThreadOption.UIThread);
            _tokens.Add(token);
        }

        /// <summary>
        /// 订阅事件 (指定线程选项)
        /// </summary>
        /// <typeparam name="TEvent">事件类型，必须继承PubSubEvent&lt;TPayload&gt;</typeparam>
        /// <typeparam name="TPayload">事件负载类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="threadOption">线程选项</param>
        /// <param name="keepSubscriberReferenceAlive">是否保持订阅者引用</param>
        /// <param name="filter">可选的事件过滤器</param>
        public void Subscribe<TEvent, TPayload>(
            Action<TPayload> handler,
            ThreadOption threadOption,
            bool keepSubscriberReferenceAlive = false,
            Predicate<TPayload>? filter = null)
            where TEvent : PubSubEvent<TPayload>, new()
        {
            ThrowIfDisposed();

            var token = _eventAggregator
                .GetEvent<TEvent>()
                .Subscribe(handler, threadOption, keepSubscriberReferenceAlive, filter);
            _tokens.Add(token);
        }

        /// <summary>
        /// 订阅事件 (带过滤器，UI线程)
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <typeparam name="TPayload">事件负载类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="filter">事件过滤器</param>
        public void Subscribe<TEvent, TPayload>(
            Action<TPayload> handler,
            Predicate<TPayload> filter)
            where TEvent : PubSubEvent<TPayload>, new()
        {
            ThrowIfDisposed();

            var token = _eventAggregator
                .GetEvent<TEvent>()
                .Subscribe(handler, ThreadOption.UIThread, keepSubscriberReferenceAlive: false, filter);
            _tokens.Add(token);
        }

        /// <summary>
        /// 订阅无参数事件 (UI线程)
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Subscribe<TEvent>(Action handler)
            where TEvent : PubSubEvent, new()
        {
            ThrowIfDisposed();

            var token = _eventAggregator
                .GetEvent<TEvent>()
                .Subscribe(handler, ThreadOption.UIThread);
            _tokens.Add(token);
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <typeparam name="TPayload">事件负载类型</typeparam>
        /// <param name="payload">事件负载</param>
        public void Publish<TEvent, TPayload>(TPayload payload)
            where TEvent : PubSubEvent<TPayload>, new()
        {
            ThrowIfDisposed();
            _eventAggregator.GetEvent<TEvent>().Publish(payload);
        }

        /// <summary>
        /// 发布无参数事件
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        public void Publish<TEvent>()
            where TEvent : PubSubEvent, new()
        {
            ThrowIfDisposed();
            _eventAggregator.GetEvent<TEvent>().Publish();
        }

        /// <summary>
        /// 获取当前订阅数量
        /// </summary>
        public int SubscriptionCount => _tokens.Count;

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        public void ClearSubscriptions()
        {
            foreach (var token in _tokens)
            {
                token.Dispose();
            }
            _tokens.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EventSubscriptionManager));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            ClearSubscriptions();
            _disposed = true;
        }
    }
}
