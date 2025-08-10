using AutoMapper;
using LYBT.Models.Auth;
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
        /// 创建新的认证会话
        /// </summary>
        public async Task<BaseAuthSession> CreateSessionAsync(string username, Guid userId, LoginType loginType, 
                                                             string ipAddress, string? userAgent = null, 
                                                             bool rememberMe = false, string? deviceInfo = null)
        {
            var sessionModel = new AuthSessionModel
            {
                Id = Guid.NewGuid(),
                Username = username,
                UserId = userId,
                LoginType = loginType,
                LoginTime = DateTime.Now,
                ClientIp = ipAddress,
                UserAgent = userAgent,
                RememberMe = rememberMe,
                Status = AuthSessionStatus.Active,
                DeviceInfo = deviceInfo,
                LastActivityTime = DateTime.Now,
                ServerInfo = Environment.MachineName,
                CreateTime = DateTime.Now
            };

            var createdSession = await _sessionRepository.AddAsync(sessionModel);
            var baseSession = _mapper.Map<BaseAuthSession>(createdSession);

            _logger.LogInformation("创建新会话 - 用户: {Username}, 会话ID: {SessionId}, IP: {IpAddress}", 
                username, createdSession.Id, ipAddress);

            return baseSession;
        }

        /// <summary>
        /// 根据令牌哈希验证会话
        /// </summary>
        public async Task<BaseAuthSession?> ValidateSessionAsync(string tokenHash)
        {
            var sessionModel = await _sessionRepository.GetByTokenHashAsync(tokenHash);
            if (sessionModel == null)
            {
                _logger.LogWarning("会话验证失败 - 无效的令牌哈希: {TokenHash}", tokenHash[..10] + "...");
                return null;
            }

            // 检查会话状态
            if (sessionModel.Status != AuthSessionStatus.Active)
            {
                _logger.LogWarning("会话验证失败 - 会话状态无效: {Status}, 会话ID: {SessionId}", 
                    sessionModel.Status, sessionModel.Id);
                return null;
            }

            // 检查令牌是否过期
            if (sessionModel.TokenExpiryTime.HasValue && sessionModel.TokenExpiryTime.Value < DateTime.Now)
            {
                // 自动标记为过期
                sessionModel.Status = AuthSessionStatus.Expired;
                sessionModel.LogoutTime = DateTime.Now;
                await _sessionRepository.UpdateAsync(sessionModel);

                _logger.LogInformation("会话令牌已过期 - 会话ID: {SessionId}", sessionModel.Id);
                return null;
            }

            // 更新最后活跃时间
            await UpdateSessionActivityAsync(sessionModel.Id, DateTime.Now);

            return _mapper.Map<BaseAuthSession>(sessionModel);
        }

        /// <summary>
        /// 刷新会话令牌
        /// </summary>
        public async Task<BaseAuthSession?> RefreshSessionAsync(string refreshTokenHash, string newTokenHash, DateTime newExpiryTime)
        {
            var sessionModel = await _sessionRepository.GetByRefreshTokenHashAsync(refreshTokenHash);
            if (sessionModel == null || sessionModel.Status != AuthSessionStatus.Active)
            {
                _logger.LogWarning("令牌刷新失败 - 无效的刷新令牌: {RefreshToken}", refreshTokenHash[..10] + "...");
                return null;
            }

            // 更新令牌信息
            sessionModel.JwtTokenHash = newTokenHash;
            sessionModel.TokenExpiryTime = newExpiryTime;
            sessionModel.RefreshCount++;
            sessionModel.LastRefreshTime = DateTime.Now;
            sessionModel.LastActivityTime = DateTime.Now;

            await _sessionRepository.UpdateAsync(sessionModel);

            _logger.LogInformation("令牌刷新成功 - 会话ID: {SessionId}, 刷新次数: {RefreshCount}", 
                sessionModel.Id, sessionModel.RefreshCount);

            return _mapper.Map<BaseAuthSession>(sessionModel);
        }

        /// <summary>
        /// 撤销用户所有活跃会话
        /// </summary>
        public async Task RevokeAllUserSessionsAsync(Guid userId, string reason, Guid? revokedBy = null)
        {
            await _sessionRepository.RevokeAllUserSessionsAsync(userId, reason, revokedBy);
            _logger.LogInformation("撤销用户所有会话 - 用户ID: {UserId}, 原因: {Reason}", userId, reason);
        }

        /// <summary>
        /// 撤销特定会话
        /// </summary>
        public async Task RevokeSessionAsync(Guid sessionId, string reason, Guid? revokedBy = null)
        {
            await _sessionRepository.RevokeSessionAsync(sessionId, reason, revokedBy);
            _logger.LogInformation("撤销会话 - 会话ID: {SessionId}, 原因: {Reason}", sessionId, reason);
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task LogoutSessionAsync(Guid sessionId)
        {
            var sessionModel = await _sessionRepository.GetByIdAsync(sessionId);
            if (sessionModel != null && sessionModel.Status == AuthSessionStatus.Active)
            {
                sessionModel.Status = AuthSessionStatus.LoggedOut;
                sessionModel.LogoutTime = DateTime.Now;
                await _sessionRepository.UpdateAsync(sessionModel);

                _logger.LogInformation("用户登出 - 会话ID: {SessionId}, 用户: {Username}", 
                    sessionId, sessionModel.Username);
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
        /// 更新会话最后活跃时间
        /// </summary>
        public async Task UpdateSessionActivityAsync(Guid sessionId, DateTime lastActivity)
        {
            await _sessionRepository.UpdateLastActivityAsync(sessionId, lastActivity);
        }

        /// <summary>
        /// 检查并清理过期会话
        /// </summary>
        public async Task CleanupExpiredSessionsAsync()
        {
            await _sessionRepository.CleanupExpiredSessionsAsync();
            _logger.LogInformation("清理过期会话完成");
        }

        /// <summary>
        /// 获取会话统计信息
        /// </summary>
        public async Task<(int TotalSessions, int ActiveSessions, int ExpiredSessions)> GetSessionStatisticsAsync()
        {
            return await _sessionRepository.GetSessionStatsAsync();
        }

        /// <summary>
        /// 检测可疑的会话活动
        /// </summary>
        public async Task<List<BaseAuthSession>> DetectSuspiciousSessionsAsync(TimeSpan timeWindow)
        {
            var allSessions = await _sessionRepository.GetAllAsync();
            var cutoffTime = DateTime.Now - timeWindow;

            // 检测逻辑：
            // 1. 同一用户在短时间内从多个不同IP登录
            // 2. 同一IP在短时间内多个不同用户登录
            // 3. 异常的登录模式

            var suspiciousSessions = allSessions
                .Where(s => s.LoginTime >= cutoffTime)
                .GroupBy(s => s.Username)
                .Where(g => g.Select(s => s.ClientIp).Distinct().Count() > 2) // 超过2个不同IP
                .SelectMany(g => g)
                .ToList();

            return _mapper.Map<List<BaseAuthSession>>(suspiciousSessions);
        }

        /// <summary>
        /// 标记会话异常
        /// </summary>
        public async Task MarkSessionAnomalyAsync(Guid sessionId, string description)
        {
            await _sessionRepository.MarkSessionAnomalyAsync(sessionId, description);
            _logger.LogWarning("标记会话异常 - 会话ID: {SessionId}, 描述: {Description}", sessionId, description);
        }

        /// <summary>
        /// 批量更新会话状态
        /// </summary>
        public async Task UpdateSessionStatusBatchAsync(List<Guid> sessionIds, AuthSessionStatus status, string? reason = null)
        {
            await _sessionRepository.UpdateSessionStatusBatchAsync(sessionIds, status, reason);
            _logger.LogInformation("批量更新会话状态 - 数量: {Count}, 状态: {Status}", sessionIds.Count, status);
        }

        /// <summary>
        /// 根据设备信息查找会话
        /// </summary>
        public async Task<List<BaseAuthSession>> GetSessionsByDeviceAsync(string deviceInfo, TimeSpan? timeWindow = null)
        {
            var sessionModels = await _sessionRepository.GetSessionsByDeviceInfoAsync(deviceInfo, timeWindow);
            return _mapper.Map<List<BaseAuthSession>>(sessionModels);
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
        /// 检查是否为可疑登录位置
        /// </summary>
        public async Task<bool> IsSuspiciousLocationAsync(string username, string ipAddress, string? location = null)
        {
            // 获取用户过去30天的登录历史
            var userSessions = await _sessionRepository.GetActiveSessionsByUsernameAsync(username);
            var recentSessions = userSessions
                .Where(s => s.LoginTime >= DateTime.Now.AddDays(-30))
                .ToList();

            // 如果用户从未从这个IP登录过，则可能是可疑位置
            var hasLoggedInFromThisIp = recentSessions.Any(s => s.ClientIp == ipAddress);
            if (!hasLoggedInFromThisIp)
            {
                // 进一步检查IP地址的地理位置变化（这里简化处理）
                var uniqueIps = recentSessions.Select(s => s.ClientIp).Distinct().Count();
                if (uniqueIps > 0) // 用户有登录历史且这是新IP
                {
                    _logger.LogWarning("检测到可疑登录位置 - 用户: {Username}, IP: {IpAddress}", username, ipAddress);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 设置会话扩展数据
        /// </summary>
        public async Task SetSessionExtendedDataAsync(Guid sessionId, string extendedData)
        {
            var sessionModel = await _sessionRepository.GetByIdAsync(sessionId);
            if (sessionModel != null)
            {
                sessionModel.ExtendedData = extendedData;
                await _sessionRepository.UpdateAsync(sessionModel);
            }
        }

        /// <summary>
        /// 强制用户重新登录（管理员操作）
        /// </summary>
        public async Task ForceUserReloginAsync(Guid userId, string reason, Guid operatorId)
        {
            await RevokeAllUserSessionsAsync(userId, $"管理员强制重新登录: {reason}", operatorId);
            _logger.LogInformation("强制用户重新登录 - 用户ID: {UserId}, 操作员ID: {OperatorId}, 原因: {Reason}", 
                userId, operatorId, reason);
        }
    }
}