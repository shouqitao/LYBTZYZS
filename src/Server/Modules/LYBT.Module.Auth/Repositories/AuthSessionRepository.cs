using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Entities.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Repositories
{
    /// <summary>
    /// 认证会话仓储实现 - 管理用户登录会话的完整生命周期
    /// 继承BaseRepository获得通用CRUD功能，扩展会话管理特有业务方法
    /// </summary>
    public class AuthSessionRepository : BaseRepository<AuthSessionModel>, IAuthSessionRepository
    {
        /// <summary>
        /// 初始化仓储并注入统一数据库上下文
        /// </summary>
        public AuthSessionRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据用户ID获取活跃会话
        /// </summary>
        public async Task<List<AuthSessionModel>> GetActiveSessionsByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId && s.Status == AuthSessionStatus.Active)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据用户名获取活跃会话
        /// </summary>
        public async Task<List<AuthSessionModel>> GetActiveSessionsByUsernameAsync(string username)
        {
            return await _dbSet
                .Where(s => s.Username == username && s.Status == AuthSessionStatus.Active)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据JWT令牌哈希查找会话
        /// </summary>
        public async Task<AuthSessionModel?> GetByTokenHashAsync(string tokenHash)
        {
            return await _dbSet
                .FirstOrDefaultAsync(s => s.JwtTokenHash == tokenHash && !s.IsTokenRevoked);
        }

        /// <summary>
        /// 根据刷新令牌哈希查找会话
        /// </summary>
        public async Task<AuthSessionModel?> GetByRefreshTokenHashAsync(string refreshTokenHash)
        {
            return await _dbSet
                .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash && s.Status == AuthSessionStatus.Active);
        }

        /// <summary>
        /// 撤销用户的所有活跃会话
        /// </summary>
        public async Task RevokeAllUserSessionsAsync(Guid userId, string reason, Guid? revokedBy = null)
        {
            var activeSessions = await _dbSet
                .Where(s => s.UserId == userId && s.Status == AuthSessionStatus.Active)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.Status = AuthSessionStatus.Revoked;
                session.IsTokenRevoked = true;
                session.RevokeReason = reason;
                session.RevokeTime = DateTime.Now;
                session.RevokedBy = revokedBy;
                session.LogoutTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 撤销特定会话
        /// </summary>
        public async Task RevokeSessionAsync(Guid sessionId, string reason, Guid? revokedBy = null)
        {
            var session = await _dbSet.FindAsync(sessionId);
            if (session != null && session.Status == AuthSessionStatus.Active)
            {
                session.Status = AuthSessionStatus.Revoked;
                session.IsTokenRevoked = true;
                session.RevokeReason = reason;
                session.RevokeTime = DateTime.Now;
                session.RevokedBy = revokedBy;
                session.LogoutTime = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 更新会话最后活跃时间
        /// </summary>
        public async Task UpdateLastActivityAsync(Guid sessionId, DateTime lastActivity)
        {
            var session = await _dbSet.FindAsync(sessionId);
            if (session != null)
            {
                session.LastActivityTime = lastActivity;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 清理过期会话
        /// </summary>
        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _dbSet
                .Where(s => s.Status == AuthSessionStatus.Active && 
                           s.TokenExpiryTime.HasValue && 
                           s.TokenExpiryTime.Value < DateTime.Now)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.Status = AuthSessionStatus.Expired;
                session.LogoutTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 获取会话统计信息
        /// </summary>
        public async Task<(int TotalSessions, int ActiveSessions, int ExpiredSessions)> GetSessionStatsAsync()
        {
            var totalSessions = await _dbSet.CountAsync();
            var activeSessions = await _dbSet.CountAsync(s => s.Status == AuthSessionStatus.Active);
            var expiredSessions = await _dbSet.CountAsync(s => s.Status == AuthSessionStatus.Expired);

            return (totalSessions, activeSessions, expiredSessions);
        }

        /// <summary>
        /// 根据IP地址获取会话（安全监控）
        /// </summary>
        public async Task<List<AuthSessionModel>> GetSessionsByIpAddressAsync(string ipAddress, TimeSpan? withinTimeSpan = null)
        {
            var query = _dbSet.Where(s => s.ClientIp == ipAddress);

            if (withinTimeSpan.HasValue)
            {
                var cutoffTime = DateTime.Now - withinTimeSpan.Value;
                query = query.Where(s => s.LoginTime >= cutoffTime);
            }

            return await query
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }

        /// <summary>
        /// 标记会话异常
        /// </summary>
        public async Task MarkSessionAnomalyAsync(Guid sessionId, string description)
        {
            var session = await _dbSet.FindAsync(sessionId);
            if (session != null)
            {
                session.HasAnomalies = true;
                session.AnomaliesDescription = description;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 批量更新会话状态
        /// </summary>
        public async Task UpdateSessionStatusBatchAsync(List<Guid> sessionIds, AuthSessionStatus status, string? reason = null)
        {
            var sessions = await _dbSet
                .Where(s => sessionIds.Contains(s.Id))
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.Status = status;
                
                if (status == AuthSessionStatus.Revoked)
                {
                    session.IsTokenRevoked = true;
                    session.RevokeTime = DateTime.Now;
                    session.RevokeReason = reason;
                }
                else if (status == AuthSessionStatus.LoggedOut || status == AuthSessionStatus.Expired)
                {
                    session.LogoutTime = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 根据设备信息获取会话
        /// </summary>
        public async Task<List<AuthSessionModel>> GetSessionsByDeviceInfoAsync(string deviceInfo, TimeSpan? withinTimeSpan = null)
        {
            var query = _dbSet.Where(s => s.DeviceInfo != null && s.DeviceInfo.Contains(deviceInfo));

            if (withinTimeSpan.HasValue)
            {
                var cutoffTime = DateTime.Now - withinTimeSpan.Value;
                query = query.Where(s => s.LoginTime >= cutoffTime);
            }

            return await query
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }
    }
}