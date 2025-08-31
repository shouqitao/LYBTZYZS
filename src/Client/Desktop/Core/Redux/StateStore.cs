using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Redux
{
    /// <summary>
    /// Redux状态存储接口
    /// </summary>
    public interface IStateStore<TState> : IDisposable
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        TState State { get; }

        /// <summary>
        /// 分发Action
        /// </summary>
        void Dispatch(IAction action);

        /// <summary>
        /// 异步分发Action
        /// </summary>
        Task DispatchAsync(IAction action);

        /// <summary>
        /// 订阅状态变化
        /// </summary>
        IDisposable Subscribe(Action<TState> listener);

        /// <summary>
        /// 选择性订阅状态片段
        /// </summary>
        IDisposable Subscribe<TSlice>(Func<TState, TSlice> selector, Action<TSlice> listener);

        /// <summary>
        /// 获取状态历史
        /// </summary>
        IReadOnlyList<StateSnapshot<TState>> GetHistory();

        /// <summary>
        /// 时间旅行到指定状态
        /// </summary>
        void TimeTravelTo(int index);
    }

    /// <summary>
    /// 状态存储实现
    /// </summary>
    public class StateStore<TState> : IStateStore<TState> where TState : class, new()
    {
        private readonly IReducer<TState> _reducer;
        private readonly List<IMiddleware<TState>> _middlewares;
        private readonly ConcurrentBag<WeakReference<IStateSubscription>> _subscriptions;
        private readonly List<StateSnapshot<TState>> _history;
        private readonly ILogger<StateStore<TState>>? _logger;
        private readonly ReaderWriterLockSlim _stateLock;
        private readonly int _maxHistorySize;
        private TState _currentState;
        private int _dispatchDepth;

        public TState State 
        { 
            get 
            {
                _stateLock.EnterReadLock();
                try
                {
                    return _currentState;
                }
                finally
                {
                    _stateLock.ExitReadLock();
                }
            }
        }

        public StateStore(
            TState initialState,
            IReducer<TState> reducer,
            IEnumerable<IMiddleware<TState>>? middlewares = null,
            ILogger<StateStore<TState>>? logger = null,
            int maxHistorySize = 100)
        {
            _currentState = initialState ?? new TState();
            _reducer = reducer ?? throw new ArgumentNullException(nameof(reducer));
            _middlewares = middlewares?.ToList() ?? new List<IMiddleware<TState>>();
            _subscriptions = new ConcurrentBag<WeakReference<IStateSubscription>>();
            _history = new List<StateSnapshot<TState>>();
            _logger = logger;
            _stateLock = new ReaderWriterLockSlim();
            _maxHistorySize = maxHistorySize;

            // 初始化中间件
            InitializeMiddlewares();

            // 记录初始状态
            if (initialState != null)
            {
                RecordSnapshot(initialState, null);
            }
        }

        /// <summary>
        /// 分发Action
        /// </summary>
        public void Dispatch(IAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // 防止递归分发
            if (Interlocked.Increment(ref _dispatchDepth) > 10)
            {
                Interlocked.Decrement(ref _dispatchDepth);
                throw new InvalidOperationException("检测到递归分发，可能存在无限循环");
            }

            try
            {
                _logger?.LogDebug($"分发Action: {action.Type}");

                // 通过中间件管道
                var next = CreateDispatchChain();
                next(action);
            }
            finally
            {
                Interlocked.Decrement(ref _dispatchDepth);
            }
        }

        /// <summary>
        /// 异步分发Action
        /// </summary>
        public async Task DispatchAsync(IAction action)
        {
            await Task.Run(() => Dispatch(action));
        }

        /// <summary>
        /// 订阅状态变化
        /// </summary>
        public IDisposable Subscribe(Action<TState> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            var subscription = new StateSubscription<TState>(this, listener);
            _subscriptions.Add(new WeakReference<IStateSubscription>(subscription));
            
            // 立即触发当前状态
            listener(State);
            
            return subscription;
        }

        /// <summary>
        /// 选择性订阅
        /// </summary>
        public IDisposable Subscribe<TSlice>(Func<TState, TSlice> selector, Action<TSlice> listener)
        {
            if (selector == null || listener == null)
            {
                throw new ArgumentNullException();
            }

            var lastSlice = selector(State);
            return Subscribe(state =>
            {
                var newSlice = selector(state);
                if (!EqualityComparer<TSlice>.Default.Equals(lastSlice, newSlice))
                {
                    lastSlice = newSlice;
                    listener(newSlice);
                }
            });
        }

        /// <summary>
        /// 获取历史记录
        /// </summary>
        public IReadOnlyList<StateSnapshot<TState>> GetHistory()
        {
            lock (_history)
            {
                return _history.ToList();
            }
        }

        /// <summary>
        /// 时间旅行
        /// </summary>
        public void TimeTravelTo(int index)
        {
            lock (_history)
            {
                if (index < 0 || index >= _history.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                var snapshot = _history[index];
                _stateLock.EnterWriteLock();
                try
                {
                    _currentState = snapshot.State;
                    NotifySubscribers();
                    _logger?.LogInformation($"时间旅行到: {snapshot.Timestamp}");
                }
                finally
                {
                    _stateLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// 创建分发链
        /// </summary>
        private Action<IAction> CreateDispatchChain()
        {
            Action<IAction> dispatch = CoreDispatch;

            // 反向遍历中间件，构建管道
            for (int i = _middlewares.Count - 1; i >= 0; i--)
            {
                var middleware = _middlewares[i];
                var next = dispatch;
                dispatch = action => middleware.Process(this, action, next);
            }

            return dispatch;
        }

        /// <summary>
        /// 核心分发逻辑
        /// </summary>
        private void CoreDispatch(IAction action)
        {
            _stateLock.EnterWriteLock();
            try
            {
                var oldState = _currentState;
                var newState = _reducer.Reduce(oldState, action);

                if (!ReferenceEquals(oldState, newState))
                {
                    _currentState = newState;
                    RecordSnapshot(newState, action);
                    NotifySubscribers();
                    
                    _logger?.LogDebug($"状态已更新: {action.Type}");
                }
            }
            finally
            {
                _stateLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 记录快照
        /// </summary>
        private void RecordSnapshot(TState state, IAction? action)
        {
            lock (_history)
            {
                _history.Add(new StateSnapshot<TState>
                {
                    State = state,
                    Action = action,
                    Timestamp = DateTimeOffset.UtcNow
                });

                // 限制历史大小
                if (_history.Count > _maxHistorySize)
                {
                    _history.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 通知订阅者
        /// </summary>
        private void NotifySubscribers()
        {
            var currentState = _currentState;
            var deadSubscriptions = new List<WeakReference<IStateSubscription>>();

            foreach (var weakRef in _subscriptions)
            {
                if (weakRef.TryGetTarget(out var subscription))
                {
                    try
                    {
                        subscription.Notify(currentState);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "订阅者通知失败");
                    }
                }
                else
                {
                    deadSubscriptions.Add(weakRef);
                }
            }

            // 清理死引用
            foreach (var dead in deadSubscriptions)
            {
                _subscriptions.TryTake(out _);
            }
        }

        /// <summary>
        /// 初始化中间件
        /// </summary>
        private void InitializeMiddlewares()
        {
            foreach (var middleware in _middlewares)
            {
                middleware.Initialize(this);
            }
        }

        public void Dispose()
        {
            _stateLock?.Dispose();
            
            // 清理订阅
            foreach (var weakRef in _subscriptions)
            {
                if (weakRef.TryGetTarget(out var subscription))
                {
                    subscription.Dispose();
                }
            }
            _subscriptions.Clear();

            // 清理中间件
            foreach (var middleware in _middlewares)
            {
                (middleware as IDisposable)?.Dispose();
            }
        }
    }

    /// <summary>
    /// 状态快照
    /// </summary>
    public class StateSnapshot<TState>
    {
        public TState State { get; set; } = default!;
        public IAction? Action { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// 状态订阅接口
    /// </summary>
    internal interface IStateSubscription : IDisposable
    {
        void Notify(object state);
    }

    /// <summary>
    /// 状态订阅实现
    /// </summary>
    internal class StateSubscription<TState> : IStateSubscription where TState : class, new()
    {
        private readonly StateStore<TState> _store;
        private readonly Action<TState> _listener;
        private bool _disposed;

        public StateSubscription(StateStore<TState> store, Action<TState> listener)
        {
            _store = store;
            _listener = listener;
        }

        public void Notify(object state)
        {
            if (!_disposed && state is TState typedState)
            {
                _listener(typedState);
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}