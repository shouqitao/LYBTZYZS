using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Repositories
{

    /// <summary>
    /// 认证会话仓储实现 - Record-Only模式简化版本
    /// 继承OptimizedBaseRepository获得缓存和性能优化，保留基本会话功能
    /// </summary>
    public class AuthSessionRepository : OptimizedBaseRepository<AuthSession>, IAuthSessionRepository
    {

        public AuthSessionRepository(
            AppDbContext context,
            ILogger<AuthSessionRepository> logger,
            IMemoryCache cache) : base(context, logger, cache)
        {
        }

        /// <summary>
        /// 根据用户ID获取活跃会话 - 缓存优化版
        /// </summary>
        public async Task<List<AuthSession>> GetActiveSessionsByUserIdAsync(Guid userId)
        {
            var cacheKey = $"{CacheKeyPrefix}active:user:{userId}";

            if (_cache.TryGetValue<List<AuthSession>>(cacheKey, out var cached) && cached != null)
            {
                _logger.LogDebug("从缓存获取用户活跃会话 {UserId}", userId);
                return cached;
            }

            var sessions = await _dbSet
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.Status == CommonStatus.Enabled && !s.IsRevoked)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();

            // 短缓存时间，因为会话状态变化频繁
            _cache.Set(cacheKey, sessions, TimeSpan.FromMinutes(2));
            return sessions;
        }

        /// <summary>
        /// 根据用户名获取活跃会话 - Record-Only模式简化版（功能移除）
        /// </summary>
        [Obsolete("Complex session tracking removed in Record-Only mode. Use stateless JWT instead.", false)]
        public Task<List<AuthSession>> GetActiveSessionsByUsernameAsync(string username)
        {
            // Record-Only: 复杂会话跟踪功能移除，使用无状态JWT
            return Task.FromResult(new List<AuthSession>());
        }

        /// <summary>
        /// 根据JWT令牌哈希查找会话 - 缓存优化版
        /// </summary>
        public async Task<AuthSession?> GetByTokenHashAsync(string tokenHash)
        {
            var cacheKey = $"{CacheKeyPrefix}token:{tokenHash}";

            if (_cache.TryGetValue<AuthSession?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取令牌会话 {TokenHash}", tokenHash.Substring(0, 8) + "...");
                return cached;
            }

            var session = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TokenHash == tokenHash && !s.IsRevoked);

            // 短缓存时间，因为令牌验证频繁且安全敏感
            _cache.Set(cacheKey, session, TimeSpan.FromMinutes(1));
            return session;
        }

        /// <summary>
        /// 根据刷新令牌哈希查找会话 - Record-Only模式简化版（功能移除）
        /// </summary>
        [Obsolete("Complex refresh token mechanism removed in Record-Only mode. Use stateless JWT instead.", false)]
        public Task<AuthSession?> GetByRefreshTokenHashAsync(string refreshTokenHash)
        {
            // Record-Only: 复杂刷新令牌机制移除，使用无状态JWT
            return Task.FromResult<AuthSession?>(null);
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
        /// 更新会话最后活跃时间 - Record-Only模式简化版（功能移除）
        /// </summary>
        [Obsolete("Session activity tracking removed in Record-Only mode. Use stateless JWT instead.", false)]
        public async Task UpdateLastActivityAsync(Guid sessionId, DateTime lastActivity)
        {
            // Record-Only: 会话活跃跟踪移除，使用无状态JWT
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
        /// 获取会话统计信息 - Record-Only模式简化版
        /// </summary>
        [Obsolete("Complex session statistics removed in Record-Only mode. Use simple user count instead.", false)]
        public async Task<(int TotalSessions, int ActiveSessions, int ExpiredSessions)> GetSessionStatsAsync()
        {
            var totalSessions = await _dbSet.CountAsync();
            var activeSessions = await _dbSet.CountAsync(s => s.Status == CommonStatus.Enabled && !s.IsRevoked);
            var expiredSessions = await _dbSet.CountAsync(s => s.Status == CommonStatus.Disabled || s.IsRevoked);

            return (totalSessions, activeSessions, expiredSessions);
        }

        /// <summary>
        /// 根据IP地址获取会话（安全监控）- Record-Only模式简化版
        /// </summary>
        [Obsolete("Complex IP-based session monitoring removed in Record-Only mode. Use basic audit logs instead.", false)]
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
        /// 标记会话异常 - Record-Only模式简化版
        /// </summary>
        [Obsolete("Complex session anomaly detection removed in Record-Only mode. Use basic audit logs instead.", false)]
        public async Task MarkSessionAnomalyAsync(Guid sessionId, string description)
        {
            var session = await _dbSet.FindAsync(sessionId);
            if (session != null)
            {
                // Record-Only: 简化异常处理，直接撤销会话
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
        /// 根据设备信息获取会话 - Record-Only模式简化版（功能移除）
        /// </summary>
        [Obsolete("Complex device-based session tracking removed in Record-Only mode. Use stateless JWT instead.", false)]
        public Task<List<AuthSession>> GetSessionsByDeviceInfoAsync(string deviceInfo, TimeSpan? withinTimeSpan = null)
        {
            // Record-Only: 复杂设备跟踪功能移除，使用无状态JWT
            return Task.FromResult(new List<AuthSession>());
        }
    }
}
