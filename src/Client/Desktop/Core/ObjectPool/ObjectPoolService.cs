using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace LYBT.Desktop.Core.ObjectPool
{

    /// <summary>
    /// 对象池服务 - 减少频繁创建对象的GC压力
    /// </summary>
    public interface IObjectPoolService
    {

        /// <summary>
        /// 获取对象池
        /// </summary>
        ObjectPool<T> GetPool<T>() where T : class, new();

        /// <summary>
        /// 获取自定义对象池
        /// </summary>
        ObjectPool<T> GetPool<T>(IPooledObjectPolicy<T> policy) where T : class;

        /// <summary>
        /// 租用对象
        /// </summary>
        T Rent<T>() where T : class, new();

        /// <summary>
        /// 归还对象
        /// </summary>
        void Return<T>(T obj) where T : class, new();

        /// <summary>
        /// 获取池统计信息
        /// </summary>
        PoolStatistics GetStatistics<T>() where T : class;

        /// <summary>
        /// 清理池
        /// </summary>
        void ClearPool<T>() where T : class;
    }

    /// <summary>
    /// 对象池服务实现
    /// </summary>
    public class ObjectPoolService : IObjectPoolService
    {
        private readonly ConcurrentDictionary<Type, object> _pools = new();
        private readonly ConcurrentDictionary<Type, PoolStatistics> _statistics = new();
        private readonly ILogger<ObjectPoolService>? _logger;
        private readonly ObjectPoolProvider _poolProvider;

        public ObjectPoolService(ILogger<ObjectPoolService>? logger = null)
        {
            _logger = logger;
            _poolProvider = new DefaultObjectPoolProvider();
        }

        /// <summary>
        /// 获取默认对象池
        /// </summary>
        public ObjectPool<T> GetPool<T>() where T : class, new()
        {
            return (ObjectPool<T>)_pools.GetOrAdd(typeof(T), type =>
            {
                var policy = new DefaultPooledObjectPolicy<T>();
                var pool = _poolProvider.Create(policy);

                _statistics[typeof(T)] = new PoolStatistics { TypeName = typeof(T).Name };
                _logger?.LogDebug($"创建对象池: {typeof(T).Name}");

                return pool;
            });
        }

        /// <summary>
        /// 获取自定义对象池
        /// </summary>
        public ObjectPool<T> GetPool<T>(IPooledObjectPolicy<T> policy) where T : class
        {
            return (ObjectPool<T>)_pools.GetOrAdd(typeof(T), type =>
            {
                var pool = _poolProvider.Create(policy);

                _statistics[typeof(T)] = new PoolStatistics { TypeName = typeof(T).Name };
                _logger?.LogDebug($"创建自定义对象池: {typeof(T).Name}");

                return pool;
            });
        }

        /// <summary>
        /// 租用对象
        /// </summary>
        public T Rent<T>() where T : class, new()
        {
            var pool = GetPool<T>();
            var obj = pool.Get();

            if (_statistics.TryGetValue(typeof(T), out var stats))
            {
                Interlocked.Increment(ref stats.RentCount);
                Interlocked.Increment(ref stats.ActiveCount);
            }

            return obj;
        }

        /// <summary>
        /// 归还对象
        /// </summary>
        public void Return<T>(T obj) where T : class, new()
        {
            if (obj == null)
            {
                return;
            }

            var pool = GetPool<T>();
            pool.Return(obj);

            if (_statistics.TryGetValue(typeof(T), out var stats))
            {
                Interlocked.Increment(ref stats.ReturnCount);
                Interlocked.Decrement(ref stats.ActiveCount);
            }
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public PoolStatistics GetStatistics<T>() where T : class
        {
            return _statistics.GetValueOrDefault(typeof(T)) ?? new PoolStatistics { TypeName = typeof(T).Name };
        }

        /// <summary>
        /// 清理池
        /// </summary>
        public void ClearPool<T>() where T : class
        {
            if (_pools.TryRemove(typeof(T), out var pool))
            {
                _statistics.TryRemove(typeof(T), out _);
                _logger?.LogDebug($"清理对象池: {typeof(T).Name}");
            }
        }
    }

    /// <summary>
    /// 池统计信息
    /// </summary>
    public class PoolStatistics
    {
        public string TypeName { get; set; } = string.Empty;
        public long RentCount;
        public long ReturnCount;
        public long ActiveCount;
        public double ReturnRate => RentCount > 0 ? (double)ReturnCount / RentCount : 0;
    }

    /// <summary>
    /// 可池化对象接口
    /// </summary>
    public interface IPoolable
    {

        /// <summary>
        /// 重置对象状态
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// 列表对象池策略
    /// </summary>
    public class ListPooledObjectPolicy<T> : IPooledObjectPolicy<List<T>>
    {
        private readonly int _initialCapacity;
        private readonly int _maxCapacity;

        public ListPooledObjectPolicy(int initialCapacity = 16, int maxCapacity = 1024)
        {
            _initialCapacity = initialCapacity;
            _maxCapacity = maxCapacity;
        }

        public List<T> Create()
        {
            return new List<T>(_initialCapacity);
        }

        public bool Return(List<T> obj)
        {
            if (obj.Capacity > _maxCapacity)
            {
                // 容量太大，不回收
                return false;
            }

            obj.Clear();
            return true;
        }
    }

    /// <summary>
    /// StringBuilder对象池策略
    /// </summary>
    public class StringBuilderPooledObjectPolicy : IPooledObjectPolicy<System.Text.StringBuilder>
    {
        private readonly int _initialCapacity;
        private readonly int _maxCapacity;

        public StringBuilderPooledObjectPolicy(int initialCapacity = 256, int maxCapacity = 8192)
        {
            _initialCapacity = initialCapacity;
            _maxCapacity = maxCapacity;
        }

        public System.Text.StringBuilder Create()
        {
            return new System.Text.StringBuilder(_initialCapacity);
        }

        public bool Return(System.Text.StringBuilder obj)
        {
            if (obj.Capacity > _maxCapacity)
            {
                // 容量太大，不回收
                return false;
            }

            obj.Clear();
            return true;
        }
    }

    /// <summary>
    /// 数组池包装器
    /// </summary>
    public class ArrayPoolWrapper<T>
    {
        private readonly System.Buffers.ArrayPool<T> _pool;
        private readonly Dictionary<T[], int> _rentedArrays = new();
        private readonly object _lock = new();

        public ArrayPoolWrapper()
        {
            _pool = System.Buffers.ArrayPool<T>.Shared;
        }

        /// <summary>
        /// 租用数组
        /// </summary>
        public T[] Rent(int minimumLength)
        {
            var array = _pool.Rent(minimumLength);

            lock (_lock)
            {
                _rentedArrays[array] = minimumLength;
            }

            return array;
        }

        /// <summary>
        /// 归还数组
        /// </summary>
        public void Return(T[] array, bool clearArray = false)
        {
            if (array == null)
            {
                return;
            }

            lock (_lock)
            {
                _rentedArrays.Remove(array);
            }

            _pool.Return(array, clearArray);
        }

        /// <summary>
        /// 使用数组（自动归还）
        /// </summary>
        public TResult Use<TResult>(int length, Func<T[], TResult> action, bool clearArray = false)
        {
            var array = Rent(length);
            try
            {
                return action(array);
            }
            finally
            {
                Return(array, clearArray);
            }
        }
    }

    /// <summary>
    /// 对象池扩展方法
    /// </summary>
    public static class ObjectPoolExtensions
    {

        /// <summary>
        /// 使用池化对象（自动归还）
        /// </summary>
        public static TResult Use<T, TResult>(this ObjectPool<T> pool, Func<T, TResult> action)
            where T : class
        {
            var obj = pool.Get();
            try
            {
                return action(obj);
            }
            finally
            {
                pool.Return(obj);
            }
        }

        /// <summary>
        /// 使用池化对象（异步，自动归还）
        /// </summary>
        public static async Task<TResult> UseAsync<T, TResult>(this ObjectPool<T> pool, Func<T, Task<TResult>> action)
            where T : class
        {
            var obj = pool.Get();
            try
            {
                return await action(obj);
            }
            finally
            {
                pool.Return(obj);
            }
        }
    }

    /// <summary>
    /// 池化对象包装器（实现IDisposable自动归还）
    /// </summary>
    public struct PooledObject<T> : IDisposable where T : class
    {
        private readonly ObjectPool<T> _pool;
        private T? _object;

        public PooledObject(ObjectPool<T> pool)
        {
            _pool = pool;
            _object = pool.Get();
        }

        public T Object => _object ?? throw new ObjectDisposedException(nameof(PooledObject<T>));

        public void Dispose()
        {
            if (_object != null)
            {
                _pool.Return(_object);
                _object = null;
            }
        }
    }

    /// <summary>
    /// 高性能对象池（固定大小）
    /// </summary>
    public class FixedSizeObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _objects = new();
        private readonly Func<T> _objectGenerator;
        private readonly Action<T>? _resetAction;
        private readonly int _maxSize;
        private int _currentSize;

        public FixedSizeObjectPool(Func<T> objectGenerator, Action<T>? resetAction = null, int maxSize = 100)
        {
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _resetAction = resetAction;
            _maxSize = maxSize;
        }

        /// <summary>
        /// 租用对象
        /// </summary>
        public T Rent()
        {
            if (_objects.TryTake(out var item))
            {
                return item;
            }

            // 创建新对象
            Interlocked.Increment(ref _currentSize);
            return _objectGenerator();
        }

        /// <summary>
        /// 归还对象
        /// </summary>
        public void Return(T item)
        {
            if (item == null)
            {
                return;
            }

            // 重置对象
            _resetAction?.Invoke(item);

            // 如果池已满，丢弃对象
            if (_currentSize <= _maxSize)
            {
                _objects.Add(item);
            }
            else
            {
                Interlocked.Decrement(ref _currentSize);

                // 如果对象实现IDisposable，释放它
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            while (_objects.TryTake(out var item))
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _currentSize = 0;
        }

        /// <summary>
        /// 获取池大小
        /// </summary>
        public int Count => _objects.Count;
    }
}
