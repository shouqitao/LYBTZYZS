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
    public class AuthSessionRepository : BaseRepository<AuthSession>, IAuthSessionRepository
    {
        /// <summary>
        /// 初始化仓储并注入统一数据库上下文
        /// </summary>
        public AuthSessionRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据用户ID获取活跃会话 - UltraThink v2.0简化版
        /// </summary>
        public async Task<List<AuthSession>> GetActiveSessionsByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId && s.Status == CommonStatus.Enabled && !s.IsRevoked)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据用户名获取活跃会话 - UltraThink v2.0简化版（功能移除）
        /// </summary>
        public async Task<List<AuthSession>> GetActiveSessionsByUsernameAsync(string username)
        {
            // UltraThink v2.0简化版：AuthSession实体中没有Username字段
            // 功能已简化，返回空列表
            return new List<AuthSession>();
        }

        /// <summary>
        /// 根据JWT令牌哈希查找会话 - UltraThink v2.0简化版
        /// </summary>
        public async Task<AuthSession?> GetByTokenHashAsync(string tokenHash)
        {
            return await _dbSet
                .FirstOrDefaultAsync(s => s.TokenHash == tokenHash && !s.IsRevoked);
        }

        /// <summary>
        /// 根据刷新令牌哈希查找会话 - UltraThink v2.0简化版（功能移除）
        /// </summary>
        public async Task<AuthSession?> GetByRefreshTokenHashAsync(string refreshTokenHash)
        {
            // UltraThink v2.0简化版：AuthSession实体中没有RefreshTokenHash字段
            // 刷新令牌功能已简化，返回null
            return null;
        }

        /// <summary>
        /// 撤销用户的所有活跃会话 - UltraThink v2.0简化版
        /// </summary>
        public async Task RevokeAllUserSessionsAsync(Guid userId, string reason, Guid? revokedBy = null)
        {
            var activeSessions = await _dbSet
                .Where(s => s.UserId == userId && s.Status == CommonStatus.Enabled && !s.IsRevoked)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.Status = CommonStatus.Disabled;
                session.IsRevoked = true;
                session.LogoutTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 撤销特定会话 - UltraThink v2.0简化版
        /// </summary>
        public async Task RevokeSessionAsync(Guid sessionId, string reason, Guid? revokedBy = null)
        {
            var session = await _dbSet.FindAsync(sessionId);
            if (session != null && session.Status == CommonStatus.Enabled && !session.IsRevoked)
            {
                session.Status = CommonStatus.Disabled;
                session.IsRevoked = true;
                session.LogoutTime = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 更新会话最后活跃时间 - UltraThink v2.0简化版（功能移除）
        /// </summary>
        public async Task UpdateLastActivityAsync(Guid sessionId, DateTime lastActivity)
        {
            // UltraThink v2.0简化版：AuthSession实体中没有LastActivityTime字段
            // 功能已简化，无需更新活跃时间
            await Task.CompletedTask;
        }

        /// <summary>
        /// 清理过期会话 - UltraThink v2.0简化版
        /// </summary>
        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _dbSet
                .Where(s => s.Status == CommonStatus.Enabled && 
                           !s.IsRevoked && 
                           s.ExpiryTime < DateTime.Now)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.Status = CommonStatus.Disabled;
                session.IsRevoked = true;
                session.LogoutTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 获取会话统计信息 - UltraThink v2.0简化版
        /// </summary>
        public async Task<(int TotalSessions, int ActiveSessions, int ExpiredSessions)> GetSessionStatsAsync()
        {
            var totalSessions = await _dbSet.CountAsync();
            var activeSessions = await _dbSet.CountAsync(s => s.Status == CommonStatus.Enabled && !s.IsRevoked);
            var expiredSessions = await _dbSet.CountAsync(s => s.Status == CommonStatus.Disabled || s.IsRevoked);

            return (totalSessions, activeSessions, expiredSessions);
        }

        /// <summary>
        /// 根据IP地址获取会话（安全监控）- UltraThink v2.0简化版
        /// </summary>
        public async Task<List<AuthSession>> GetSessionsByIpAddressAsync(string ipAddress, TimeSpan? withinTimeSpan = null)
        {
            var query = _dbSet.Where(s => s.IpAddress == ipAddress);

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
        /// 标记会话异常 - UltraThink v2.0简化版
        /// </summary>
        public async Task MarkSessionAnomalyAsync(Guid sessionId, string description)
        {
            var session = await _dbSet.FindAsync(sessionId);
            if (session != null)
            {
                // UltraThink v2.0简化版：直接撤销异常会话
                session.IsRevoked = true;
                session.Status = CommonStatus.Disabled;
                session.LogoutTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 批量更新会话状态 - UltraThink v2.0简化版
        /// </summary>
        public async Task UpdateSessionStatusBatchAsync(List<Guid> sessionIds, CommonStatus status, string? reason = null)
        {
            var sessions = await _dbSet
                .Where(s => sessionIds.Contains(s.Id))
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.Status = status;
                
                if (status == CommonStatus.Disabled)
                {
                    session.IsRevoked = true;
                    session.LogoutTime = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 根据设备信息获取会话 - UltraThink v2.0简化版（功能移除）
        /// </summary>
        public async Task<List<AuthSession>> GetSessionsByDeviceInfoAsync(string deviceInfo, TimeSpan? withinTimeSpan = null)
        {
            // UltraThink v2.0简化版：AuthSession实体中没有DeviceInfo字段
            // 设备信息功能已简化，返回空列表
            return new List<AuthSession>();
        }
    }
}