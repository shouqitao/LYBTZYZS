using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LYBT.WPF.Client.Core.Redux
{
    /// <summary>
    /// Reducer接口 - 处理状态更新的纯函数
    /// </summary>
    public interface IReducer<TState>
    {
        /// <summary>
        /// 处理Action并返回新状态
        /// </summary>
        TState Reduce(TState state, IAction action);
    }

    /// <summary>
    /// 组合多个Reducer
    /// </summary>
    public class CombinedReducer<TState> : IReducer<TState>
    {
        private readonly ImmutableList<IReducer<TState>> _reducers;

        public CombinedReducer(params IReducer<TState>[] reducers)
        {
            _reducers = reducers.ToImmutableList();
        }

        public CombinedReducer(IEnumerable<IReducer<TState>> reducers)
        {
            _reducers = reducers.ToImmutableList();
        }

        public TState Reduce(TState state, IAction action)
        {
            return _reducers.Aggregate(state, (current, reducer) => reducer.Reduce(current, action));
        }
    }

    /// <summary>
    /// 函数式Reducer
    /// </summary>
    public class FunctionalReducer<TState> : IReducer<TState>
    {
        private readonly Func<TState, IAction, TState> _reduceFunc;

        public FunctionalReducer(Func<TState, IAction, TState> reduceFunc)
        {
            _reduceFunc = reduceFunc ?? throw new ArgumentNullException(nameof(reduceFunc));
        }

        public TState Reduce(TState state, IAction action)
        {
            return _reduceFunc(state, action);
        }
    }

    /// <summary>
    /// 模式匹配Reducer
    /// </summary>
    public class PatternMatchingReducer<TState> : IReducer<TState>
    {
        private readonly Dictionary<string, Func<TState, IAction, TState>> _handlers = new();
        private readonly Func<TState, IAction, TState>? _defaultHandler;

        public PatternMatchingReducer(Func<TState, IAction, TState>? defaultHandler = null)
        {
            _defaultHandler = defaultHandler;
        }

        /// <summary>
        /// 注册Action处理器
        /// </summary>
        public PatternMatchingReducer<TState> On<TAction>(Func<TState, TAction, TState> handler) 
            where TAction : IAction
        {
            var actionType = typeof(TAction).Name;
            _handlers[actionType] = (state, action) => handler(state, (TAction)action);
            return this;
        }

        /// <summary>
        /// 注册指定类型的处理器
        /// </summary>
        public PatternMatchingReducer<TState> On(string actionType, Func<TState, IAction, TState> handler)
        {
            _handlers[actionType] = handler;
            return this;
        }

        public TState Reduce(TState state, IAction action)
        {
            // 优先使用Type属性匹配
            if (_handlers.TryGetValue(action.Type, out var typeHandler))
            {
                return typeHandler(state, action);
            }

            // 其次使用类型名匹配
            var actionTypeName = action.GetType().Name;
            if (_handlers.TryGetValue(actionTypeName, out var handler))
            {
                return handler(state, action);
            }

            // 使用默认处理器或返回原状态
            return _defaultHandler?.Invoke(state, action) ?? state;
        }
    }

    /// <summary>
    /// 不可变状态辅助类
    /// </summary>
    public static class ImmutableStateHelper
    {
        /// <summary>
        /// 创建新状态并修改指定属性
        /// </summary>
        public static T With<T, TValue>(this T state, 
            System.Linq.Expressions.Expression<Func<T, TValue>> selector, 
            TValue value) where T : class, new()
        {
            // 简化实现：使用JSON序列化进行深拷贝
            var json = System.Text.Json.JsonSerializer.Serialize(state);
            var newState = System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
            
            // 使用反射设置属性值
            var memberExpression = selector.Body as System.Linq.Expressions.MemberExpression;
            if (memberExpression != null)
            {
                var property = memberExpression.Member as System.Reflection.PropertyInfo;
                property?.SetValue(newState, value);
            }
            
            return newState;
        }

        /// <summary>
        /// 创建不可变字典
        /// </summary>
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull
        {
            return ImmutableDictionary.CreateRange(source);
        }

        /// <summary>
        /// 创建不可变列表
        /// </summary>
        public static ImmutableList<T> ToImmutableList<T>(this IEnumerable<T> source)
        {
            return ImmutableList.CreateRange(source);
        }
    }

    /// <summary>
    /// Reducer构建器
    /// </summary>
    public class ReducerBuilder<TState>
    {
        private readonly List<IReducer<TState>> _reducers = new();

        /// <summary>
        /// 添加Reducer
        /// </summary>
        public ReducerBuilder<TState> Add(IReducer<TState> reducer)
        {
            _reducers.Add(reducer);
            return this;
        }

        /// <summary>
        /// 添加函数式Reducer
        /// </summary>
        public ReducerBuilder<TState> Add(Func<TState, IAction, TState> reduceFunc)
        {
            _reducers.Add(new FunctionalReducer<TState>(reduceFunc));
            return this;
        }

        /// <summary>
        /// 添加模式匹配Reducer
        /// </summary>
        public ReducerBuilder<TState> AddPatternMatching(
            Action<PatternMatchingReducer<TState>> configure)
        {
            var reducer = new PatternMatchingReducer<TState>();
            configure(reducer);
            _reducers.Add(reducer);
            return this;
        }

        /// <summary>
        /// 构建最终Reducer
        /// </summary>
        public IReducer<TState> Build()
        {
            if (_reducers.Count == 0)
            {
                throw new InvalidOperationException("至少需要添加一个Reducer");
            }

            return _reducers.Count == 1 
                ? _reducers[0] 
                : new CombinedReducer<TState>(_reducers);
        }
    }
}