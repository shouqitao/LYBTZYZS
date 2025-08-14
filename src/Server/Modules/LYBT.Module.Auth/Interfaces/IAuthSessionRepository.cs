using LYBT.Infrastructure.Interfaces;
using LYBT.Entities.Auth;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 认证会话仓储接口 - 管理用户登录会话的完整生命周期
    /// 继承BaseRepository提供通用CRUD，扩展会话管理特定业务方法
    /// </summary>
    public interface IAuthSessionRepository : IBaseRepository<AuthSessionModel>
    {
        /// <summary>
        /// 根据用户ID获取活跃会话
        /// </summary>
        Task<List<AuthSessionModel>> GetActiveSessionsByUserIdAsync(Guid userId);

        /// <summary>
        /// 根据用户名获取活跃会话
        /// </summary>
        Task<List<AuthSessionModel>> GetActiveSessionsByUsernameAsync(string username);

        /// <summary>
        /// 根据JWT令牌哈希查找会话
        /// </summary>
        Task<AuthSessionModel?> GetByTokenHashAsync(string tokenHash);

        /// <summary>
        /// 根据刷新令牌哈希查找会话
        /// </summary>
        Task<AuthSessionModel?> GetByRefreshTokenHashAsync(string refreshTokenHash);

        /// <summary>
        /// 撤销用户的所有活跃会话
        /// </summary>
        Task RevokeAllUserSessionsAsync(Guid userId, string reason, Guid? revokedBy = null);

        /// <summary>
        /// 撤销特定会话
        /// </summary>
        Task RevokeSessionAsync(Guid sessionId, string reason, Guid? revokedBy = null);

        /// <summary>
        /// 更新会话最后活跃时间
        /// </summary>
        Task UpdateLastActivityAsync(Guid sessionId, DateTime lastActivity);

        /// <summary>
        /// 清理过期会话
        /// </summary>
        Task CleanupExpiredSessionsAsync();

        /// <summary>
        /// 获取会话统计信息
        /// </summary>
        Task<(int TotalSessions, int ActiveSessions, int ExpiredSessions)> GetSessionStatsAsync();

        /// <summary>
        /// 根据IP地址获取会话（安全监控）
        /// </summary>
        Task<List<AuthSessionModel>> GetSessionsByIpAddressAsync(string ipAddress, TimeSpan? withinTimeSpan = null);

        /// <summary>
        /// 标记会话异常
        /// </summary>
        Task MarkSessionAnomalyAsync(Guid sessionId, string description);

        /// <summary>
        /// 批量更新会话状态
        /// </summary>
        Task UpdateSessionStatusBatchAsync(List<Guid> sessionIds, AuthSessionStatus status, string? reason = null);

        /// <summary>
        /// 根据设备信息获取会话
        /// </summary>
        Task<List<AuthSessionModel>> GetSessionsByDeviceInfoAsync(string deviceInfo, TimeSpan? withinTimeSpan = null);
    }
}