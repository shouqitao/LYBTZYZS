using LYBT.Entities.Auth;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 认证会话服务接口 - 管理用户登录会话的完整生命周期
    /// 提供会话创建、验证、刷新、撤销等核心功能
    /// </summary>
    public interface IAuthSessionService
    {
        /// <summary>
        /// 创建新的认证会话 - UltraThink v2.0简化版
        /// </summary>
        Task<BaseAuthSession> CreateSessionAsync(string username, Guid userId, string ipAddress, string? userAgent = null);

        /// <summary>
        /// 根据令牌哈希验证会话
        /// </summary>
        Task<BaseAuthSession?> ValidateSessionAsync(string tokenHash);

        /// <summary>
        /// 刷新会话令牌
        /// </summary>
        Task<BaseAuthSession?> RefreshSessionAsync(string refreshTokenHash, string newTokenHash, DateTime newExpiryTime);

        /// <summary>
        /// 撤销用户所有活跃会话
        /// </summary>
        Task RevokeAllUserSessionsAsync(Guid userId, string reason, Guid? revokedBy = null);

        /// <summary>
        /// 撤销特定会话
        /// </summary>
        Task RevokeSessionAsync(Guid sessionId, string reason, Guid? revokedBy = null);

        /// <summary>
        /// 用户登出
        /// </summary>
        Task LogoutSessionAsync(Guid sessionId);

        /// <summary>
        /// 获取用户的活跃会话列表
        /// </summary>
        Task<List<BaseAuthSession>> GetUserActiveSessionsAsync(Guid userId);

        /// <summary>
        /// 更新会话最后活跃时间
        /// </summary>
        Task UpdateSessionActivityAsync(Guid sessionId, DateTime lastActivity);

        /// <summary>
        /// 检查并清理过期会话
        /// </summary>
        Task CleanupExpiredSessionsAsync();

        /// <summary>
        /// 获取会话统计信息
        /// </summary>
        Task<(int TotalSessions, int ActiveSessions, int ExpiredSessions)> GetSessionStatisticsAsync();

        /// <summary>
        /// 检测可疑的会话活动
        /// </summary>
        Task<List<BaseAuthSession>> DetectSuspiciousSessionsAsync(TimeSpan timeWindow);

        /// <summary>
        /// 标记会话异常
        /// </summary>
        Task MarkSessionAnomalyAsync(Guid sessionId, string description);

        /// <summary>
        /// 批量更新会话状态 - UltraThink v2.0简化版
        /// </summary>
        Task UpdateSessionStatusBatchAsync(List<Guid> sessionIds, CommonStatus status, string? reason = null);

        /// <summary>
        /// 根据设备信息查找会话
        /// </summary>
        Task<List<BaseAuthSession>> GetSessionsByDeviceAsync(string deviceInfo, TimeSpan? timeWindow = null);

        /// <summary>
        /// 根据IP地址查找会话（安全监控）
        /// </summary>
        Task<List<BaseAuthSession>> GetSessionsByIpAddressAsync(string ipAddress, TimeSpan? timeWindow = null);

        /// <summary>
        /// 检查是否为可疑登录位置 - UltraThink v2.0简化版
        /// </summary>
        Task<bool> IsSuspiciousLocationAsync(Guid userId, string ipAddress, string? location = null);

        /// <summary>
        /// 设置会话扩展数据
        /// </summary>
        Task SetSessionExtendedDataAsync(Guid sessionId, string extendedData);

        /// <summary>
        /// 强制用户重新登录（管理员操作）
        /// </summary>
        Task ForceUserReloginAsync(Guid userId, string reason, Guid operatorId);
    }
}