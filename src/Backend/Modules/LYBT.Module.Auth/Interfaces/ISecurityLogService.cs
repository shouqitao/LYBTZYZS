using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 安全日志服务接口 - 统一记录和管理系统安全事件
    /// 提供安全事件记录、分析、报告和响应功能
    /// </summary>
    public interface ISecurityLogService
    {
        /// <summary>
        /// 记录安全事件
        /// </summary>
        Task<BaseSecurityLog> LogSecurityEventAsync(AuthEventType eventType, string description,
                                                   Guid? userId = null, string? username = null,
                                                   string? clientIp = null, string? userAgent = null,
                                                   SecurityLevel level = SecurityLevel.Low,
                                                   string? affectedResource = null,
                                                   OperationResult result = OperationResult.Success,
                                                   string? details = null, Guid? sessionId = null);

        /// <summary>
        /// 记录登录成功事件
        /// </summary>
        Task LogLoginSuccessAsync(Guid userId, string username, string? clientIp, string? userAgent, 
                                 LoginType loginType, Guid? sessionId = null);

        /// <summary>
        /// 记录登录失败事件
        /// </summary>
        Task LogLoginFailureAsync(string username, string failureReason, string? clientIp, 
                                 string? userAgent, LoginType loginType, SecurityLevel riskLevel);

        /// <summary>
        /// 记录权限被拒绝事件
        /// </summary>
        Task LogPermissionDeniedAsync(Guid? userId, string? username, string resource, 
                                     string action, string? clientIp = null);

        /// <summary>
        /// 记录数据访问事件
        /// </summary>
        Task LogDataAccessAsync(Guid userId, string username, string resourceType, 
                               string resourceId, string operation, bool isSuccess = true);

        /// <summary>
        /// 记录可疑活动事件
        /// </summary>
        Task LogSuspiciousActivityAsync(string description, string? username = null, 
                                       string? clientIp = null, SecurityLevel level = SecurityLevel.High,
                                       string? evidence = null);

        /// <summary>
        /// 记录系统错误事件
        /// </summary>
        Task LogSystemErrorAsync(string errorMessage, string? stackTrace = null, 
                                string? requestPath = null, Guid? userId = null);

        /// <summary>
        /// 记录密码变更事件
        /// </summary>
        Task LogPasswordChangeAsync(Guid userId, string username, bool isSuccess, 
                                   string? clientIp = null, bool isReset = false);

        /// <summary>
        /// 记录账户锁定事件
        /// </summary>
        Task LogAccountLockAsync(Guid userId, string username, string reason, 
                                TimeSpan lockDuration, string? clientIp = null);

        /// <summary>
        /// 获取未处理的安全日志
        /// </summary>
        Task<List<BaseSecurityLog>> GetUnprocessedLogsAsync();

        /// <summary>
        /// 获取需要通知的安全日志
        /// </summary>
        Task<List<BaseSecurityLog>> GetLogsRequiringNotificationAsync();

        /// <summary>
        /// 标记日志为已处理
        /// </summary>
        Task MarkLogAsProcessedAsync(Guid logId, Guid processedBy, string? notes = null);

        /// <summary>
        /// 批量处理安全日志
        /// </summary>
        Task BatchProcessLogsAsync(List<Guid> logIds, Guid processedBy, string? notes = null);

        /// <summary>
        /// 标记日志为已通知
        /// </summary>
        Task MarkLogAsNotifiedAsync(Guid logId, string notificationMethod);

        /// <summary>
        /// 根据事件类型获取日志
        /// </summary>
        Task<List<BaseSecurityLog>> GetLogsByEventTypeAsync(AuthEventType eventType, TimeSpan timeSpan);

        /// <summary>
        /// 根据安全级别获取日志
        /// </summary>
        Task<List<BaseSecurityLog>> GetLogsBySecurityLevelAsync(SecurityLevel level, TimeSpan timeSpan);

        /// <summary>
        /// 获取用户的安全日志
        /// </summary>
        Task<List<BaseSecurityLog>> GetUserSecurityLogsAsync(Guid userId, TimeSpan timeSpan);

        /// <summary>
        /// 获取IP地址的安全日志
        /// </summary>
        Task<List<BaseSecurityLog>> GetIpSecurityLogsAsync(string ipAddress, TimeSpan timeSpan);

        /// <summary>
        /// 获取高风险安全事件
        /// </summary>
        Task<List<BaseSecurityLog>> GetHighRiskEventsAsync(TimeSpan timeSpan, int minRiskScore = 70);

        /// <summary>
        /// 获取需要升级的安全事件
        /// </summary>
        Task<List<BaseSecurityLog>> GetEventsRequiringEscalationAsync();

        /// <summary>
        /// 获取安全事件统计
        /// </summary>
        Task<Dictionary<AuthEventType, int>> GetEventStatisticsAsync(TimeSpan timeSpan);

        /// <summary>
        /// 获取安全趋势分析
        /// </summary>
        Task<Dictionary<DateTime, int>> GetSecurityTrendsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 检测安全异常模式
        /// </summary>
        Task<List<string>> DetectSecurityAnomaliesAsync(TimeSpan analysisWindow);

        /// <summary>
        /// 生成安全事件报告
        /// </summary>
        Task<string> GenerateSecurityReportAsync(DateTime startDate, DateTime endDate, 
                                                SecurityLevel? minLevel = null);

        /// <summary>
        /// 搜索安全日志
        /// </summary>
        Task<List<BaseSecurityLog>> SearchSecurityLogsAsync(string searchTerm, 
                                                            SecurityLevel? minLevel = null, 
                                                            TimeSpan? timeSpan = null);

        /// <summary>
        /// 获取相关安全事件
        /// </summary>
        Task<List<BaseSecurityLog>> GetRelatedEventsAsync(Guid logId, TimeSpan correlationWindow);

        /// <summary>
        /// 更新事件风险评分
        /// </summary>
        Task UpdateEventRiskScoreAsync(Guid logId, int riskScore, string? analysisResult = null);

        /// <summary>
        /// 清理和归档旧日志
        /// </summary>
        Task CleanupAndArchiveLogsAsync(TimeSpan archiveAfter);

        /// <summary>
        /// 获取合规性审计数据
        /// </summary>
        Task<List<BaseSecurityLog>> GetComplianceAuditDataAsync(DateTime startDate, DateTime endDate, 
                                                               string? complianceType = null);
    }
}