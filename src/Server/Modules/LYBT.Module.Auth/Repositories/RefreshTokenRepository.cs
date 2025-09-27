using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Repositories
{
    /// <summary>
    /// RefreshToken仓储实现
    /// 管理刷新令牌的存储和查询
    /// </summary>
    public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
    {
        private readonly ILogger<RefreshTokenRepository> _logger;

        public RefreshTokenRepository(
            AppDbContext context,
            ILogger<RefreshTokenRepository> logger) : base(context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据令牌值查找RefreshToken
        /// </summary>
        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            try
            {
                return await _dbSet
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查找RefreshToken失败: {Token}", token);
                throw;
            }
        }

        /// <summary>
        /// 根据JwtId查找RefreshToken
        /// </summary>
        public async Task<RefreshToken?> GetByJwtIdAsync(string jwtId)
        {
            try
            {
                return await _dbSet
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.JwtId == jwtId && !rt.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据JwtId查找RefreshToken失败: {JwtId}", jwtId);
                throw;
            }
        }

        /// <summary>
        /// 获取用户的所有有效RefreshToken
        /// </summary>
        public async Task<List<RefreshToken>> GetActiveTokensByUserAsync(Guid userId)
        {
            try
            {
                return await _dbSet
                    .Where(rt => rt.UserId == userId &&
                           !rt.IsDeleted &&
                           !rt.IsUsed &&
                           !rt.IsRevoked &&
                           rt.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(rt => rt.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户RefreshTokens失败: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 撤销用户的所有RefreshToken
        /// </summary>
        public async Task RevokeAllUserTokensAsync(Guid userId)
        {
            try
            {
                var tokens = await _dbSet
                    .Where(rt => rt.UserId == userId &&
                           !rt.IsDeleted &&
                           !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    token.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("撤销用户 {UserId} 的 {Count} 个RefreshTokens", userId, tokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销用户RefreshTokens失败: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 撤销指定设备的所有RefreshToken
        /// </summary>
        public async Task RevokeDeviceTokensAsync(Guid userId, string deviceId)
        {
            try
            {
                var tokens = await _dbSet
                    .Where(rt => rt.UserId == userId &&
                           rt.DeviceId == deviceId &&
                           !rt.IsDeleted &&
                           !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    token.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("撤销设备 {DeviceId} 的 {Count} 个RefreshTokens", deviceId, tokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤销设备RefreshTokens失败: {DeviceId}", deviceId);
                throw;
            }
        }

        /// <summary>
        /// 清理过期的RefreshToken
        /// </summary>
        public async Task<int> CleanupExpiredTokensAsync()
        {
            try
            {
                var expiredTokens = await _dbSet
                    .Where(rt => rt.ExpiresAt < DateTime.UtcNow ||
                           (rt.IsUsed && rt.UsedAt < DateTime.UtcNow.AddDays(-7)))
                    .ToListAsync();

                if (expiredTokens.Any())
                {
                    _dbSet.RemoveRange(expiredTokens);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("清理了 {Count} 个过期的RefreshTokens", expiredTokens.Count);
                }

                return expiredTokens.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期RefreshTokens失败");
                throw;
            }
        }

        /// <summary>
        /// 标记RefreshToken为已使用
        /// </summary>
        public async Task<bool> MarkAsUsedAsync(string token)
        {
            try
            {
                var refreshToken = await GetByTokenAsync(token);
                if (refreshToken == null)
                {
                    _logger.LogWarning("未找到RefreshToken: {Token}", token);
                    return false;
                }

                if (refreshToken.IsUsed)
                {
                    _logger.LogWarning("RefreshToken已被使用: {Token}", token);
                    return false;
                }

                refreshToken.IsUsed = true;
                refreshToken.UsedAt = DateTime.UtcNow;
                refreshToken.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("RefreshToken已标记为已使用: {Token}", token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记RefreshToken为已使用失败: {Token}", token);
                throw;
            }
        }

        /// <summary>
        /// 获取用户的设备列表
        /// </summary>
        public async Task<List<DeviceInfo>> GetUserDevicesAsync(Guid userId)
        {
            try
            {
                var devices = await _dbSet
                    .Where(rt => rt.UserId == userId &&
                           !rt.IsDeleted &&
                           !rt.IsRevoked &&
                           rt.ExpiresAt > DateTime.UtcNow)
                    .GroupBy(rt => new { rt.DeviceId, rt.DeviceName, rt.UserAgent })
                    .Select(g => new DeviceInfo
                    {
                        DeviceId = g.Key.DeviceId,
                        DeviceName = g.Key.DeviceName ?? "未知设备",
                        UserAgent = g.Key.UserAgent,
                        LastActiveAt = g.Max(rt => rt.CreatedAt),
                        TokenCount = g.Count()
                    })
                    .ToListAsync();

                return devices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户设备列表失败: {UserId}", userId);
                throw;
            }
        }
    }

    /// <summary>
    /// 设备信息DTO
    /// </summary>
    public class DeviceInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public DateTime LastActiveAt { get; set; }
        public int TokenCount { get; set; }
    }
}