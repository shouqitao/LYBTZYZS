using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace LYBT.Desktop.Core.Memory
{
    /// <summary>
    /// 弱事件管理器 - 防止事件订阅导致的内存泄漏
    /// </summary>
    public class WeakEventManager<TEventArgs> where TEventArgs : EventArgs
    {
        private readonly List<WeakSubscription> _subscriptions = new();
        private readonly object _lock = new();
        private readonly ConditionalWeakTable<object, object> _strongReferences = new();
        private DateTime _lastCleanup = DateTime.UtcNow;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 订阅事件（弱引用）
        /// </summary>
        public void Subscribe(EventHandler<TEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                CleanupIfNeeded();
                _subscriptions.Add(new WeakSubscription(handler));
            }
        }

        /// <summary>
        /// 订阅事件（强引用，需要手动取消订阅）
        /// </summary>
        public IDisposable SubscribeStrong(EventHandler<TEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var subscription = new StrongSubscription(handler, () => Unsubscribe(handler));
            
            lock (_lock)
            {
                _strongReferences.Add(subscription, handler);
            }
            
            Subscribe(handler);
            return subscription;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void Unsubscribe(EventHandler<TEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                _subscriptions.RemoveAll(s => s.IsMatch(handler));
            }
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public void Raise(object sender, TEventArgs args)
        {
            List<EventHandler<TEventArgs>> handlers;
            
            lock (_lock)
            {
                CleanupIfNeeded();
                handlers = _subscriptions
                    .Where(s => s.IsAlive)
                    .Select(s => s.Handler)
                    .Where(h => h != null)
                    .ToList()!;
            }

            // 在锁外触发事件，避免死锁
            foreach (var handler in handlers)
            {
                try
                {
                    // 在UI线程触发
                    if (handler.Target is DispatcherObject dispatcherObject)
                    {
                        dispatcherObject.Dispatcher.BeginInvoke(handler, sender, args);
                    }
                    else
                    {
                        handler(sender, args);
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但不中断其他处理器
                    System.Diagnostics.Debug.WriteLine($"WeakEventManager: 事件处理器异常 - {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清理已释放的订阅
        /// </summary>
        private void CleanupIfNeeded()
        {
            var now = DateTime.UtcNow;
            if (now - _lastCleanup > _cleanupInterval)
            {
                _subscriptions.RemoveAll(s => !s.IsAlive);
                _lastCleanup = now;
            }
        }

        /// <summary>
        /// 获取当前活跃订阅数
        /// </summary>
        public int GetActiveSubscriptionCount()
        {
            lock (_lock)
            {
                return _subscriptions.Count(s => s.IsAlive);
            }
        }

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _subscriptions.Clear();
                _strongReferences.Clear();
            }
        }

        /// <summary>
        /// 弱订阅包装器
        /// </summary>
        private class WeakSubscription
        {
            private readonly WeakReference _methodTarget;
            private readonly System.Reflection.MethodInfo _methodInfo;

            public WeakSubscription(EventHandler<TEventArgs> handler)
            {
                _methodTarget = handler.Target != null 
                    ? new WeakReference(handler.Target) 
                    : null!;
                _methodInfo = handler.Method;
            }

            public bool IsAlive => _methodTarget?.IsAlive ?? true;

            public EventHandler<TEventArgs>? Handler
            {
                get
                {
                    if (_methodTarget == null)
                    {
                        // 静态方法
                        return (EventHandler<TEventArgs>)Delegate.CreateDelegate(
                            typeof(EventHandler<TEventArgs>), 
                            null, 
                            _methodInfo);
                    }

                    var target = _methodTarget.Target;
                    if (target == null)
                        return null;

                    return (EventHandler<TEventArgs>)Delegate.CreateDelegate(
                        typeof(EventHandler<TEventArgs>), 
                        target, 
                        _methodInfo);
                }
            }

            public bool IsMatch(EventHandler<TEventArgs> handler)
            {
                if (_methodInfo != handler.Method)
                    return false;

                if (_methodTarget == null)
                    return handler.Target == null;

                return _methodTarget.Target == handler.Target;
            }
        }

        /// <summary>
        /// 强订阅包装器
        /// </summary>
        private class StrongSubscription : IDisposable
        {
            private readonly Action _unsubscribe;
            private bool _disposed;

            public StrongSubscription(EventHandler<TEventArgs> handler, Action unsubscribe)
            {
                Handler = handler;
                _unsubscribe = unsubscribe;
            }

            public EventHandler<TEventArgs> Handler { get; }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _unsubscribe();
                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// 泛型弱事件管理器（支持任意委托类型）
    /// </summary>
    public class GenericWeakEventManager<TDelegate> where TDelegate : Delegate
    {
        private readonly List<WeakDelegate> _delegates = new();
        private readonly object _lock = new();

        /// <summary>
        /// 添加弱引用委托
        /// </summary>
        public void AddHandler(TDelegate handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                Cleanup();
                _delegates.Add(new WeakDelegate(handler));
            }
        }

        /// <summary>
        /// 移除委托
        /// </summary>
        public void RemoveHandler(TDelegate handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                _delegates.RemoveAll(d => d.IsMatch(handler));
            }
        }

        /// <summary>
        /// 获取所有活跃的委托
        /// </summary>
        public IEnumerable<TDelegate> GetHandlers()
        {
            lock (_lock)
            {
                Cleanup();
                return _delegates
                    .Where(d => d.IsAlive)
                    .Select(d => d.GetDelegate())
                    .Where(d => d != null)
                    .Cast<TDelegate>()
                    .ToList();
            }
        }

        /// <summary>
        /// 清理已释放的委托
        /// </summary>
        private void Cleanup()
        {
            _delegates.RemoveAll(d => !d.IsAlive);
        }

        /// <summary>
        /// 弱委托包装器
        /// </summary>
        private class WeakDelegate
        {
            private readonly WeakReference? _weakTarget;
            private readonly System.Reflection.MethodInfo _method;
            private readonly Type _delegateType;

            public WeakDelegate(Delegate handler)
            {
                _weakTarget = handler.Target != null 
                    ? new WeakReference(handler.Target) 
                    : null;
                _method = handler.Method;
                _delegateType = handler.GetType();
            }

            public bool IsAlive => _weakTarget?.IsAlive ?? true;

            public Delegate? GetDelegate()
            {
                if (_weakTarget == null)
                {
                    // 静态方法
                    return Delegate.CreateDelegate(_delegateType, null, _method);
                }

                var target = _weakTarget.Target;
                return target != null 
                    ? Delegate.CreateDelegate(_delegateType, target, _method) 
                    : null;
            }

            public bool IsMatch(Delegate other)
            {
                if (_method != other.Method)
                    return false;

                if (_weakTarget == null)
                    return other.Target == null;

                return _weakTarget.Target == other.Target;
            }
        }
    }

    /// <summary>
    /// 弱事件扩展方法
    /// </summary>
    public static class WeakEventExtensions
    {
        /// <summary>
        /// 创建弱事件订阅
        /// </summary>
        public static IDisposable SubscribeWeak<TEventArgs>(
            this EventHandler<TEventArgs> handler,
            Action<EventHandler<TEventArgs>> subscribe,
            Action<EventHandler<TEventArgs>> unsubscribe) where TEventArgs : EventArgs
        {
            var weakHandler = new WeakEventHandler<TEventArgs>(handler);
            subscribe(weakHandler.Handler);
            
            return new DisposableAction(() => unsubscribe(weakHandler.Handler));
        }

        /// <summary>
        /// 一次性操作包装器
        /// </summary>
        private class DisposableAction : IDisposable
        {
            private Action? _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action?.Invoke();
                _action = null;
            }
        }

        /// <summary>
        /// 弱事件处理器
        /// </summary>
        private class WeakEventHandler<TEventArgs> where TEventArgs : EventArgs
        {
            private readonly WeakReference? _weakTarget;
            private readonly System.Reflection.MethodInfo _method;

            public WeakEventHandler(EventHandler<TEventArgs> handler)
            {
                _weakTarget = handler.Target != null 
                    ? new WeakReference(handler.Target) 
                    : null;
                _method = handler.Method;
            }

            public void Handler(object? sender, TEventArgs e)
            {
                var target = _weakTarget?.Target;
                if (_weakTarget == null || target != null)
                {
                    var handler = _weakTarget == null
                        ? (EventHandler<TEventArgs>)Delegate.CreateDelegate(
                            typeof(EventHandler<TEventArgs>), null, _method)
                        : (EventHandler<TEventArgs>)Delegate.CreateDelegate(
                            typeof(EventHandler<TEventArgs>), target, _method);
                    
                    handler(sender, e);
                }
            }
        }
    }
}