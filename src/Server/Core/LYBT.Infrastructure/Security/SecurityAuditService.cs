using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 安全审计服务 - Epic 05-P0-03: 数据安全保障
    /// 记录敏感操作的完整审计日志，满足医疗数据合规要求
    /// </summary>
    public interface ISecurityAuditService
    {
        /// <summary>
        /// 记录数据访问审计
        /// </summary>
        Task LogDataAccessAsync(DataAccessAuditEntry entry);

        /// <summary>
        /// 记录认证审计
        /// </summary>
        Task LogAuthenticationAsync(AuthenticationAuditEntry entry);

        /// <summary>
        /// 记录授权审计
        /// </summary>
        Task LogAuthorizationAsync(AuthorizationAuditEntry entry);

        /// <summary>
        /// 记录敏感操作审计
        /// </summary>
        Task LogSensitiveOperationAsync(SensitiveOperationAuditEntry entry);
    }

    /// <summary>
    /// 安全审计服务实现
    /// </summary>
    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly ILogger<SecurityAuditService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SecurityAuditService(
            ILogger<SecurityAuditService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc/>
        public async Task LogDataAccessAsync(DataAccessAuditEntry entry)
        {
            var context = GetAuditContext();
            var auditLog = new AuditLogEntry
            {
                EventType = "DataAccess",
                UserId = context.UserId,
                UserName = context.UserName,
                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,
                Timestamp = DateTime.UtcNow,
                ResourceId = entry.ResourceId,
                ResourceType = entry.ResourceType,
                Operation = entry.Operation,
                Details = JsonSerializer.Serialize(new
                {
                    entry.TableName,
                    entry.RecordId,
                    entry.FieldsAccessed,
                    entry.QueryType,
                    Success = entry.Success,
                    ErrorMessage = entry.ErrorMessage
                }),
                Severity = entry.Success ? AuditSeverity.Information : AuditSeverity.Warning
            };

            await LogAuditEntryAsync(auditLog);
        }

        /// <inheritdoc/>
        public async Task LogAuthenticationAsync(AuthenticationAuditEntry entry)
        {
            var context = GetAuditContext();
            var auditLog = new AuditLogEntry
            {
                EventType = "Authentication",
                UserId = entry.UserId,
                UserName = entry.UserName ?? context.UserName,
                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,
                Timestamp = DateTime.UtcNow,
                Operation = entry.Operation,
                Details = JsonSerializer.Serialize(new
                {
                    entry.LoginMethod,
                    entry.Success,
                    entry.FailureReason,
                    entry.SessionId,
                    RememberMe = entry.RememberMe
                }),
                Severity = entry.Success ? AuditSeverity.Information : AuditSeverity.Warning
            };

            await LogAuditEntryAsync(auditLog);
        }

        /// <inheritdoc/>
        public async Task LogAuthorizationAsync(AuthorizationAuditEntry entry)
        {
            var context = GetAuditContext();
            var auditLog = new AuditLogEntry
            {
                EventType = "Authorization",
                UserId = context.UserId,
                UserName = context.UserName,
                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,
                Timestamp = DateTime.UtcNow,
                ResourceId = entry.ResourceId,
                ResourceType = entry.ResourceType,
                Operation = entry.Operation,
                Details = JsonSerializer.Serialize(new
                {
                    entry.RequiredRole,
                    entry.UserRoles,
                    entry.AccessGranted,
                    entry.DenialReason
                }),
                Severity = entry.AccessGranted ? AuditSeverity.Information : AuditSeverity.Warning
            };

            await LogAuditEntryAsync(auditLog);
        }

        /// <inheritdoc/>
        public async Task LogSensitiveOperationAsync(SensitiveOperationAuditEntry entry)
        {
            var context = GetAuditContext();
            var auditLog = new AuditLogEntry
            {
                EventType = "SensitiveOperation",
                UserId = context.UserId,
                UserName = context.UserName,
                IpAddress = context.IpAddress,
                UserAgent = context.UserAgent,
                Timestamp = DateTime.UtcNow,
                ResourceId = entry.ResourceId,
                ResourceType = entry.ResourceType,
                Operation = entry.Operation,
                Details = JsonSerializer.Serialize(new
                {
                    entry.OperationType,
                    entry.DataCategory,
                    entry.RecordCount,
                    entry.Success,
                    entry.ErrorMessage,
                    BusinessContext = entry.BusinessContext
                }),
                Severity = GetOperationSeverity(entry.OperationType, entry.Success)
            };

            await LogAuditEntryAsync(auditLog);
        }

        private AuditContext GetAuditContext()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return new AuditContext
                {
                    UserId = "SYSTEM",
                    UserName = "SYSTEM",
                    IpAddress = "127.0.0.1",
                    UserAgent = "System Process"
                };
            }

            var user = httpContext.User;
            return new AuditContext
            {
                UserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ANONYMOUS",
                UserName = user?.FindFirst(ClaimTypes.Name)?.Value ?? "ANONYMOUS",
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString() ?? "Unknown"
            };
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            // 尝试获取真实客户端IP地址（考虑代理和负载均衡器）
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',')[0].Trim();
            }

            var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private static AuditSeverity GetOperationSeverity(string operationType, bool success)
        {
            if (!success)
                return AuditSeverity.Error;

            return operationType.ToUpper() switch
            {
                "DELETE" => AuditSeverity.Warning,
                "EXPORT" => AuditSeverity.Warning,
                "BULK_DELETE" => AuditSeverity.Critical,
                "ADMIN_OVERRIDE" => AuditSeverity.Critical,
                _ => AuditSeverity.Information
            };
        }

        private async Task LogAuditEntryAsync(AuditLogEntry entry)
        {
            try
            {
                // 结构化日志记录，便于分析和查询
                _logger.Log(
                    GetLogLevel(entry.Severity),
                    "安全审计 - {EventType}: {Operation} | 用户: {UserName}({UserId}) | 资源: {ResourceType}/{ResourceId} | IP: {IpAddress} | 详情: {Details}",
                    entry.EventType,
                    entry.Operation,
                    entry.UserName,
                    entry.UserId,
                    entry.ResourceType ?? "N/A",
                    entry.ResourceId ?? "N/A",
                    entry.IpAddress,
                    entry.Details);

                // TODO: 可扩展到专用的审计数据库或外部日志系统
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录安全审计日志失败");

                // 审计日志记录失败不应影响业务操作，但需要记录到事件日志
            }
        }

        private static LogLevel GetLogLevel(AuditSeverity severity)
        {
            return severity switch
            {
                AuditSeverity.Information => LogLevel.Information,
                AuditSeverity.Warning => LogLevel.Warning,
                AuditSeverity.Error => LogLevel.Error,
                AuditSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Information
            };
        }
    }

    #region 审计实体类

    /// <summary>
    /// 审计上下文
    /// </summary>
    public class AuditContext
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string IpAddress { get; set; } = null!;
        public string UserAgent { get; set; } = null!;
    }

    /// <summary>
    /// 审计日志条目
    /// </summary>
    public class AuditLogEntry
    {
        public string EventType { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string IpAddress { get; set; } = null!;
        public string UserAgent { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string? ResourceId { get; set; }
        public string? ResourceType { get; set; }
        public string Operation { get; set; } = null!;
        public string Details { get; set; } = null!;
        public AuditSeverity Severity { get; set; }
    }

    /// <summary>
    /// 数据访问审计条目
    /// </summary>
    public class DataAccessAuditEntry
    {
        public string? ResourceId { get; set; }
        public string? ResourceType { get; set; }
        public string Operation { get; set; } = null!;
        public string? TableName { get; set; }
        public string? RecordId { get; set; }
        public string[]? FieldsAccessed { get; set; }
        public string? QueryType { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 认证审计条目
    /// </summary>
    public class AuthenticationAuditEntry
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string Operation { get; set; } = null!;
        public string? LoginMethod { get; set; }
        public bool Success { get; set; } = true;
        public string? FailureReason { get; set; }
        public string? SessionId { get; set; }
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// 授权审计条目
    /// </summary>
    public class AuthorizationAuditEntry
    {
        public string? ResourceId { get; set; }
        public string? ResourceType { get; set; }
        public string Operation { get; set; } = null!;
        public string? RequiredRole { get; set; }
        public string[]? UserRoles { get; set; }
        public bool AccessGranted { get; set; }
        public string? DenialReason { get; set; }
    }

    /// <summary>
    /// 敏感操作审计条目
    /// </summary>
    public class SensitiveOperationAuditEntry
    {
        public string? ResourceId { get; set; }
        public string? ResourceType { get; set; }
        public string Operation { get; set; } = null!;
        public string OperationType { get; set; } = null!;
        public string? DataCategory { get; set; }
        public int RecordCount { get; set; } = 1;
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? BusinessContext { get; set; }
    }

    /// <summary>
    /// 审计严重程度
    /// </summary>
    public enum AuditSeverity
    {
        Information,
        Warning,
        Error,
        Critical
    }

    #endregion
}
