namespace LYBT.Desktop.Core.Models.Cache {

    /// <summary>
    /// 缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    public class CacheEntry<T> : CacheEntryBase {

        /// <summary>
        /// 缓存值
        /// </summary>
        public T? Value { get; set; }

        /// <summary>
        /// 获取缓存值
        /// </summary>
        /// <returns>缓存值</returns>
        public override object? GetValue() {
            return Value;
        }

        /// <summary>
        /// 是否已过期
        /// </summary>
        public override bool IsExpired {
            get {
                var now = DateTimeOffset.Now;

                // 检查绝对过期时间
                if (AbsoluteExpiration.HasValue && now >= AbsoluteExpiration.Value) {
                    return true;
                }

                // 检查滑动过期时间
                if (SlidingExpiration.HasValue) {
                    var slidingExpiry = LastAccessedAt.Add(SlidingExpiration.Value);
                    if (DateTime.Now >= slidingExpiry) {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 剩余生存时间
        /// </summary>
        public override TimeSpan? TimeToLive {
            get {
                if (AbsoluteExpiration.HasValue) {
                    var ttl = AbsoluteExpiration.Value - DateTimeOffset.Now;
                    return ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero;
                }

                if (SlidingExpiration.HasValue) {
                    var ttl = LastAccessedAt.Add(SlidingExpiration.Value) - DateTime.Now;
                    return ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero;
                }

                return null;
            }
        }

        /// <summary>
        /// 创建缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="policy">缓存策略</param>
        /// <returns>缓存项实例</returns>
        public static CacheEntry<T> Create(string key, T value, CachePolicy policy) {
            return new CacheEntry<T> {
                Key = key,
                Value = value,
                CreatedAt = DateTime.Now,
                LastAccessedAt = DateTime.Now,
                AccessCount = 0,
                AbsoluteExpiration = policy.AbsoluteExpiration,
                SlidingExpiration = policy.SlidingExpiration,
                Priority = policy.Priority,
                Partition = policy.Partition,
                Dependencies = policy.Dependencies != null ? new List<string>(policy.Dependencies) : null,
                EstimatedSize = EstimateSize(value)
            };
        }

        /// <summary>
        /// 估算对象大小
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>估算大小（字节）</returns>
        private static long EstimateSize(T? obj) {
            if (obj == null) {
                return 0;
            }

            // 基本类型的大小估算
            var type = typeof(T);

            if (type == typeof(string)) {
                var str = obj as string;
                return str?.Length * 2 ?? 0; // Unicode字符占用2字节
            }

            if (type.IsPrimitive) {
                return type == typeof(bool) ? 1 :
                       type == typeof(byte) ? 1 :
                       type == typeof(sbyte) ? 1 :
                       type == typeof(char) ? 2 :
                       type == typeof(short) ? 2 :
                       type == typeof(ushort) ? 2 :
                       type == typeof(int) ? 4 :
                       type == typeof(uint) ? 4 :
                       type == typeof(long) ? 8 :
                       type == typeof(ulong) ? 8 :
                       type == typeof(float) ? 4 :
                       type == typeof(double) ? 8 :
                       8; // 默认8字节
            }

            if (type == typeof(DateTime)) {
                return 8;
            }

            if (type == typeof(DateTimeOffset)) {
                return 16;
            }

            if (type == typeof(TimeSpan)) {
                return 8;
            }

            if (type == typeof(Guid)) {
                return 16;
            }

            // 集合类型的粗略估算
            if (obj is System.Collections.ICollection collection) {
                return collection.Count * 64; // 每个元素假设64字节
            }

            // 其他复杂类型的默认估算
            return 256; // 默认256字节
        }
    }

    /// <summary>
    /// 非泛型缓存项基类
    /// </summary>
    public abstract class CacheEntryBase {

        /// <summary>
        /// 缓存键
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最后访问时间
        /// </summary>
        public DateTime LastAccessedAt { get; set; }

        /// <summary>
        /// 访问次数
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// 绝对过期时间
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// 滑动过期时间
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public CachePriority Priority { get; set; }

        /// <summary>
        /// 分区名称
        /// </summary>
        public string? Partition { get; set; }

        /// <summary>
        /// 依赖的缓存键
        /// </summary>
        public List<string>? Dependencies { get; set; }

        /// <summary>
        /// 估算大小（字节）
        /// </summary>
        public long EstimatedSize { get; set; }

        /// <summary>
        /// 获取缓存值
        /// </summary>
        /// <returns>缓存值</returns>
        public abstract object? GetValue();

        /// <summary>
        /// 是否已过期
        /// </summary>
        public abstract bool IsExpired { get; }

        /// <summary>
        /// 剩余生存时间
        /// </summary>
        public abstract TimeSpan? TimeToLive { get; }

        /// <summary>
        /// 更新访问统计
        /// </summary>
        public virtual void UpdateAccessStats() {
            LastAccessedAt = DateTime.Now;
            AccessCount++;
        }
    }

    /// <summary>
    /// 缓存优先级
    /// </summary>
    public enum CachePriority {
        Low,
        Normal,
        High,
        NeverRemove
    }

    /// <summary>
    /// 缓存策略
    /// </summary>
    public class CachePolicy {

        /// <summary>
        /// 绝对过期时间
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// 相对过期时间
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public CachePriority Priority { get; set; } = CachePriority.Normal;

        /// <summary>
        /// 分区名称
        /// </summary>
        public string? Partition { get; set; }

        /// <summary>
        /// 依赖的缓存键（当依赖项变化时，此项也会失效）
        /// </summary>
        public List<string>? Dependencies { get; set; }

        /// <summary>
        /// 创建默认策略
        /// </summary>
        /// <param name="expiration">过期时间</param>
        /// <returns>缓存策略</returns>
        public static CachePolicy Default(TimeSpan expiration) {
            return new CachePolicy {
                SlidingExpiration = expiration
            };
        }

        /// <summary>
        /// 创建绝对过期策略
        /// </summary>
        /// <param name="expiration">绝对过期时间</param>
        /// <returns>缓存策略</returns>
        public static CachePolicy Absolute(DateTimeOffset expiration) {
            return new CachePolicy {
                AbsoluteExpiration = expiration
            };
        }

        /// <summary>
        /// 创建滑动过期策略
        /// </summary>
        /// <param name="expiration">滑动过期时间</param>
        /// <returns>缓存策略</returns>
        public static CachePolicy Sliding(TimeSpan expiration) {
            return new CachePolicy {
                SlidingExpiration = expiration
            };
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics {

        /// <summary>
        /// 命中次数
        /// </summary>
        public long HitCount { get; set; }

        /// <summary>
        /// 未命中次数
        /// </summary>
        public long MissCount { get; set; }

        /// <summary>
        /// 总请求次数
        /// </summary>
        public long TotalRequests => HitCount + MissCount;

        /// <summary>
        /// 命中率
        /// </summary>
        public double HitRate => TotalRequests == 0 ? 0 : (double)HitCount / TotalRequests;

        /// <summary>
        /// 缓存项数量
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// 估算内存占用（字节）
        /// </summary>
        public long EstimatedMemoryUsage { get; set; }

        /// <summary>
        /// 统计开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 上次清理时间
        /// </summary>
        public DateTime? LastCleanupTime { get; set; }

        /// <summary>
        /// 清理次数
        /// </summary>
        public int CleanupCount { get; set; }
    }
}
