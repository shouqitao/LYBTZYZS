namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// JWT黑名单服务接口
    /// 用于管理被撤销的JWT Token
    /// </summary>
    public interface IJwtBlacklistService
    {
        /// <summary>
        /// 将Token添加到黑名单
        /// </summary>
        /// <param name="jwtId">JWT ID</param>
        /// <param name="expiration">Token过期时间</param>
        /// <returns>操作结果</returns>
        Task<bool> AddToBlacklistAsync(string jwtId, DateTime expiration);

        /// <summary>
        /// 检查Token是否在黑名单中
        /// </summary>
        /// <param name="jwtId">JWT ID</param>
        /// <returns>是否在黑名单中</returns>
        Task<bool> IsBlacklistedAsync(string jwtId);

        /// <summary>
        /// 批量将Token添加到黑名单
        /// </summary>
        /// <param name="tokenInfos">Token信息列表</param>
        /// <returns>成功添加的数量</returns>
        Task<int> AddMultipleToBlacklistAsync(IEnumerable<(string JwtId, DateTime Expiration)> tokenInfos);

        /// <summary>
        /// 清理过期的黑名单记录
        /// </summary>
        /// <returns>清理的记录数量</returns>
        Task<int> CleanupExpiredAsync();

        /// <summary>
        /// 获取黑名单统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        Task<BlacklistStats> GetStatsAsync();
    }

    /// <summary>
    /// 黑名单统计信息
    /// </summary>
    public class BlacklistStats
    {
        /// <summary>
        /// 黑名单中的Token总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 今天添加的Token数量
        /// </summary>
        public int TodayAddedCount { get; set; }

        /// <summary>
        /// 最近清理时间
        /// </summary>
        public DateTime? LastCleanupTime { get; set; }

        /// <summary>
        /// 内存使用情况（字节）
        /// </summary>
        public long MemoryUsage { get; set; }
    }
}