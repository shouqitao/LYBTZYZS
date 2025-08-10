using AutoMapper;
using LYBT.Models.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 安全日志服务实现 - 统一记录和管理系统安全事件
    /// 提供安全事件记录、分析、报告和响应功能
    /// </summary>
    public class SecurityLogService : ISecurityLogService
    {
        private readonly ISecurityLogRepository _securityLogRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SecurityLogService> _logger;

        public SecurityLogService(
            ISecurityLogRepository securityLogRepository,
            IMapper mapper,
            ILogger<SecurityLogService> logger)
        {
            _securityLogRepository = securityLogRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 记录安全事件
        /// </summary>
        public async Task<BaseSecurityLog> LogSecurityEventAsync(AuthEventType eventType, string description,
                                                                Guid? userId = null, string? username = null,
                                                                string? clientIp = null, string? userAgent = null,
                                                                SecurityLevel level = SecurityLevel.Low,
                                                                string? affectedResource = null,
                                                                OperationResult result = OperationResult.Success,
                                                                string? details = null, Guid? sessionId = null)
        {
            var logModel = new SecurityLogModel
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Description = description,
                EventTime = DateTime.Now,
                UserId = userId,
                Username = username,
                ClientIp = clientIp,
                UserAgent = userAgent,
                Level = level,
                AffectedResource = affectedResource,
                Result = result,
                Details = details,
                SessionId = sessionId,
                RequiresNotification = level >= SecurityLevel.High,
                IsProcessed = false,
                CreateTime = DateTime.Now,
                RiskScore = CalculateRiskScore(eventType, level, result),
                RequiresEscalation = level >= SecurityLevel.Critical
            };

            if (logModel.RequiresEscalation)
            {
                logModel.EscalationLevel = (int)level;
            }

            var savedLog = await _securityLogRepository.LogSecurityEventAsync(logModel);

            // 记录到系统日志
            _logger.LogInformation("安全事件已记录 - 类型: {EventType}, 级别: {Level}, 用户: {Username}, IP: {ClientIp}", 
                eventType, level, username, clientIp);

            return _mapper.Map<BaseSecurityLog>(savedLog);
        }

        /// <summary>
        /// 记录登录成功事件
        /// </summary>
        public async Task LogLoginSuccessAsync(Guid userId, string username, string? clientIp, string? userAgent, 
                                             LoginType loginType, Guid? sessionId = null)
        {
            var description = $"用户登录成功 - 登录方式: {GetLoginTypeDescription(loginType)}";
            
            await LogSecurityEventAsync(
                AuthEventType.LoginSuccess, 
                description,
                userId, 
                username, 
                clientIp, 
                userAgent,
                SecurityLevel.Low,
                "Authentication",
                OperationResult.Success,
                JsonSerializer.Serialize(new { LoginType = loginType }),
                sessionId
            );
        }

        /// <summary>
        /// 记录登录失败事件
        /// </summary>
        public async Task LogLoginFailureAsync(string username, string failureReason, string? clientIp, 
                                             string? userAgent, LoginType loginType, SecurityLevel riskLevel)
        {
            var description = $"用户登录失败 - 原因: {failureReason}";
            
            await LogSecurityEventAsync(
                AuthEventType.LoginFailed, 
                description,
                null, 
                username, 
                clientIp, 
                userAgent,
                riskLevel,
                "Authentication",
                OperationResult.Failed,
                JsonSerializer.Serialize(new { FailureReason = failureReason, LoginType = loginType })
            );
        }

        /// <summary>
        /// 记录权限被拒绝事件
        /// </summary>
        public async Task LogPermissionDeniedAsync(Guid? userId, string? username, string resource, 
                                                 string action, string? clientIp = null)
        {
            var description = $"权限被拒绝 - 资源: {resource}, 操作: {action}";
            
            await LogSecurityEventAsync(
                AuthEventType.PermissionDenied, 
                description,
                userId, 
                username, 
                clientIp, 
                null,
                SecurityLevel.Medium,
                resource,
                OperationResult.Forbidden,
                JsonSerializer.Serialize(new { Resource = resource, Action = action })
            );
        }

        /// <summary>
        /// 记录数据访问事件
        /// </summary>
        public async Task LogDataAccessAsync(Guid userId, string username, string resourceType, 
                                           string resourceId, string operation, bool isSuccess = true)
        {
            var description = $"数据访问 - {operation} {resourceType}";
            
            await LogSecurityEventAsync(
                AuthEventType.DataAccess, 
                description,
                userId, 
                username, 
                null, 
                null,
                SecurityLevel.Low,
                $"{resourceType}/{resourceId}",
                isSuccess ? OperationResult.Success : OperationResult.Failed,
                JsonSerializer.Serialize(new { ResourceType = resourceType, ResourceId = resourceId, Operation = operation })
            );
        }

        /// <summary>
        /// 记录可疑活动事件
        /// </summary>
        public async Task LogSuspiciousActivityAsync(string description, string? username = null, 
                                                   string? clientIp = null, SecurityLevel level = SecurityLevel.High,
                                                   string? evidence = null)
        {
            await LogSecurityEventAsync(
                AuthEventType.SuspiciousActivity, 
                $"可疑活动检测: {description}",
                null, 
                username, 
                clientIp, 
                null,
                level,
                "Security",
                OperationResult.Warning,
                evidence
            );
        }

        /// <summary>
        /// 记录系统错误事件
        /// </summary>
        public async Task LogSystemErrorAsync(string errorMessage, string? stackTrace = null, 
                                            string? requestPath = null, Guid? userId = null)
        {
            var logModel = new SecurityLogModel
            {
                Id = Guid.NewGuid(),
                EventType = AuthEventType.SystemError,
                Description = $"系统错误: {errorMessage}",
                EventTime = DateTime.Now,
                UserId = userId,
                Level = SecurityLevel.Medium,
                AffectedResource = requestPath,
                Result = OperationResult.Error,
                Details = errorMessage,
                StackTrace = stackTrace,
                RequestPath = requestPath,
                RequiresNotification = true,
                CreateTime = DateTime.Now,
                RiskScore = 40
            };

            await _securityLogRepository.LogSecurityEventAsync(logModel);
        }

        /// <summary>
        /// 记录密码变更事件
        /// </summary>
        public async Task LogPasswordChangeAsync(Guid userId, string username, bool isSuccess, 
                                               string? clientIp = null, bool isReset = false)
        {
            var description = isReset ? "密码重置" : "密码修改";
            description += isSuccess ? "成功" : "失败";
            
            await LogSecurityEventAsync(
                AuthEventType.PasswordChanged, 
                description,
                userId, 
                username, 
                clientIp, 
                null,
                SecurityLevel.Medium,
                "UserAccount",
                isSuccess ? OperationResult.Success : OperationResult.Failed,
                JsonSerializer.Serialize(new { IsReset = isReset, IsSuccess = isSuccess })
            );
        }

        /// <summary>
        /// 记录账户锁定事件
        /// </summary>
        public async Task LogAccountLockAsync(Guid userId, string username, string reason, 
                                            TimeSpan lockDuration, string? clientIp = null)
        {
            var description = $"账户锁定 - 原因: {reason}, 时长: {lockDuration.TotalMinutes}分钟";
            
            await LogSecurityEventAsync(
                AuthEventType.AccountLocked, 
                description,
                userId, 
                username, 
                clientIp, 
                null,
                SecurityLevel.High,
                "UserAccount",
                OperationResult.Success,
                JsonSerializer.Serialize(new { Reason = reason, LockDuration = lockDuration })
            );
        }

        /// <summary>
        /// 获取未处理的安全日志
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetUnprocessedLogsAsync()
        {
            var logs = await _securityLogRepository.GetUnprocessedLogsAsync();
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 获取需要通知的安全日志
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetLogsRequiringNotificationAsync()
        {
            var logs = await _securityLogRepository.GetLogsRequiringNotificationAsync();
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 标记日志为已处理
        /// </summary>
        public async Task MarkLogAsProcessedAsync(Guid logId, Guid processedBy, string? notes = null)
        {
            await _securityLogRepository.MarkAsProcessedAsync(logId, processedBy, notes);
            _logger.LogInformation("安全日志已标记为处理 - 日志ID: {LogId}, 处理人: {ProcessedBy}", logId, processedBy);
        }

        /// <summary>
        /// 批量处理安全日志
        /// </summary>
        public async Task BatchProcessLogsAsync(List<Guid> logIds, Guid processedBy, string? notes = null)
        {
            await _securityLogRepository.MarkBatchAsProcessedAsync(logIds, processedBy, notes);
            _logger.LogInformation("批量处理安全日志 - 数量: {Count}, 处理人: {ProcessedBy}", logIds.Count, processedBy);
        }

        /// <summary>
        /// 标记日志为已通知
        /// </summary>
        public async Task MarkLogAsNotifiedAsync(Guid logId, string notificationMethod)
        {
            await _securityLogRepository.MarkAsNotifiedAsync(logId, notificationMethod);
        }

        /// <summary>
        /// 根据事件类型获取日志
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetLogsByEventTypeAsync(AuthEventType eventType, TimeSpan timeSpan)
        {
            var logs = await _securityLogRepository.GetLogsByEventTypeAsync(eventType, timeSpan);
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 根据安全级别获取日志
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetLogsBySecurityLevelAsync(SecurityLevel level, TimeSpan timeSpan)
        {
            var logs = await _securityLogRepository.GetLogsBySecurityLevelAsync(level, timeSpan);
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 获取用户的安全日志
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetUserSecurityLogsAsync(Guid userId, TimeSpan timeSpan)
        {
            var logs = await _securityLogRepository.GetLogsByUserAsync(userId, timeSpan);
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 获取IP地址的安全日志
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetIpSecurityLogsAsync(string ipAddress, TimeSpan timeSpan)
        {
            var logs = await _securityLogRepository.GetLogsByIpAddressAsync(ipAddress, timeSpan);
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 获取高风险安全事件
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetHighRiskEventsAsync(TimeSpan timeSpan, int minRiskScore = 70)
        {
            var logs = await _securityLogRepository.GetHighRiskEventsAsync(timeSpan, minRiskScore);
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 获取需要升级的安全事件
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetEventsRequiringEscalationAsync()
        {
            var logs = await _securityLogRepository.GetEventsRequiringEscalationAsync();
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 获取安全事件统计
        /// </summary>
        public async Task<Dictionary<AuthEventType, int>> GetEventStatisticsAsync(TimeSpan timeSpan)
        {
            return await _securityLogRepository.GetEventTypeStatsAsync(timeSpan);
        }

        /// <summary>
        /// 获取安全趋势分析
        /// </summary>
        public async Task<Dictionary<DateTime, int>> GetSecurityTrendsAsync(DateTime startDate, DateTime endDate)
        {
            return await _securityLogRepository.GetEventsByHourAsync(startDate, endDate);
        }

        /// <summary>
        /// 检测安全异常模式
        /// </summary>
        public async Task<List<string>> DetectSecurityAnomaliesAsync(TimeSpan analysisWindow)
        {
            var anomalies = new List<string>();

            // 检测异常事件模式
            var eventStats = await GetEventStatisticsAsync(analysisWindow);
            var totalEvents = eventStats.Values.Sum();
            
            if (totalEvents == 0) return anomalies;

            // 检测异常高的失败登录
            if (eventStats.ContainsKey(AuthEventType.LoginFailed))
            {
                var failureRate = (double)eventStats[AuthEventType.LoginFailed] / totalEvents;
                if (failureRate > 0.5)
                {
                    anomalies.Add($"登录失败率异常高: {failureRate:P}");
                }
            }

            // 检测异常高的权限拒绝
            if (eventStats.ContainsKey(AuthEventType.PermissionDenied))
            {
                var denialCount = eventStats[AuthEventType.PermissionDenied];
                if (denialCount > totalEvents * 0.1)
                {
                    anomalies.Add($"权限拒绝事件异常频繁: {denialCount} 次");
                }
            }

            // 检测可疑活动激增
            if (eventStats.ContainsKey(AuthEventType.SuspiciousActivity))
            {
                var suspiciousCount = eventStats[AuthEventType.SuspiciousActivity];
                if (suspiciousCount > 10)
                {
                    anomalies.Add($"可疑活动数量异常: {suspiciousCount} 次");
                }
            }

            // 检测最活跃的攻击IP
            var activeIps = await _securityLogRepository.GetMostActiveIpsAsync(analysisWindow, 5);
            var highRiskIps = activeIps.Where(ip => ip.HighRiskCount > 5);
            
            if (highRiskIps.Any())
            {
                anomalies.Add($"检测到 {highRiskIps.Count()} 个高风险IP地址");
            }

            return anomalies;
        }

        /// <summary>
        /// 生成安全事件报告
        /// </summary>
        public async Task<string> GenerateSecurityReportAsync(DateTime startDate, DateTime endDate, 
                                                             SecurityLevel? minLevel = null)
        {
            var timeSpan = endDate - startDate;
            var eventStats = await GetEventStatisticsAsync(timeSpan);
            var levelStats = await _securityLogRepository.GetSecurityLevelStatsAsync(timeSpan);
            var trends = await GetSecurityTrendsAsync(startDate, endDate);
            var highRiskEvents = await GetHighRiskEventsAsync(timeSpan);
            var activeUsers = await _securityLogRepository.GetMostActiveUsersAsync(timeSpan, 10);
            var activeIps = await _securityLogRepository.GetMostActiveIpsAsync(timeSpan, 10);

            var report = new
            {
                ReportPeriod = new { Start = startDate, End = endDate },
                Summary = new
                {
                    TotalEvents = eventStats.Values.Sum(),
                    HighRiskEvents = highRiskEvents.Count,
                    MostActiveUser = activeUsers.FirstOrDefault(),
                    MostActiveIp = activeIps.FirstOrDefault()
                },
                EventTypeDistribution = eventStats,
                SecurityLevelDistribution = levelStats,
                HourlyTrends = trends,
                TopUsers = activeUsers.Take(5),
                TopIPs = activeIps.Take(5),
                HighRiskEventsSample = highRiskEvents.Take(10).Select(e => new
                {
                    e.EventType,
                    e.Description,
                    e.EventTime,
                    e.Level,
                    e.Username,
                    e.ClientIp
                }),
                GeneratedAt = DateTime.Now,
                MinSecurityLevel = minLevel
            };

            return JsonSerializer.Serialize(report, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>
        /// 搜索安全日志
        /// </summary>
        public async Task<List<BaseSecurityLog>> SearchSecurityLogsAsync(string searchTerm, 
                                                                        SecurityLevel? minLevel = null, 
                                                                        TimeSpan? timeSpan = null)
        {
            var logs = await _securityLogRepository.SearchLogsAsync(searchTerm, minLevel, timeSpan);
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 获取相关安全事件
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetRelatedEventsAsync(Guid logId, TimeSpan correlationWindow)
        {
            var logs = await _securityLogRepository.GetRelatedEventsAsync(logId, correlationWindow);
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        /// <summary>
        /// 更新事件风险评分
        /// </summary>
        public async Task UpdateEventRiskScoreAsync(Guid logId, int riskScore, string? analysisResult = null)
        {
            await _securityLogRepository.UpdateRiskScoreAsync(logId, riskScore, analysisResult);
            _logger.LogInformation("更新安全事件风险评分 - 日志ID: {LogId}, 评分: {RiskScore}", logId, riskScore);
        }

        /// <summary>
        /// 清理和归档旧日志
        /// </summary>
        public async Task CleanupAndArchiveLogsAsync(TimeSpan archiveAfter)
        {
            await _securityLogRepository.ArchiveOldLogsAsync(archiveAfter);
            await _securityLogRepository.CleanupArchivedLogsAsync();
            _logger.LogInformation("安全日志清理和归档完成 - 归档期限: {ArchiveAfter}", archiveAfter);
        }

        /// <summary>
        /// 获取合规性审计数据
        /// </summary>
        public async Task<List<BaseSecurityLog>> GetComplianceAuditDataAsync(DateTime startDate, DateTime endDate, 
                                                                            string? complianceType = null)
        {
            var logs = await _securityLogRepository.GetComplianceReportDataAsync(startDate, endDate, complianceType);
            return _mapper.Map<List<BaseSecurityLog>>(logs);
        }

        #region 私有辅助方法

        /// <summary>
        /// 计算事件风险评分
        /// </summary>
        private int CalculateRiskScore(AuthEventType eventType, SecurityLevel level, OperationResult result)
        {
            var baseScore = (int)level * 20;

            // 根据事件类型调整分数
            baseScore += eventType switch
            {
                AuthEventType.LoginFailed => 15,
                AuthEventType.SuspiciousActivity => 30,
                AuthEventType.PermissionDenied => 20,
                AuthEventType.AccountLocked => 25,
                AuthEventType.SecurityAlert => 35,
                AuthEventType.ComplianceViolation => 40,
                AuthEventType.SystemError => 10,
                _ => 5
            };

            // 根据操作结果调整分数
            if (result == OperationResult.Failed || result == OperationResult.Error)
            {
                baseScore += 10;
            }

            return Math.Min(100, baseScore);
        }

        /// <summary>
        /// 获取登录类型描述
        /// </summary>
        private string GetLoginTypeDescription(LoginType loginType)
        {
            return loginType switch
            {
                LoginType.Password => "密码登录",
                LoginType.WeChat => "微信登录",
                LoginType.SmsCode => "短信验证码",
                LoginType.QrCode => "二维码扫描",
                LoginType.Fingerprint => "指纹识别",
                LoginType.FaceRecognition => "人脸识别",
                LoginType.TwoFactor => "双因子认证",
                _ => "其他方式"
            };
        }

        #endregion
    }
}