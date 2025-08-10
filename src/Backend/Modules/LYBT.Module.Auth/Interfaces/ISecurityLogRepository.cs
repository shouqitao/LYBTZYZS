using LYBT.Infrastructure.Interfaces;
using LYBT.Models.Auth;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 安全日志仓储接口 - 记录和管理系统安全事件
    /// 继承BaseRepository提供通用CRUD，扩展安全日志管理特定业务方法
    /// </summary>
    public interface ISecurityLogRepository : IBaseRepository<SecurityLogModel>
    {
        /// <summary>
        /// 记录安全事件
        /// </summary>
        Task<SecurityLogModel> LogSecurityEventAsync(SecurityLogModel logEntry);

        /// <summary>
        /// 获取未处理的安全日志
        /// </summary>
        Task<List<SecurityLogModel>> GetUnprocessedLogsAsync();

        /// <summary>
        /// 获取需要通知的安全日志
        /// </summary>
        Task<List<SecurityLogModel>> GetLogsRequiringNotificationAsync();

        /// <summary>
        /// 标记日志为已处理
        /// </summary>
        Task MarkAsProcessedAsync(Guid logId, Guid processedBy, string? notes = null);

        /// <summary>
        /// 批量标记日志为已处理
        /// </summary>
        Task MarkBatchAsProcessedAsync(List<Guid> logIds, Guid processedBy, string? notes = null);

        /// <summary>
        /// 标记日志为已通知
        /// </summary>
        Task MarkAsNotifiedAsync(Guid logId, string notificationMethod);

        /// <summary>
        /// 根据事件类型获取日志
        /// </summary>
        Task<List<SecurityLogModel>> GetLogsByEventTypeAsync(AuthEventType eventType, TimeSpan timeSpan);

        /// <summary>
        /// 根据安全级别获取日志
        /// </summary>
        Task<List<SecurityLogModel>> GetLogsBySecurityLevelAsync(SecurityLevel level, TimeSpan timeSpan);

        /// <summary>
        /// 根据用户获取安全日志
        /// </summary>
        Task<List<SecurityLogModel>> GetLogsByUserAsync(Guid userId, TimeSpan timeSpan);

        /// <summary>
        /// 根据IP地址获取安全日志
        /// </summary>
        Task<List<SecurityLogModel>> GetLogsByIpAddressAsync(string ipAddress, TimeSpan timeSpan);

        /// <summary>
        /// 获取高风险安全事件
        /// </summary>
        Task<List<SecurityLogModel>> GetHighRiskEventsAsync(TimeSpan timeSpan, int minRiskScore = 70);

        /// <summary>
        /// 获取需要升级的安全事件
        /// </summary>
        Task<List<SecurityLogModel>> GetEventsRequiringEscalationAsync();

        /// <summary>
        /// 获取安全事件统计
        /// </summary>
        Task<Dictionary<AuthEventType, int>> GetEventTypeStatsAsync(TimeSpan timeSpan);

        /// <summary>
        /// 获取安全级别统计
        /// </summary>
        Task<Dictionary<SecurityLevel, int>> GetSecurityLevelStatsAsync(TimeSpan timeSpan);

        /// <summary>
        /// 按时间段统计安全事件
        /// </summary>
        Task<Dictionary<DateTime, int>> GetEventsByHourAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取最活跃的用户（安全事件角度）
        /// </summary>
        Task<List<(Guid UserId, string Username, int EventCount)>> GetMostActiveUsersAsync(TimeSpan timeSpan, int topCount = 10);

        /// <summary>
        /// 获取最活跃的IP地址（安全事件角度）
        /// </summary>
        Task<List<(string IpAddress, int EventCount, int HighRiskCount)>> GetMostActiveIpsAsync(TimeSpan timeSpan, int topCount = 10);

        /// <summary>
        /// 检查是否存在异常模式
        /// </summary>
        Task<bool> HasAnomalousPatternAsync(string pattern, TimeSpan timeWindow);

        /// <summary>
        /// 获取相关安全事件
        /// </summary>
        Task<List<SecurityLogModel>> GetRelatedEventsAsync(Guid logId, TimeSpan correlationWindow);

        /// <summary>
        /// 删除已归档的日志
        /// </summary>
        Task CleanupArchivedLogsAsync();

        /// <summary>
        /// 归档旧日志
        /// </summary>
        Task ArchiveOldLogsAsync(TimeSpan archiveAfter);

        /// <summary>
        /// 更新风险评估分数
        /// </summary>
        Task UpdateRiskScoreAsync(Guid logId, int riskScore, string? analysisResult = null);

        /// <summary>
        /// 获取合规性报告数据
        /// </summary>
        Task<List<SecurityLogModel>> GetComplianceReportDataAsync(DateTime startDate, DateTime endDate, string? complianceType = null);

        /// <summary>
        /// 搜索安全日志
        /// </summary>
        Task<List<SecurityLogModel>> SearchLogsAsync(string searchTerm, SecurityLevel? minLevel = null, TimeSpan? timeSpan = null);
    }
}