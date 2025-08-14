using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Entities.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Repositories
{
    /// <summary>
    /// 登录尝试仓储实现 - 记录和分析所有登录尝试
    /// 继承BaseRepository获得通用CRUD功能，扩展登录尝试跟踪特有业务方法
    /// </summary>
    public class LoginAttemptRepository : BaseRepository<LoginAttemptModel>, ILoginAttemptRepository
    {
        /// <summary>
        /// 初始化仓储并注入统一数据库上下文
        /// </summary>
        public LoginAttemptRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 记录登录尝试
        /// </summary>
        public async Task<LoginAttemptModel> RecordAttemptAsync(LoginAttemptModel attempt)
        {
            attempt.CreateTime = DateTime.Now;
            await _dbSet.AddAsync(attempt);
            await _context.SaveChangesAsync();
            return attempt;
        }

        /// <summary>
        /// 获取用户最近的登录尝试
        /// </summary>
        public async Task<List<LoginAttemptModel>> GetRecentAttemptsByUsernameAsync(string username, TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(a => a.Username == username && a.AttemptTime >= cutoffTime)
                .OrderByDescending(a => a.AttemptTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取IP地址的最近登录尝试
        /// </summary>
        public async Task<List<LoginAttemptModel>> GetRecentAttemptsByIpAsync(string ipAddress, TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(a => a.ClientIp == ipAddress && a.AttemptTime >= cutoffTime)
                .OrderByDescending(a => a.AttemptTime)
                .ToListAsync();
        }

        /// <summary>
        /// 统计用户最近失败次数
        /// </summary>
        public async Task<int> GetFailureCountByUsernameAsync(string username, TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .CountAsync(a => a.Username == username && 
                               !a.IsSuccess && 
                               a.AttemptTime >= cutoffTime);
        }

        /// <summary>
        /// 统计IP地址最近失败次数
        /// </summary>
        public async Task<int> GetFailureCountByIpAsync(string ipAddress, TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .CountAsync(a => a.ClientIp == ipAddress && 
                               !a.IsSuccess && 
                               a.AttemptTime >= cutoffTime);
        }

        /// <summary>
        /// 获取可疑的登录尝试
        /// </summary>
        public async Task<List<LoginAttemptModel>> GetSuspiciousAttemptsAsync(TimeSpan timeSpan, SecurityLevel minRiskLevel = SecurityLevel.High)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(a => a.AttemptTime >= cutoffTime && 
                           (a.IsSuspicious || a.RiskLevel >= minRiskLevel))
                .OrderByDescending(a => a.RiskLevel)
                .ThenByDescending(a => a.AttemptTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取需要审查的登录尝试
        /// </summary>
        public async Task<List<LoginAttemptModel>> GetAttemptsRequiringReviewAsync()
        {
            return await _dbSet
                .Where(a => a.RequiresReview && !a.IsReviewed)
                .OrderByDescending(a => a.RiskLevel)
                .ThenByDescending(a => a.AttemptTime)
                .ToListAsync();
        }

        /// <summary>
        /// 标记尝试为已审查
        /// </summary>
        public async Task MarkAsReviewedAsync(Guid attemptId, Guid reviewedBy, string? notes = null)
        {
            var attempt = await _dbSet.FindAsync(attemptId);
            if (attempt != null)
            {
                attempt.IsReviewed = true;
                attempt.ReviewedBy = reviewedBy;
                attempt.ReviewTime = DateTime.Now;
                attempt.ReviewNotes = notes;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 批量标记尝试为已审查
        /// </summary>
        public async Task MarkBatchAsReviewedAsync(List<Guid> attemptIds, Guid reviewedBy, string? notes = null)
        {
            var attempts = await _dbSet
                .Where(a => attemptIds.Contains(a.Id))
                .ToListAsync();

            foreach (var attempt in attempts)
            {
                attempt.IsReviewed = true;
                attempt.ReviewedBy = reviewedBy;
                attempt.ReviewTime = DateTime.Now;
                attempt.ReviewNotes = notes;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 获取登录成功率统计
        /// </summary>
        public async Task<(int TotalAttempts, int SuccessfulAttempts, double SuccessRate)> GetLoginStatsAsync(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            var totalAttempts = await _dbSet.CountAsync(a => a.AttemptTime >= cutoffTime);
            var successfulAttempts = await _dbSet.CountAsync(a => a.AttemptTime >= cutoffTime && a.IsSuccess);
            
            var successRate = totalAttempts > 0 ? (double)successfulAttempts / totalAttempts * 100 : 0;
            return (totalAttempts, successfulAttempts, successRate);
        }

        /// <summary>
        /// 获取风险级别统计
        /// </summary>
        public async Task<Dictionary<SecurityLevel, int>> GetRiskLevelStatsAsync(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(a => a.AttemptTime >= cutoffTime)
                .GroupBy(a => a.RiskLevel)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 按时间段统计登录尝试
        /// </summary>
        public async Task<Dictionary<DateTime, int>> GetAttemptsByHourAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(a => a.AttemptTime >= startDate && a.AttemptTime <= endDate)
                .GroupBy(a => new DateTime(a.AttemptTime.Year, a.AttemptTime.Month, a.AttemptTime.Day, a.AttemptTime.Hour, 0, 0))
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取顶级攻击IP地址
        /// </summary>
        public async Task<List<(string IpAddress, int AttemptCount, int FailureCount)>> GetTopAttackingIpsAsync(TimeSpan timeSpan, int topCount = 10)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(a => a.AttemptTime >= cutoffTime && !string.IsNullOrEmpty(a.ClientIp))
                .GroupBy(a => a.ClientIp)
                .Select(g => new 
                {
                    IpAddress = g.Key,
                    AttemptCount = g.Count(),
                    FailureCount = g.Count(a => !a.IsSuccess)
                })
                .Where(x => x.FailureCount > 0)
                .OrderByDescending(x => x.FailureCount)
                .Take(topCount)
                .ToListAsync()
                .ContinueWith(task => task.Result.Select(x => (x.IpAddress!, x.AttemptCount, x.FailureCount)).ToList());
        }

        /// <summary>
        /// 获取常用登录方式统计
        /// </summary>
        public async Task<Dictionary<LoginType, int>> GetLoginTypeStatsAsync(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(a => a.AttemptTime >= cutoffTime)
                .GroupBy(a => a.LoginType)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 删除旧的登录尝试记录
        /// </summary>
        public async Task CleanupOldAttemptsAsync(TimeSpan retentionPeriod)
        {
            var cutoffTime = DateTime.Now - retentionPeriod;
            var oldAttempts = await _dbSet
                .Where(a => a.AttemptTime < cutoffTime)
                .ToListAsync();

            _dbSet.RemoveRange(oldAttempts);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 检查是否为暴力破解攻击
        /// </summary>
        public async Task<bool> IsBruteForceAttackAsync(string username, string ipAddress, TimeSpan timeWindow, int threshold)
        {
            var cutoffTime = DateTime.Now - timeWindow;
            var failureCount = await _dbSet
                .CountAsync(a => (a.Username == username || a.ClientIp == ipAddress) &&
                               !a.IsSuccess && 
                               a.AttemptTime >= cutoffTime);

            return failureCount >= threshold;
        }

        /// <summary>
        /// 获取用户登录历史趋势
        /// </summary>
        public async Task<List<LoginAttemptModel>> GetUserLoginHistoryAsync(string username, DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(a => a.Username == username && 
                           a.AttemptTime >= startDate && 
                           a.AttemptTime <= endDate)
                .OrderByDescending(a => a.AttemptTime)
                .ToListAsync();
        }
    }
}