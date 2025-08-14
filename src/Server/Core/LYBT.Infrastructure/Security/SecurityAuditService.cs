using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 安全审计服务 - UltraThink重构安全审计架构
    /// 记录和监控系统安全事件
    /// </summary>
    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SecurityAuditService> _logger;
        private readonly IEncryptionService _encryptionService;

        public SecurityAuditService(
            AppDbContext context,
            ILogger<SecurityAuditService> logger,
            IEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        /// <summary>
        /// 记录登录事件
        /// </summary>
        public async Task LogLoginAttemptAsync(LoginAuditEvent loginEvent)
        {
            try
            {
                var auditEntry = new SecurityAuditLog
                {
                    Id = Guid.NewGuid(),
                    EventType = SecurityEventType.LoginAttempt,
                    UserId = loginEvent.UserId,
                    UserName = loginEvent.UserName,
                    ClientIP = loginEvent.ClientIP,
                    UserAgent = loginEvent.UserAgent,
                    IsSuccess = loginEvent.IsSuccess,
                    EventData = JsonSerializer.Serialize(new
                    {
                        loginEvent.LoginMethod,
                        loginEvent.FailureReason,
                        loginEvent.RememberMe
                    }),
                    CreatedAt = DateTime.UtcNow,
                    SessionId = loginEvent.SessionId
                };

                _context.SecurityAuditLogs.Add(auditEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("记录登录审计事件: 用户 {UserName}, IP {ClientIP}, 结果 {Result}",
                    loginEvent.UserName, loginEvent.ClientIP, loginEvent.IsSuccess ? "成功" : "失败");

                // 检查可疑登录活动
                await CheckSuspiciousLoginActivityAsync(loginEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录登录审计事件失败");
            }
        }

        /// <summary>
        /// 记录API访问事件
        /// </summary>
        public async Task LogApiAccessAsync(ApiAccessAuditEvent accessEvent)
        {
            try
            {
                var auditEntry = new SecurityAuditLog
                {
                    Id = Guid.NewGuid(),
                    EventType = SecurityEventType.ApiAccess,
                    UserId = accessEvent.UserId,
                    UserName = accessEvent.UserName,
                    ClientIP = accessEvent.ClientIP,
                    UserAgent = accessEvent.UserAgent,
                    IsSuccess = accessEvent.IsSuccess,
                    EventData = JsonSerializer.Serialize(new
                    {
                        accessEvent.Endpoint,
                        accessEvent.HttpMethod,
                        accessEvent.StatusCode,
                        accessEvent.ResponseTimeMs,
                        accessEvent.RequestId
                    }),
                    CreatedAt = DateTime.UtcNow,
                    SessionId = accessEvent.SessionId
                };

                _context.SecurityAuditLogs.Add(auditEntry);
                await _context.SaveChangesAsync();

                // 记录高风险API访问
                if (IsHighRiskEndpoint(accessEvent.Endpoint))
                {
                    _logger.LogWarning("高风险API访问: 用户 {UserName}, 端点 {Endpoint}, IP {ClientIP}",
                        accessEvent.UserName, accessEvent.Endpoint, accessEvent.ClientIP);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录API访问审计事件失败");
            }
        }

        /// <summary>
        /// 记录数据访问事件
        /// </summary>
        public async Task LogDataAccessAsync(DataAccessAuditEvent dataEvent)
        {
            try
            {
                var auditEntry = new SecurityAuditLog
                {
                    Id = Guid.NewGuid(),
                    EventType = SecurityEventType.DataAccess,
                    UserId = dataEvent.UserId,
                    UserName = dataEvent.UserName,
                    ClientIP = dataEvent.ClientIP,
                    IsSuccess = dataEvent.IsSuccess,
                    EventData = JsonSerializer.Serialize(new
                    {
                        dataEvent.TableName,
                        dataEvent.Operation,
                        dataEvent.RecordId,
                        dataEvent.AffectedColumns,
                        OldValues = dataEvent.OldValues != null ? 
                            _encryptionService.Encrypt(JsonSerializer.Serialize(dataEvent.OldValues)) : null,
                        NewValues = dataEvent.NewValues != null ? 
                            _encryptionService.Encrypt(JsonSerializer.Serialize(dataEvent.NewValues)) : null
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                _context.SecurityAuditLogs.Add(auditEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("记录数据访问审计事件: 用户 {UserName}, 表 {TableName}, 操作 {Operation}",
                    dataEvent.UserName, dataEvent.TableName, dataEvent.Operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录数据访问审计事件失败");
            }
        }

        /// <summary>
        /// 记录安全异常事件
        /// </summary>
        public async Task LogSecurityExceptionAsync(SecurityExceptionAuditEvent exceptionEvent)
        {
            try
            {
                var auditEntry = new SecurityAuditLog
                {
                    Id = Guid.NewGuid(),
                    EventType = SecurityEventType.SecurityException,
                    UserId = exceptionEvent.UserId,
                    UserName = exceptionEvent.UserName,
                    ClientIP = exceptionEvent.ClientIP,
                    UserAgent = exceptionEvent.UserAgent,
                    IsSuccess = false,
                    EventData = JsonSerializer.Serialize(new
                    {
                        exceptionEvent.ExceptionType,
                        exceptionEvent.ExceptionMessage,
                        exceptionEvent.StackTrace,
                        exceptionEvent.RequestPath,
                        exceptionEvent.ThreatLevel
                    }),
                    CreatedAt = DateTime.UtcNow,
                    SessionId = exceptionEvent.SessionId
                };

                _context.SecurityAuditLogs.Add(auditEntry);
                await _context.SaveChangesAsync();

                _logger.LogError("记录安全异常审计事件: 类型 {ExceptionType}, 威胁级别 {ThreatLevel}, IP {ClientIP}",
                    exceptionEvent.ExceptionType, exceptionEvent.ThreatLevel, exceptionEvent.ClientIP);

                // 高威胁级别事件立即告警
                if (exceptionEvent.ThreatLevel == ThreatLevel.High || exceptionEvent.ThreatLevel == ThreatLevel.Critical)
                {
                    await SendSecurityAlertAsync(exceptionEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录安全异常审计事件失败");
            }
        }

        /// <summary>
        /// 获取用户活动报告
        /// </summary>
        public async Task<UserActivityReport> GetUserActivityReportAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var logs = await _context.SecurityAuditLogs
                    .Where(l => l.UserId == userId && l.CreatedAt >= startDate && l.CreatedAt <= endDate)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();

                return new UserActivityReport
                {
                    UserId = userId,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalEvents = logs.Count,
                    LoginAttempts = logs.Count(l => l.EventType == SecurityEventType.LoginAttempt),
                    SuccessfulLogins = logs.Count(l => l.EventType == SecurityEventType.LoginAttempt && l.IsSuccess),
                    FailedLogins = logs.Count(l => l.EventType == SecurityEventType.LoginAttempt && !l.IsSuccess),
                    ApiAccesses = logs.Count(l => l.EventType == SecurityEventType.ApiAccess),
                    DataAccesses = logs.Count(l => l.EventType == SecurityEventType.DataAccess),
                    SecurityExceptions = logs.Count(l => l.EventType == SecurityEventType.SecurityException),
                    UniqueIPs = logs.Select(l => l.ClientIP).Distinct().Count(),
                    LastActivity = logs.FirstOrDefault()?.CreatedAt,
                    Activities = logs.Take(100).Select(l => new AuditLogSummary
                    {
                        Id = l.Id,
                        EventType = l.EventType.ToString(),
                        IsSuccess = l.IsSuccess,
                        ClientIP = l.ClientIP,
                        CreatedAt = l.CreatedAt
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户活动报告失败: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 获取安全警报
        /// </summary>
        public async Task<IEnumerable<SecurityAlert>> GetSecurityAlertsAsync(int hours = 24)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);
            
            var alerts = new List<SecurityAlert>();

            // 检查暴力破解攻击
            await CheckBruteForceAttacksAsync(alerts, cutoffTime);

            // 检查异常IP访问
            await CheckAbnormalIPActivityAsync(alerts, cutoffTime);

            // 检查高风险操作
            await CheckHighRiskOperationsAsync(alerts, cutoffTime);

            return alerts;
        }

        /// <summary>
        /// 检查可疑登录活动
        /// </summary>
        private async Task CheckSuspiciousLoginActivityAsync(LoginAuditEvent loginEvent)
        {
            var recentFailures = await _context.SecurityAuditLogs
                .Where(l => l.EventType == SecurityEventType.LoginAttempt &&
                           l.ClientIP == loginEvent.ClientIP &&
                           !l.IsSuccess &&
                           l.CreatedAt >= DateTime.UtcNow.AddMinutes(-15))
                .CountAsync();

            if (recentFailures >= 5)
            {
                _logger.LogWarning("检测到可疑登录活动: IP {ClientIP} 在15分钟内失败 {FailureCount} 次",
                    loginEvent.ClientIP, recentFailures);

                await LogSecurityExceptionAsync(new SecurityExceptionAuditEvent
                {
                    ExceptionType = "SuspiciousLoginActivity",
                    ExceptionMessage = $"IP {loginEvent.ClientIP} 短时间内多次登录失败",
                    ClientIP = loginEvent.ClientIP,
                    ThreatLevel = ThreatLevel.Medium,
                    RequestPath = "/auth/login"
                });
            }
        }

        /// <summary>
        /// 检查是否为高风险端点
        /// </summary>
        private bool IsHighRiskEndpoint(string endpoint)
        {
            var highRiskEndpoints = new[]
            {
                "/api/v1/users/delete",
                "/api/v1/database/backup",
                "/api/v1/system/config",
                "/api/v1/auth/admin",
                "/api/v1/patients/delete",
                "/api/v1/medical-records/delete"
            };

            return highRiskEndpoints.Any(riskEndpoint => 
                endpoint.Contains(riskEndpoint, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 发送安全告警
        /// </summary>
        private async Task SendSecurityAlertAsync(SecurityExceptionAuditEvent exceptionEvent)
        {
            // TODO: 实现告警通知（邮件、短信、钉钉等）
            _logger.LogCritical("🚨 安全告警: {ExceptionType} - {ExceptionMessage} (IP: {ClientIP})",
                exceptionEvent.ExceptionType, exceptionEvent.ExceptionMessage, exceptionEvent.ClientIP);
        }

        /// <summary>
        /// 检查暴力破解攻击
        /// </summary>
        private async Task CheckBruteForceAttacksAsync(List<SecurityAlert> alerts, DateTime cutoffTime)
        {
            var suspiciousIPs = await _context.SecurityAuditLogs
                .Where(l => l.EventType == SecurityEventType.LoginAttempt &&
                           !l.IsSuccess &&
                           l.CreatedAt >= cutoffTime)
                .GroupBy(l => l.ClientIP)
                .Where(g => g.Count() >= 10)
                .Select(g => new { IP = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var suspiciousIP in suspiciousIPs)
            {
                alerts.Add(new SecurityAlert
                {
                    Id = Guid.NewGuid(),
                    Type = SecurityAlertType.BruteForceAttack,
                    Severity = AlertSeverity.High,
                    Title = "暴力破解攻击检测",
                    Message = $"IP {suspiciousIP.IP} 在过去24小时内登录失败 {suspiciousIP.Count} 次",
                    CreatedAt = DateTime.UtcNow,
                    Data = new { IP = suspiciousIP.IP, FailureCount = suspiciousIP.Count }
                });
            }
        }

        /// <summary>
        /// 检查异常IP活动
        /// </summary>
        private async Task CheckAbnormalIPActivityAsync(List<SecurityAlert> alerts, DateTime cutoffTime)
        {
            // 检查来自异常地理位置的访问
            var highActivityIPs = await _context.SecurityAuditLogs
                .Where(l => l.CreatedAt >= cutoffTime)
                .GroupBy(l => l.ClientIP)
                .Where(g => g.Count() >= 1000) // 24小时内超过1000次请求
                .Select(g => new { IP = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var highActivityIP in highActivityIPs)
            {
                alerts.Add(new SecurityAlert
                {
                    Id = Guid.NewGuid(),
                    Type = SecurityAlertType.AbnormalTraffic,
                    Severity = AlertSeverity.Medium,
                    Title = "异常IP活动",
                    Message = $"IP {highActivityIP.IP} 在过去24小时内请求 {highActivityIP.Count} 次",
                    CreatedAt = DateTime.UtcNow,
                    Data = new { IP = highActivityIP.IP, RequestCount = highActivityIP.Count }
                });
            }
        }

        /// <summary>
        /// 检查高风险操作
        /// </summary>
        private async Task CheckHighRiskOperationsAsync(List<SecurityAlert> alerts, DateTime cutoffTime)
        {
            var highRiskOperations = await _context.SecurityAuditLogs
                .Where(l => l.EventType == SecurityEventType.DataAccess &&
                           l.CreatedAt >= cutoffTime &&
                           l.EventData.Contains("DELETE"))
                .CountAsync();

            if (highRiskOperations >= 100)
            {
                alerts.Add(new SecurityAlert
                {
                    Id = Guid.NewGuid(),
                    Type = SecurityAlertType.HighRiskOperation,
                    Severity = AlertSeverity.High,
                    Title = "大量删除操作",
                    Message = $"过去24小时内检测到 {highRiskOperations} 次删除操作",
                    CreatedAt = DateTime.UtcNow,
                    Data = new { OperationCount = highRiskOperations }
                });
            }
        }
    }
}