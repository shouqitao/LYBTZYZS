namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 安全审计服务接口
    /// </summary>
    public interface ISecurityAuditService
    {
        /// <summary>
        /// 记录登录尝试事件
        /// </summary>
        Task LogLoginAttemptAsync(LoginAuditEvent loginEvent);

        /// <summary>
        /// 记录API访问事件
        /// </summary>
        Task LogApiAccessAsync(ApiAccessAuditEvent accessEvent);

        /// <summary>
        /// 记录数据访问事件
        /// </summary>
        Task LogDataAccessAsync(DataAccessAuditEvent dataEvent);

        /// <summary>
        /// 记录安全异常事件
        /// </summary>
        Task LogSecurityExceptionAsync(SecurityExceptionAuditEvent exceptionEvent);

        /// <summary>
        /// 获取用户活动报告
        /// </summary>
        Task<UserActivityReport> GetUserActivityReportAsync(Guid userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取安全警报
        /// </summary>
        Task<IEnumerable<SecurityAlert>> GetSecurityAlertsAsync(int hours = 24);
    }

    /// <summary>
    /// 登录审计事件
    /// </summary>
    public class LoginAuditEvent
    {
        public Guid? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ClientIP { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }
        public string LoginMethod { get; set; } = "Password"; // Password, MFA, SSO
        public bool RememberMe { get; set; }
        public string? SessionId { get; set; }
    }

    /// <summary>
    /// API访问审计事件
    /// </summary>
    public class ApiAccessAuditEvent
    {
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string ClientIP { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public long ResponseTimeMs { get; set; }
        public string? RequestId { get; set; }
        public string? SessionId { get; set; }
    }

    /// <summary>
    /// 数据访问审计事件
    /// </summary>
    public class DataAccessAuditEvent
    {
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string ClientIP { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty; // SELECT, INSERT, UPDATE, DELETE
        public string? RecordId { get; set; }
        public List<string>? AffectedColumns { get; set; }
        public object? OldValues { get; set; }
        public object? NewValues { get; set; }
        public bool IsSuccess { get; set; }
    }

    /// <summary>
    /// 安全异常审计事件
    /// </summary>
    public class SecurityExceptionAuditEvent
    {
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string ClientIP { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string ExceptionMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? RequestPath { get; set; }
        public ThreatLevel ThreatLevel { get; set; }
        public string? SessionId { get; set; }
    }

    /// <summary>
    /// 威胁级别
    /// </summary>
    public enum ThreatLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// 安全事件类型
    /// </summary>
    public enum SecurityEventType
    {
        LoginAttempt = 1,
        ApiAccess = 2,
        DataAccess = 3,
        SecurityException = 4,
        PermissionDenied = 5,
        ConfigurationChange = 6
    }

    /// <summary>
    /// 用户活动报告
    /// </summary>
    public class UserActivityReport
    {
        public Guid UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalEvents { get; set; }
        public int LoginAttempts { get; set; }
        public int SuccessfulLogins { get; set; }
        public int FailedLogins { get; set; }
        public int ApiAccesses { get; set; }
        public int DataAccesses { get; set; }
        public int SecurityExceptions { get; set; }
        public int UniqueIPs { get; set; }
        public DateTime? LastActivity { get; set; }
        public List<AuditLogSummary> Activities { get; set; } = new();
    }

    /// <summary>
    /// 审计日志摘要
    /// </summary>
    public class AuditLogSummary
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string ClientIP { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 安全警报
    /// </summary>
    public class SecurityAlert
    {
        public Guid Id { get; set; }
        public SecurityAlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public object? Data { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedBy { get; set; }
    }

    /// <summary>
    /// 安全警报类型
    /// </summary>
    public enum SecurityAlertType
    {
        BruteForceAttack = 1,
        AbnormalTraffic = 2,
        HighRiskOperation = 3,
        SuspiciousLogin = 4,
        DataBreach = 5,
        SystemIntrusion = 6
    }

    /// <summary>
    /// 警报严重程度
    /// </summary>
    public enum AlertSeverity
    {
        Info = 1,
        Low = 2,
        Medium = 3,
        High = 4,
        Critical = 5
    }
}