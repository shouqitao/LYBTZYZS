using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LYBT.Desktop.Core.Mvvm;

namespace LYBT.Desktop.Core.Redux
{

    /// <summary>
    /// Redux状态ViewModel基类 - 自动订阅Store变化
    /// </summary>
    public abstract class StateViewModel<TState> : ObservableObject, IDisposable
        where TState : class, new()
    {
        private readonly IStateStore<TState> _store;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        /// <summary>
        /// 当前状态
        /// </summary>
        protected TState State => _store.State;

        /// <summary>
        /// Store引用
        /// </summary>
        protected IStateStore<TState> Store => _store;

        protected StateViewModel(IStateStore<TState> store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));

            // 订阅整体状态变化
            var subscription = _store.Subscribe(OnStateChanged);
            _subscriptions.Add(subscription);

            // 初始化选择器
            InitializeSelectors();
        }

        /// <summary>
        /// 初始化选择器（子类重写）
        /// </summary>
        protected virtual void InitializeSelectors()
        {
        }

        /// <summary>
        /// 状态变化处理
        /// </summary>
        protected virtual void OnStateChanged(TState state)
        {
            // 触发所有属性更新
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// 选择性订阅状态片段
        /// </summary>
        protected void Select<TSlice>(
            Expression<Func<TState, TSlice>> selector,
            Action<TSlice> onChange,
            [CallerMemberName] string? propertyName = null)
        {
            var selectorFunc = selector.Compile();

            var subscription = _store.Subscribe(selectorFunc, slice =>
            {
                onChange(slice);
                if (!string.IsNullOrEmpty(propertyName))
                {
                    OnPropertyChanged(propertyName);
                }
            });

            _subscriptions.Add(subscription);
        }

        /// <summary>
        /// 分发Action
        /// </summary>
        protected void Dispatch(IAction action)
        {
            _store.Dispatch(action);
        }

        /// <summary>
        /// 创建分发命令
        /// </summary>
        protected ICommand CreateDispatchCommand(Func<IAction> actionFactory)
        {
            return new RelayCommand(() => Dispatch(actionFactory()));
        }

        /// <summary>
        /// 创建带参数的分发命令
        /// </summary>
        protected ICommand CreateDispatchCommand<TParam>(Func<TParam, IAction> actionFactory)
        {
            return new RelayCommand<TParam>(param =>
            {
                if (param != null)
                {
                    Dispatch(actionFactory(param));
                }
            });
        }

        /// <summary>
        /// 创建异步分发命令
        /// </summary>
        protected ICommand CreateAsyncDispatchCommand(Func<Task<IAction>> actionFactory)
        {
            return new AsyncRelayCommand(async () =>
            {
                var action = await actionFactory();
                Dispatch(action);
            });
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription?.Dispose();
                }
                _subscriptions.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 具有局部状态的StateViewModel
    /// </summary>
    public abstract class StateViewModel<TState, TLocalState> : StateViewModel<TState>
        where TState : class, new()
        where TLocalState : class, new()
    {
        private TLocalState _localState;

        /// <summary>
        /// 局部状态（不在Store中）
        /// </summary>
        protected TLocalState LocalState
        {
            get => _localState;
            set => SetProperty(ref _localState, value);
        }

        protected StateViewModel(IStateStore<TState> store, TLocalState? initialLocalState = null)
            : base(store)
        {
            _localState = initialLocalState ?? new TLocalState();
        }

        /// <summary>
        /// 更新局部状态属性
        /// </summary>
        protected void UpdateLocal<TValue>(
            Expression<Func<TLocalState, TValue>> selector,
            TValue value,
            [CallerMemberName] string? propertyName = null)
        {
            // 使用反射更新属性
            var memberExpression = selector.Body as MemberExpression;
            if (memberExpression != null)
            {
                var property = memberExpression.Member as System.Reflection.PropertyInfo;
                property?.SetValue(_localState, value);
                OnPropertyChanged(propertyName);
            }
        }
    }

    /// <summary>
    /// 状态选择器
    /// </summary>
    public class StateSelector<TState, TSlice> : INotifyPropertyChanged
    {
        private readonly IStateStore<TState> _store;
        private readonly Func<TState, TSlice> _selector;
        private TSlice? _value;
        private IDisposable? _subscription;

        public TSlice? Value
        {
            get => _value;
            private set
            {
                if (!EqualityComparer<TSlice>.Default.Equals(_value, value))
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        public StateSelector(IStateStore<TState> store, Func<TState, TSlice> selector)
        {
            _store = store;
            _selector = selector;
            Subscribe();
        }

        private void Subscribe()
        {
            _subscription = _store.Subscribe(_selector, slice => Value = slice);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 自动映射ViewModel - 自动映射状态到属性
    /// </summary>
    public abstract class AutoMappedViewModel<TState> : StateViewModel<TState>
        where TState : class, new()
    {
        private readonly Dictionary<string, Func<TState, object?>> _propertySelectors = new();

        protected AutoMappedViewModel(IStateStore<TState> store) : base(store)
        {
            InitializePropertyMappings();
        }

        /// <summary>
        /// 初始化属性映射
        /// </summary>
        protected abstract void InitializePropertyMappings();

        /// <summary>
        /// 映射状态属性到ViewModel属性
        /// </summary>
        protected void MapProperty<TValue>(
            Expression<Func<TState, TValue>> stateSelector,
            [CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            var selector = stateSelector.Compile();
            _propertySelectors[propertyName] = state => selector(state);

            // 订阅该属性的变化
            Select(stateSelector, _ => OnPropertyChanged(propertyName), propertyName);
        }

        /// <summary>
        /// 获取映射的属性值
        /// </summary>
        protected T? GetMappedValue<T>([CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName) || !_propertySelectors.TryGetValue(propertyName, out var selector))
            {
                return default;
            }

            var value = selector(State);
            return value is T typedValue ? typedValue : default;
        }
    }

    /// <summary>
    /// 集合StateViewModel - 处理列表数据
    /// </summary>
    public abstract class CollectionStateViewModel<TState, TItem> : StateViewModel<TState>
        where TState : class, new()
    {
        private readonly ObservableCollection<TItem> _items = new();

        /// <summary>
        /// 可观察集合
        /// </summary>
        public ObservableCollection<TItem> Items => _items;

        protected CollectionStateViewModel(IStateStore<TState> store) : base(store)
        {
        }

        /// <summary>
        /// 更新集合
        /// </summary>
        protected void UpdateCollection(IEnumerable<TItem> newItems)
        {
            _items.Clear();
            foreach (var item in newItems)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// 批量更新集合（优化性能）
        /// </summary>
        protected void BatchUpdateCollection(IEnumerable<TItem> newItems)
        {
            using (BeginBatchUpdate())
            {
                _items.Clear();
                foreach (var item in newItems)
                {
                    _items.Add(item);
                }
            }
        }
    }
}
