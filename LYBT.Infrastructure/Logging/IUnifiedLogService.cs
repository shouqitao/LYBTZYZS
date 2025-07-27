using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Common.Models;
using LYBT.Common.Enums.Logs;

namespace LYBT.Infrastructure.Logging {

    /// <summary>
    /// 统一日志服务接口
    /// </summary>
    public interface IUnifiedLogService {

        // ==================== 基础日志操作 ====================

        /// <summary>
        /// 创建操作日志
        /// </summary>
        Task<bool> CreateLogAsync(LogCreateDto logCreateDto);

        /// <summary>
        /// 批量创建日志
        /// </summary>
        Task<bool> CreateLogsAsync(IEnumerable<LogCreateDto> logCreateDtos);

        /// <summary>
        /// 分页查询日志
        /// </summary>
        Task<PagedResult<LogDto>> GetLogsAsync(LogQueryDto queryDto);

        /// <summary>
        /// 根据ID获取日志详情
        /// </summary>
        Task<LogDto?> GetLogByIdAsync(Guid id);

        /// <summary>
        /// 删除过期日志
        /// </summary>
        Task<int> DeleteExpiredLogsAsync(DateTime beforeDate);

        // ==================== 系统日志 ====================

        /// <summary>
        /// 记录系统信息日志
        /// </summary>
        Task LogInfoAsync(string source, string message, Guid? userId = null, string? requestId = null);

        /// <summary>
        /// 记录系统警告日志
        /// </summary>
        Task LogWarningAsync(string source, string message, Guid? userId = null, string? requestId = null);

        /// <summary>
        /// 记录系统错误日志
        /// </summary>
        Task LogErrorAsync(string source, string message, Exception? exception = null, Guid? userId = null, string? requestId = null);

        /// <summary>
        /// 记录系统致命错误日志
        /// </summary>
        Task LogFatalAsync(string source, string message, Exception? exception = null, Guid? userId = null, string? requestId = null);

        // ==================== 用户操作日志 ====================

        /// <summary>
        /// 记录用户操作日志
        /// </summary>
        Task LogUserActionAsync(Guid userId, string userName, LogActionType actionType, 
            string module, string function, string description, 
            string? requestPath = null, string? httpMethod = null, 
            string? parameters = null, bool isSuccess = true, 
            string? errorMessage = null, string? clientIP = null, 
            string? userAgent = null, long duration = 0);

        /// <summary>
        /// 记录用户登录日志
        /// </summary>
        Task LogUserLoginAsync(Guid userId, string userName, string clientIP, string userAgent, bool isSuccess, string? errorMessage = null);

        /// <summary>
        /// 记录用户登出日志
        /// </summary>
        Task LogUserLogoutAsync(Guid userId, string userName, string clientIP);

        // ==================== 错误日志 ====================

        /// <summary>
        /// 记录错误日志
        /// </summary>
        Task LogErrorAsync(Exception exception, string? requestPath = null, string? httpMethod = null, 
            Guid? userId = null, string? clientIP = null, string? userAgent = null);

        /// <summary>
        /// 标记错误为已解决
        /// </summary>
        Task<bool> MarkErrorResolvedAsync(Guid errorLogId, string resolutionNotes);

        // ==================== 审计日志 ====================

        /// <summary>
        /// 记录审计日志
        /// </summary>
        Task LogAuditAsync(string eventType, string resourceType, string resourceId, 
            Guid? userId, string? userName, string description, 
            object? oldValues = null, object? newValues = null, 
            string? clientIP = null, string? sessionId = null, 
            string? requestId = null, string? riskLevel = null);

        // ==================== 性能日志 ====================

        /// <summary>
        /// 记录性能日志
        /// </summary>
        Task LogPerformanceAsync(string operationName, string moduleName, string methodName, 
            DateTime startTime, DateTime endTime, long duration,
            double? cpuUsage = null, long? memoryUsage = null, 
            int? databaseQueries = null, int? cacheHits = null, int? cacheMisses = null,
            int? httpStatusCode = null, long? requestSize = null, long? responseSize = null,
            Guid? userId = null, string? clientIP = null, string? requestPath = null,
            string? performanceLevel = null, object? additionalData = null);

        // ==================== 统计查询 ====================

        /// <summary>
        /// 获取日志统计信息
        /// </summary>
        Task<Dictionary<string, object>> GetLogStatisticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取用户操作统计
        /// </summary>
        Task<Dictionary<string, object>> GetUserActionStatisticsAsync(Guid userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取系统性能统计
        /// </summary>
        Task<Dictionary<string, object>> GetPerformanceStatisticsAsync(DateTime startDate, DateTime endDate);

        // ==================== 导出功能 ====================

        /// <summary>
        /// 导出日志到CSV
        /// </summary>
        Task<byte[]> ExportLogsToCsvAsync(LogQueryDto queryDto);

        /// <summary>
        /// 导出日志到Excel
        /// </summary>
        Task<byte[]> ExportLogsToExcelAsync(LogQueryDto queryDto);
    }
}