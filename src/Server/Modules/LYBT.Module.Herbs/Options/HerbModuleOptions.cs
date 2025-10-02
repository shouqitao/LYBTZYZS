namespace LYBT.Module.Herbs.Options
{
    /// <summary>
    /// 中药模块配置选项
    /// </summary>
    public class HerbModuleOptions
    {
        /// <summary>
        /// 分页默认大小
        /// </summary>
        public int DefaultPageSize { get; set; } = 20;

        /// <summary>
        /// 最大分页大小
        /// </summary>
        public int MaxPageSize { get; set; } = 100;

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCache { get; set; } = false;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 10;
    }
}
