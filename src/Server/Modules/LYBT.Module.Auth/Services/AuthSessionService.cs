using AutoMapper;
using LYBT.Entities.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证会话服务实现 - 管理用户登录会话的完整生命周期
    /// 提供会话创建、验证、刷新、撤销等核心功能
    /// </summary>
    public class AuthSessionService : IAuthSessionService
    {
        private readonly IAuthSessionRepository _sessionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthSessionService> _logger;

        public AuthSessionService(
            IAuthSessionRepository sessionRepository,
            IMapper mapper,
            ILogger<AuthSessionService> logger)
        {
            _sessionRepository = sessionRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 创建新的认证会话 - UltraThink v2.0简化版
        /// </summary>
        public async Task<BaseAuthSession> CreateSessionAsync(string username, Guid userId, string ipAddress, string? userAgent = null)
        {
            var sessionModel = new AuthSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = "", // 需要外部传入实际Token哈希                LoginTime = DateTime.Now,
                ExpiryTime = DateTime.Now.AddHours(8), // 8小时过期
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsRevoked = false,
                Status = CommonStatus.Enabled
            };

            var createdSession = await _sessionRepository.AddAsync(sessionModel);
            var baseSession = _mapper.Map<BaseAuthSession>(createdSession);

            _logger.LogInformation("创建新会话 - 用户ID: {UserId}, 会话ID: {SessionId}, IP: {IpAddress}",                 userId, createdSession.Id, ipAddress);

            return baseSession;
        }

        /// <summary>
        /// 根据令牌哈希验证会话 - UltraThink v2.0简化版
        /// </summary>
        public async Task<BaseAuthSession?> ValidateSessionAsync(string tokenHash)
        {
            var sessionModel = await _sessionRepository.GetByTokenHashAsync(tokenHash);
            if (sessionModel == null)
            {
                _logger.LogWarning("会话验证失败 - 无效的令牌哈希: {TokenHash}", tokenHash[..10] + "...");                return null;
            }

            // 检查会话状态
            if (sessionModel.Status != CommonStatus.Enabled)
            {
                _logger.LogWarning("会话验证失败 - 会话状态无效: {Status}, 会话ID: {SessionId}",                     sessionModel.Status, sessionModel.Id);
                return null;
            }

            // 检查是否被撤销
            if (sessionModel.IsRevoked)
            {
                _logger.LogWarning("会话验证失败 - 会话已被撤销: {SessionId}", sessionModel.Id);                return null;
            }

            // 检查令牌是否过期
            if (sessionModel.ExpiryTime < DateTime.Now)
            {
                // 自动标记为已撤销
                sessionModel.IsRevoked = true;
                sessionModel.Status = CommonStatus.Disabled;
                sessionModel.LogoutTime = DateTime.Now;
                await _sessionRepository.UpdateAsync(sessionModel);

                _logger.LogInformation("会话令牌已过期 - 会话ID: {SessionId}", sessionModel.Id);                return null;
            }

            return _mapper.Map<BaseAuthSession>(sessionModel);
        }

        /// <summary>
        /// 刷新会话令牌 - UltraThink v2.0简化版
        /// </summary>
        public async Task<BaseAuthSession?> RefreshSessionAsync(string currentTokenHash, string newTokenHash, DateTime newExpiryTime)
        {
            var sessionModel = await _sessionRepository.GetByTokenHashAsync(currentTokenHash);
            if (sessionModel == null || sessionModel.Status != CommonStatus.Enabled || sessionModel.IsRevoked)
            {
                _logger.LogWarning("令牌刷新失败 - 无效的令牌: {Token}", currentTokenHash[..10] + "...");                return null;
            }

            // 更新令牌信息
            sessionModel.TokenHash = newTokenHash;
            sessionModel.ExpiryTime = newExpiryTime;

            await _sessionRepository.UpdateAsync(sessionModel);

            _logger.LogInformation("令牌刷新成功 - 会话ID: {SessionId}", sessionModel.Id);            return _mapper.Map<BaseAuthSession>(sessionModel);
        }

        /// <summary>
        /// 撤销用户所有活跃会话
        /// </summary>
        public async Task RevokeAllUserSessionsAsync(Guid userId, string reason, Guid? revokedBy = null)
        {
            await _sessionRepository.RevokeAllUserSessionsAsync(userId, reason, revokedBy);
            _logger.LogInformation("撤销用户所有会话 - 用户ID: {UserId}, 原因: {Reason}", userId, reason);        }

        /// <summary>
        /// 撤销特定会话
        /// </summary>
        public async Task RevokeSessionAsync(Guid sessionId, string reason, Guid? revokedBy = null)
        {
            await _sessionRepository.RevokeSessionAsync(sessionId, reason, revokedBy);
            _logger.LogInformation("撤销会话 - 会话ID: {SessionId}, 原因: {Reason}", sessionId, reason);        }

        /// <summary>
        /// 用户登出 - UltraThink v2.0简化版
        /// </summary>
        public async Task LogoutSessionAsync(Guid sessionId)
        {
            var sessionModel = await _sessionRepository.GetByIdAsync(sessionId);
            if (sessionModel != null && sessionModel.Status == CommonStatus.Enabled && !sessionModel.IsRevoked)
            {
                sessionModel.IsRevoked = true;
                sessionModel.Status = CommonStatus.Disabled;
                sessionModel.LogoutTime = DateTime.Now;
                await _sessionRepository.UpdateAsync(sessionModel);

                _logger.LogInformation("用户登出 - 会话ID: {SessionId}, 用户ID: {UserId}",                     sessionId, sessionModel.UserId);
            }
        }

        /// <summary>
        /// 获取用户的活跃会话列表
        /// </summary>
        public async Task<List<BaseAuthSession>> GetUserActiveSessionsAsync(Guid userId)
        {
            var sessionModels = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId);
            return _mapper.Map<List<BaseAuthSession>>(sessionModels);
        }

        /// <summary>
        /// 更新会话最后活跃时间 - UltraThink v2.0简化版（功能暂时移除）
        /// </summary>
        public async Task UpdateSessionActivityAsync(Guid sessionId, DateTime lastActivity)
        {
            // UltraThink v2.0简化版：暂时移除最后活跃时间功能
            // 原因：AuthSession实体中没有LastActivityTime字段
            _logger.LogDebug("会话活动更新请求 - 会话ID: {SessionId} (功能已简化)", sessionId);            await Task.CompletedTask;
        }

        /// <summary>
        /// 检查并清理过期会话
        /// </summary>
        public async Task CleanupExpiredSessionsAsync()
        {
            await _sessionRepository.CleanupExpiredSessionsAsync();
            _logger.LogInformation("清理过期会话完成");        }

        /// <summary>
        /// 获取会话统计信息
        /// </summary>
        public async Task<(int TotalSessions, int ActiveSessions, int ExpiredSessions)> GetSessionStatisticsAsync()
        {
            return await _sessionRepository.GetSessionStatsAsync();
        }

        /// <summary>
        /// 检测可疑的会话活动 - UltraThink v2.0简化版
        /// </summary>
        public async Task<List<BaseAuthSession>> DetectSuspiciousSessionsAsync(TimeSpan timeWindow)
        {
            var allSessions = await _sessionRepository.GetAllAsync();
            var cutoffTime = DateTime.Now - timeWindow;

            // 检测逻辑简化：同一用户在短时间内从多个不同IP登录
            var suspiciousSessions = allSessions
                .Where(s => s.LoginTime >= cutoffTime)
                .GroupBy(s => s.UserId)
                .Where(g => g.Select(s => s.IpAddress).Distinct().Count() > 2) // 超过2个不同IP
                .SelectMany(g => g)
                .ToList();

            return _mapper.Map<List<BaseAuthSession>>(suspiciousSessions);
        }

        /// <summary>
        /// 标记会话异常 - UltraThink v2.0简化版
        /// </summary>
        public async Task MarkSessionAnomalyAsync(Guid sessionId, string description)
        {
            // UltraThink v2.0简化版：直接标记为已撤销，记录日志
            var sessionModel = await _sessionRepository.GetByIdAsync(sessionId);
            if (sessionModel != null)
            {
                sessionModel.IsRevoked = true;
                sessionModel.Status = CommonStatus.Disabled;
                await _sessionRepository.UpdateAsync(sessionModel);
            }
            
            _logger.LogWarning("标记会话异常并撤销 - 会话ID: {SessionId}, 描述: {Description}", sessionId, description);        }

        /// <summary>
        /// 批量更新会话状态 - UltraThink v2.0简化版
        /// </summary>
        public async Task UpdateSessionStatusBatchAsync(List<Guid> sessionIds, CommonStatus status, string? reason = null)
        {
            foreach (var sessionId in sessionIds)
            {
                var session = await _sessionRepository.GetByIdAsync(sessionId);
                if (session != null)
                {
                    session.Status = status;
                    if (status == CommonStatus.Disabled)
                    {
                        session.IsRevoked = true;
                        session.LogoutTime = DateTime.Now;
                    }
                    await _sessionRepository.UpdateAsync(session);
                }
            }
            _logger.LogInformation("批量更新会话状态 - 数量: {Count}, 状态: {Status}", sessionIds.Count, status);        }

        /// <summary>
        /// 根据设备信息查找会话 - UltraThink v2.0简化版（功能移除）
        /// </summary>
        public Task<List<BaseAuthSession>> GetSessionsByDeviceAsync(string deviceInfo, TimeSpan? timeWindow = null)
        {
            // UltraThink v2.0简化版：AuthSession实体中没有DeviceInfo字段
            // 返回空列表，记录请求日志
            _logger.LogDebug("设备会话查询请求 - 设备信息: {DeviceInfo} (功能已简化)", deviceInfo);
            return Task.FromResult(new List<BaseAuthSession>());
        }

        /// <summary>
        /// 根据IP地址查找会话（安全监控）
        /// </summary>
        public async Task<List<BaseAuthSession>> GetSessionsByIpAddressAsync(string ipAddress, TimeSpan? timeWindow = null)
        {
            var sessionModels = await _sessionRepository.GetSessionsByIpAddressAsync(ipAddress, timeWindow);
            return _mapper.Map<List<BaseAuthSession>>(sessionModels);
        }

        /// <summary>
        /// 检查是否为可疑登录位置 - UltraThink v2.0简化版
        /// </summary>
        public async Task<bool> IsSuspiciousLocationAsync(Guid userId, string ipAddress, string? location = null)
        {
            // 获取用户过去30天的登录历史
            var userSessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId);
            var recentSessions = userSessions
                .Where(s => s.LoginTime >= DateTime.Now.AddDays(-30))
                .ToList();

            // 如果用户从未从这个IP登录过，则可能是可疑位置
            var hasLoggedInFromThisIp = recentSessions.Any(s => s.IpAddress == ipAddress);
            if (!hasLoggedInFromThisIp)
            {
                // 进一步检查IP地址的地理位置变化（这里简化处理）
                var uniqueIps = recentSessions.Select(s => s.IpAddress).Distinct().Count();
                if (uniqueIps > 0) // 用户有登录历史且这是新IP
                {
                    _logger.LogWarning("检测到可疑登录位置 - 用户ID: {UserId}, IP: {IpAddress}", userId, ipAddress);                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 设置会话扩展数据 - UltraThink v2.0简化版（功能移除）
        /// </summary>
        public async Task SetSessionExtendedDataAsync(Guid sessionId, string extendedData)
        {
            // UltraThink v2.0简化版：AuthSession实体中没有ExtendedData字段
            // 功能已移除，仅记录请求日志
            _logger.LogDebug("会话扩展数据设置请求 - 会话ID: {SessionId} (功能已简化)", sessionId);            await Task.CompletedTask;
        }

        /// <summary>
        /// 强制用户重新登录（管理员操作）
        /// </summary>
        public async Task ForceUserReloginAsync(Guid userId, string reason, Guid operatorId)
        {
            await RevokeAllUserSessionsAsync(userId, $"管理员强制重新登录: {reason}", operatorId);            _logger.LogInformation("强制用户重新登录 - 用户ID: {UserId}, 操作员ID: {OperatorId}, 原因: {Reason}", 
                userId, operatorId, reason);
        }
    }
}
