using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Redux
{
    /// <summary>
    /// Redux中间件接口
    /// </summary>
    public interface IMiddleware<TState>
    {
        /// <summary>
        /// 初始化中间件
        /// </summary>
        void Initialize(IStateStore<TState> store);

        /// <summary>
        /// 处理Action
        /// </summary>
        void Process(IStateStore<TState> store, IAction action, Action<IAction> next);
    }

    /// <summary>
    /// 日志中间件 - 记录所有Action和状态变化
    /// </summary>
    public class LoggingMiddleware<TState> : IMiddleware<TState>
    {
        private readonly ILogger<LoggingMiddleware<TState>>? _logger;
        private readonly bool _logPayload;
        private readonly bool _logState;

        public LoggingMiddleware(
            ILogger<LoggingMiddleware<TState>>? logger = null,
            bool logPayload = true,
            bool logState = false)
        {
            _logger = logger;
            _logPayload = logPayload;
            _logState = logState;
        }

        public void Initialize(IStateStore<TState> store)
        {
            _logger?.LogInformation("LoggingMiddleware初始化");
        }

        public void Process(IStateStore<TState> store, IAction action, Action<IAction> next)
        {
            var sw = Stopwatch.StartNew();
            var prevState = store.State;

            _logger?.LogDebug($"[Action] {action.Type} @ {action.Timestamp:HH:mm:ss.fff}");
            
            if (_logPayload && action is IAction<object> payloadAction)
            {
                var json = JsonSerializer.Serialize(payloadAction.Payload, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    MaxDepth = 3
                });
                _logger?.LogDebug($"[Payload] {json}");
            }

            next(action);

            sw.Stop();
            var nextState = store.State;

            if (_logState && !ReferenceEquals(prevState, nextState))
            {
                _logger?.LogDebug($"[State Changed] 耗时: {sw.ElapsedMilliseconds}ms");
            }
        }
    }

    /// <summary>
    /// 异步Action中间件 - 处理异步操作
    /// </summary>
    public class AsyncActionMiddleware<TState> : IMiddleware<TState>
    {
        private readonly Dictionary<string, Func<IStateStore<TState>, IAction, Task>> _asyncHandlers = new();
        private readonly ILogger<AsyncActionMiddleware<TState>>? _logger;

        public AsyncActionMiddleware(ILogger<AsyncActionMiddleware<TState>>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// 注册异步处理器
        /// </summary>
        public void RegisterHandler(string actionType, Func<IStateStore<TState>, IAction, Task> handler)
        {
            _asyncHandlers[actionType] = handler;
        }

        public void Initialize(IStateStore<TState> store)
        {
            _logger?.LogInformation($"AsyncActionMiddleware初始化，注册了 {_asyncHandlers.Count} 个处理器");
        }

        public void Process(IStateStore<TState> store, IAction action, Action<IAction> next)
        {
            // 检查是否有异步处理器
            if (_asyncHandlers.TryGetValue(action.Type, out var handler))
            {
                _logger?.LogDebug($"异步处理Action: {action.Type}");
                
                // 立即分发开始Action
                next(ActionCreator.CreateAsyncStart(action.Type));

                // 异步执行
                Task.Run(async () =>
                {
                    try
                    {
                        await handler(store, action);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"异步Action执行失败: {action.Type}");
                        store.Dispatch(ActionCreator.CreateAsyncError(action.Type, ex.Message));
                    }
                });
            }
            else
            {
                // 非异步Action，直接传递
                next(action);
            }
        }
    }

    /// <summary>
    /// DevTools中间件 - 支持Redux DevTools
    /// </summary>
    public class DevToolsMiddleware<TState> : IMiddleware<TState>
    {
        private readonly List<DevToolsEvent> _events = new();
        private readonly int _maxEvents;
        private IStateStore<TState>? _store;

        public IReadOnlyList<DevToolsEvent> Events => _events;

        public DevToolsMiddleware(int maxEvents = 1000)
        {
            _maxEvents = maxEvents;
        }

        public void Initialize(IStateStore<TState> store)
        {
            _store = store;
        }

        public void Process(IStateStore<TState> store, IAction action, Action<IAction> next)
        {
            var prevState = store.State;
            var startTime = DateTimeOffset.UtcNow;

            next(action);

            var nextState = store.State;
            var endTime = DateTimeOffset.UtcNow;

            // 记录事件
            var devEvent = new DevToolsEvent
            {
                Action = action,
                PrevState = prevState,
                NextState = nextState,
                Timestamp = startTime,
                Duration = endTime - startTime
            };

            lock (_events)
            {
                _events.Add(devEvent);
                
                // 限制事件数量
                if (_events.Count > _maxEvents)
                {
                    _events.RemoveAt(0);
                }
            }

            // 触发DevTools更新
            OnEventRecorded?.Invoke(devEvent);
        }

        /// <summary>
        /// 时间旅行
        /// </summary>
        public void TimeTravel(int eventIndex)
        {
            if (_store == null || eventIndex < 0 || eventIndex >= _events.Count)
            {
                return;
            }

            lock (_events)
            {
                // 重置到初始状态，然后重放Action到指定位置
                var eventsToReplay = _events.Take(eventIndex + 1).ToList();
                
                // 这里需要Store支持重置功能
                // 简化实现：直接跳转到指定事件的状态
                _store.TimeTravelTo(eventIndex);
            }
        }

        /// <summary>
        /// 导出事件日志
        /// </summary>
        public string ExportLog()
        {
            lock (_events)
            {
                return JsonSerializer.Serialize(_events, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
        }

        /// <summary>
        /// 事件记录通知
        /// </summary>
        public event Action<DevToolsEvent>? OnEventRecorded;

        public class DevToolsEvent
        {
            public IAction Action { get; set; } = null!;
            public object? PrevState { get; set; }
            public object? NextState { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public TimeSpan Duration { get; set; }
        }
    }

    /// <summary>
    /// 防抖中间件 - 防止频繁触发相同Action
    /// </summary>
    public class DebounceMiddleware<TState> : IMiddleware<TState>
    {
        private readonly Dictionary<string, DateTimeOffset> _lastActionTimes = new();
        private readonly Dictionary<string, TimeSpan> _debounceIntervals = new();
        private readonly TimeSpan _defaultInterval;

        public DebounceMiddleware(TimeSpan defaultInterval)
        {
            _defaultInterval = defaultInterval;
        }

        /// <summary>
        /// 配置特定Action的防抖间隔
        /// </summary>
        public void ConfigureDebounce(string actionType, TimeSpan interval)
        {
            _debounceIntervals[actionType] = interval;
        }

        public void Initialize(IStateStore<TState> store)
        {
        }

        public void Process(IStateStore<TState> store, IAction action, Action<IAction> next)
        {
            var interval = _debounceIntervals.TryGetValue(action.Type, out var customInterval) 
                ? customInterval 
                : _defaultInterval;

            lock (_lastActionTimes)
            {
                if (_lastActionTimes.TryGetValue(action.Type, out var lastTime))
                {
                    var elapsed = DateTimeOffset.UtcNow - lastTime;
                    if (elapsed < interval)
                    {
                        // 忽略过于频繁的Action
                        return;
                    }
                }

                _lastActionTimes[action.Type] = DateTimeOffset.UtcNow;
            }

            next(action);
        }
    }

    /// <summary>
    /// 验证中间件 - 验证Action和状态
    /// </summary>
    public class ValidationMiddleware<TState> : IMiddleware<TState>
    {
        private readonly List<IActionValidator> _validators = new();
        private readonly ILogger<ValidationMiddleware<TState>>? _logger;

        public ValidationMiddleware(ILogger<ValidationMiddleware<TState>>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// 添加验证器
        /// </summary>
        public void AddValidator(IActionValidator validator)
        {
            _validators.Add(validator);
        }

        public void Initialize(IStateStore<TState> store)
        {
            _logger?.LogInformation($"ValidationMiddleware初始化，{_validators.Count} 个验证器");
        }

        public void Process(IStateStore<TState> store, IAction action, Action<IAction> next)
        {
            // 验证Action
            foreach (var validator in _validators)
            {
                var result = validator.Validate(action);
                if (!result.IsValid)
                {
                    _logger?.LogWarning($"Action验证失败: {action.Type}, 错误: {result.Error}");
                    
                    // 分发验证错误Action
                    store.Dispatch(new ValidationErrorAction(action.Type, result.Error));
                    return;
                }
            }

            next(action);
        }
    }

    /// <summary>
    /// Action验证器接口
    /// </summary>
    public interface IActionValidator
    {
        ValidationResult Validate(IAction action);
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }

        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(string error) => new() { IsValid = false, Error = error };
    }

    /// <summary>
    /// 验证错误Action
    /// </summary>
    public class ValidationErrorAction : ActionBase
    {
        public string OriginalActionType { get; }
        public string Error { get; }

        public ValidationErrorAction(string originalActionType, string? error) 
            : base("VALIDATION_ERROR")
        {
            OriginalActionType = originalActionType;
            Error = error ?? "未知验证错误";
        }
    }
}