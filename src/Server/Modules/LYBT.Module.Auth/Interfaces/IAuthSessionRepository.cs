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
        /// 根据JWT令牌哈希查找会话
        /// </summary>
        Task<AuthSession?> GetByTokenHashAsync(string tokenHash);


        /// <summary>
        /// 撤销用户的所有活跃会话
        /// </summary>
        Task RevokeAllUserSessionsAsync(Guid userId, string reason, Guid? revokedBy = null);

        /// <summary>
        /// 撤销特定会话
        /// </summary>
        Task RevokeSessionAsync(Guid sessionId, string reason, Guid? revokedBy = null);


        /// <summary>
        /// 清理过期会话
        /// </summary>
        Task CleanupExpiredSessionsAsync();




        /// <summary>
        /// 批量更新会话状态 - UltraThink v2.0简化版
        /// </summary>
        Task UpdateSessionStatusBatchAsync(List<Guid> sessionIds, CommonStatus status, string? reason = null);

    }
}
