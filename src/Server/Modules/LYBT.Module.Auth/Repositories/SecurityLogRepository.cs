using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Entities.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Repositories
{
    /// <summary>
    /// 安全日志仓储实现 - 记录和管理系统安全事件
    /// 继承BaseRepository获得通用CRUD功能，扩展安全日志管理特有业务方法
    /// </summary>
    public class SecurityLogRepository : BaseRepository<SecurityLogModel>, ISecurityLogRepository
    {
        /// <summary>
        /// 初始化仓储并注入统一数据库上下文
        /// </summary>
        public SecurityLogRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 记录安全事件
        /// </summary>
        public async Task<SecurityLogModel> LogSecurityEventAsync(SecurityLogModel logEntry)
        {
            logEntry.CreateTime = DateTime.Now;
            logEntry.EventTime = logEntry.EventTime == default ? DateTime.Now : logEntry.EventTime;
            
            await _dbSet.AddAsync(logEntry);
            await _context.SaveChangesAsync();
            return logEntry;
        }

        /// <summary>
        /// 获取未处理的安全日志
        /// </summary>
        public async Task<List<SecurityLogModel>> GetUnprocessedLogsAsync()
        {
            return await _dbSet
                .Where(l => !l.IsProcessed)
                .OrderByDescending(l => l.Level)
                .ThenByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取需要通知的安全日志
        /// </summary>
        public async Task<List<SecurityLogModel>> GetLogsRequiringNotificationAsync()
        {
            return await _dbSet
                .Where(l => l.RequiresNotification && !l.IsNotified)
                .OrderByDescending(l => l.Level)
                .ThenByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 标记日志为已处理
        /// </summary>
        public async Task MarkAsProcessedAsync(Guid logId, Guid processedBy, string? notes = null)
        {
            var log = await _dbSet.FindAsync(logId);
            if (log != null)
            {
                log.IsProcessed = true;
                log.ProcessedBy = processedBy;
                log.ProcessedTime = DateTime.Now;
                log.ProcessingNotes = notes;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 批量标记日志为已处理
        /// </summary>
        public async Task MarkBatchAsProcessedAsync(List<Guid> logIds, Guid processedBy, string? notes = null)
        {
            var logs = await _dbSet
                .Where(l => logIds.Contains(l.Id))
                .ToListAsync();

            foreach (var log in logs)
            {
                log.IsProcessed = true;
                log.ProcessedBy = processedBy;
                log.ProcessedTime = DateTime.Now;
                log.ProcessingNotes = notes;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 标记日志为已通知
        /// </summary>
        public async Task MarkAsNotifiedAsync(Guid logId, string notificationMethod)
        {
            var log = await _dbSet.FindAsync(logId);
            if (log != null)
            {
                log.IsNotified = true;
                log.NotifiedTime = DateTime.Now;
                log.NotificationMethod = notificationMethod;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 根据事件类型获取日志
        /// </summary>
        public async Task<List<SecurityLogModel>> GetLogsByEventTypeAsync(AuthEventType eventType, TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(l => l.EventType == eventType && l.EventTime >= cutoffTime)
                .OrderByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据安全级别获取日志
        /// </summary>
        public async Task<List<SecurityLogModel>> GetLogsBySecurityLevelAsync(SecurityLevel level, TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(l => l.Level >= level && l.EventTime >= cutoffTime)
                .OrderByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据用户获取安全日志
        /// </summary>
        public async Task<List<SecurityLogModel>> GetLogsByUserAsync(Guid userId, TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(l => l.UserId == userId && l.EventTime >= cutoffTime)
                .OrderByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据IP地址获取安全日志
        /// </summary>
        public async Task<List<SecurityLogModel>> GetLogsByIpAddressAsync(string ipAddress, TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(l => l.ClientIp == ipAddress && l.EventTime >= cutoffTime)
                .OrderByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取高风险安全事件
        /// </summary>
        public async Task<List<SecurityLogModel>> GetHighRiskEventsAsync(TimeSpan timeSpan, int minRiskScore = 70)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(l => l.EventTime >= cutoffTime && 
                           (l.RiskScore >= minRiskScore || l.Level >= SecurityLevel.High))
                .OrderByDescending(l => l.RiskScore)
                .ThenByDescending(l => l.Level)
                .ThenByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取需要升级的安全事件
        /// </summary>
        public async Task<List<SecurityLogModel>> GetEventsRequiringEscalationAsync()
        {
            return await _dbSet
                .Where(l => l.RequiresEscalation && !l.IsProcessed)
                .OrderByDescending(l => l.EscalationLevel)
                .ThenByDescending(l => l.Level)
                .ThenByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取安全事件统计
        /// </summary>
        public async Task<Dictionary<AuthEventType, int>> GetEventTypeStatsAsync(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(l => l.EventTime >= cutoffTime)
                .GroupBy(l => l.EventType)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取安全级别统计
        /// </summary>
        public async Task<Dictionary<SecurityLevel, int>> GetSecurityLevelStatsAsync(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(l => l.EventTime >= cutoffTime)
                .GroupBy(l => l.Level)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 按时间段统计安全事件
        /// </summary>
        public async Task<Dictionary<DateTime, int>> GetEventsByHourAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(l => l.EventTime >= startDate && l.EventTime <= endDate)
                .GroupBy(l => new DateTime(l.EventTime.Year, l.EventTime.Month, l.EventTime.Day, l.EventTime.Hour, 0, 0))
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取最活跃的用户（安全事件角度）
        /// </summary>
        public async Task<List<(Guid UserId, string Username, int EventCount)>> GetMostActiveUsersAsync(TimeSpan timeSpan, int topCount = 10)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(l => l.EventTime >= cutoffTime && l.UserId.HasValue && !string.IsNullOrEmpty(l.Username))
                .GroupBy(l => new { l.UserId, l.Username })
                .Select(g => new 
                {
                    UserId = g.Key.UserId!.Value,
                    Username = g.Key.Username!,
                    EventCount = g.Count()
                })
                .OrderByDescending(x => x.EventCount)
                .Take(topCount)
                .ToListAsync()
                .ContinueWith(task => task.Result.Select(x => (x.UserId, x.Username, x.EventCount)).ToList());
        }

        /// <summary>
        /// 获取最活跃的IP地址（安全事件角度）
        /// </summary>
        public async Task<List<(string IpAddress, int EventCount, int HighRiskCount)>> GetMostActiveIpsAsync(TimeSpan timeSpan, int topCount = 10)
        {
            var cutoffTime = DateTime.Now - timeSpan;
            return await _dbSet
                .Where(l => l.EventTime >= cutoffTime && !string.IsNullOrEmpty(l.ClientIp))
                .GroupBy(l => l.ClientIp)
                .Select(g => new 
                {
                    IpAddress = g.Key,
                    EventCount = g.Count(),
                    HighRiskCount = g.Count(l => l.Level >= SecurityLevel.High || l.RiskScore >= 70)
                })
                .OrderByDescending(x => x.HighRiskCount)
                .ThenByDescending(x => x.EventCount)
                .Take(topCount)
                .ToListAsync()
                .ContinueWith(task => task.Result.Select(x => (x.IpAddress!, x.EventCount, x.HighRiskCount)).ToList());
        }

        /// <summary>
        /// 检查是否存在异常模式
        /// </summary>
        public async Task<bool> HasAnomalousPatternAsync(string pattern, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.Now - timeWindow;
            return await _dbSet
                .AnyAsync(l => l.EventTime >= cutoffTime && 
                              (l.Description.Contains(pattern) || 
                               (l.Details != null && l.Details.Contains(pattern))));
        }

        /// <summary>
        /// 获取相关安全事件
        /// </summary>
        public async Task<List<SecurityLogModel>> GetRelatedEventsAsync(Guid logId, TimeSpan correlationWindow)
        {
            var targetLog = await _dbSet.FindAsync(logId);
            if (targetLog == null) return new List<SecurityLogModel>();

            var startTime = targetLog.EventTime - correlationWindow;
            var endTime = targetLog.EventTime + correlationWindow;

            return await _dbSet
                .Where(l => l.Id != logId && 
                           l.EventTime >= startTime && 
                           l.EventTime <= endTime &&
                           (l.UserId == targetLog.UserId || 
                            l.ClientIp == targetLog.ClientIp ||
                            l.SessionId == targetLog.SessionId))
                .OrderByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 删除已归档的日志
        /// </summary>
        public async Task CleanupArchivedLogsAsync()
        {
            var archivedLogs = await _dbSet
                .Where(l => l.IsArchived && 
                           l.RetentionExpiry.HasValue && 
                           l.RetentionExpiry.Value < DateTime.Now)
                .ToListAsync();

            _dbSet.RemoveRange(archivedLogs);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 归档旧日志
        /// </summary>
        public async Task ArchiveOldLogsAsync(TimeSpan archiveAfter)
        {
            var cutoffTime = DateTime.Now - archiveAfter;
            var oldLogs = await _dbSet
                .Where(l => !l.IsArchived && 
                           l.EventTime < cutoffTime && 
                           l.IsProcessed)
                .ToListAsync();

            foreach (var log in oldLogs)
            {
                log.IsArchived = true;
                log.ArchivedTime = DateTime.Now;
                // 设置保留期限（例如归档后再保留1年）
                log.RetentionExpiry = DateTime.Now.AddYears(1);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 更新风险评估分数
        /// </summary>
        public async Task UpdateRiskScoreAsync(Guid logId, int riskScore, string? analysisResult = null)
        {
            var log = await _dbSet.FindAsync(logId);
            if (log != null)
            {
                log.RiskScore = riskScore;
                if (!string.IsNullOrEmpty(analysisResult))
                    log.AutoAnalysisResult = analysisResult;
                
                // 根据风险分数自动设置是否需要升级
                if (riskScore >= 80)
                {
                    log.RequiresEscalation = true;
                    log.EscalationLevel = riskScore >= 90 ? 3 : 2;
                }

                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 获取合规性报告数据
        /// </summary>
        public async Task<List<SecurityLogModel>> GetComplianceReportDataAsync(DateTime startDate, DateTime endDate, string? complianceType = null)
        {
            var query = _dbSet
                .Where(l => l.EventTime >= startDate && l.EventTime <= endDate);

            if (!string.IsNullOrEmpty(complianceType))
            {
                query = query.Where(l => l.ComplianceFlags != null && l.ComplianceFlags.Contains(complianceType));
            }

            return await query
                .OrderByDescending(l => l.EventTime)
                .ToListAsync();
        }

        /// <summary>
        /// 搜索安全日志
        /// </summary>
        public async Task<List<SecurityLogModel>> SearchLogsAsync(string searchTerm, SecurityLevel? minLevel = null, TimeSpan? timeSpan = null)
        {
            var query = _dbSet.AsQueryable();

            if (timeSpan.HasValue)
            {
                var cutoffTime = DateTime.Now - timeSpan.Value;
                query = query.Where(l => l.EventTime >= cutoffTime);
            }

            if (minLevel.HasValue)
            {
                query = query.Where(l => l.Level >= minLevel.Value);
            }

            query = query.Where(l => l.Description.Contains(searchTerm) ||
                                    (l.Details != null && l.Details.Contains(searchTerm)) ||
                                    (l.Username != null && l.Username.Contains(searchTerm)) ||
                                    (l.AffectedResource != null && l.AffectedResource.Contains(searchTerm)));

            return await query
                .OrderByDescending(l => l.EventTime)
                .ToListAsync();
        }
    }
}