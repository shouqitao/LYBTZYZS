using LYBT.Entities.Auth;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces
{

    /// <summary>
    /// 认证会话仓储接口 - Record-Only模式简化版本
    /// 继承BaseRepository提供通用CRUD，保留基本会话功能.
    /// </summary>
    public interface IAuthSessionRepository : IBaseRepository<AuthSession>
    {

        /// <summary>
        /// 根据用户ID获取活跃会话
        /// </summary>
        Task<List<AuthSession>> GetActiveSessionsByUserIdAsync(Guid userId);

        /// <summary>
        /// 根据用户名获取活跃会话
        /// </summary>
        [Obsolete("Complex session tracking removed in Record-Only mode. Use stateless JWT instead.", false)]
        Task<List<AuthSession>> GetActiveSessionsByUsernameAsync(string username);

        /// <summary>
        /// 根据JWT令牌哈希查找会话
        /// </summary>
        Task<AuthSession?> GetByTokenHashAsync(string tokenHash);

        /// <summary>
        /// 根据刷新令牌哈希查找会话
        /// </summary>
        [Obsolete("Complex refresh token mechanism removed in Record-Only mode. Use stateless JWT instead.", false)]
        Task<AuthSession?> GetByRefreshTokenHashAsync(string refreshTokenHash);

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
        [Obsolete("Session activity tracking removed in Record-Only mode. Use stateless JWT instead.", false)]
        Task UpdateLastActivityAsync(Guid sessionId, DateTime lastActivity);

        /// <summary>
        /// 清理过期会话
        /// </summary>
        Task CleanupExpiredSessionsAsync();

        /// <summary>
        /// 获取会话统计信息
        /// </summary>
        [Obsolete("Complex session statistics removed in Record-Only mode. Use simple user count instead.", false)]
        Task<(int TotalSessions, int ActiveSessions, int ExpiredSessions)> GetSessionStatsAsync();

        /// <summary>
        /// 根据IP地址获取会话（安全监控）
        /// </summary>
        [Obsolete("Complex IP-based session monitoring removed in Record-Only mode. Use basic audit logs instead.", false)]
        Task<List<AuthSession>> GetSessionsByIpAddressAsync(string ipAddress, TimeSpan? withinTimeSpan = null);

        /// <summary>
        /// 标记会话异常
        /// </summary>
        [Obsolete("Complex session anomaly detection removed in Record-Only mode. Use basic audit logs instead.", false)]
        Task MarkSessionAnomalyAsync(Guid sessionId, string description);

        /// <summary>
        /// 批量更新会话状态 - UltraThink v2.0简化版
        /// </summary>
        Task UpdateSessionStatusBatchAsync(List<Guid> sessionIds, CommonStatus status, string? reason = null);

        /// <summary>
        /// 根据设备信息获取会话
        /// </summary>
        [Obsolete("Complex device-based session tracking removed in Record-Only mode. Use stateless JWT instead.", false)]
        Task<List<AuthSession>> GetSessionsByDeviceInfoAsync(string deviceInfo, TimeSpan? withinTimeSpan = null);
    }
}
