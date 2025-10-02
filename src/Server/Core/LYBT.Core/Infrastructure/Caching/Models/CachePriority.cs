namespace LYBT.Core.Infrastructure.Caching.Models
{
    /// <summary>
    /// 缓存优先级枚举
    /// </summary>
    public enum CachePriority
    {
        /// <summary>
        /// 低优先级 - 在内存压力时最先被清理
        /// </summary>
        Low = 0,

        /// <summary>
        /// 普通优先级 - 默认优先级
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 高优先级 - 在内存压力时较晚被清理
        /// </summary>
        High = 2,

        /// <summary>
        /// 永不移除 - 除非显式移除或过期，否则不会被清理
        /// </summary>
        NeverRemove = 3
    }
}
