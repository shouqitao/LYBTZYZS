using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证查询服务 - UltraThink架构
    /// 职责：令牌验证、会话查询、用户状态查询
    /// </summary>
    public class AuthQueryService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IAuthSessionRepository _authSessionRepository;
        private readonly IJwtAuthenticationService _jwtAuthenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthQueryService> _logger;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthQueryService(
            IAuthRepository authRepository,
            IAuthSessionRepository authSessionRepository,
            IJwtAuthenticationService jwtAuthenticationService,
            IMapper mapper,
            ILogger<AuthQueryService> logger,
            SysAdminHandler sysAdminHandler)
        {
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
            _authSessionRepository = authSessionRepository ?? throw new ArgumentNullException(nameof(authSessionRepository));
            _jwtAuthenticationService = jwtAuthenticationService ?? throw new ArgumentNullException(nameof(jwtAuthenticationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sysAdminHandler = sysAdminHandler ?? throw new ArgumentNullException(nameof(sysAdminHandler));
        }

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return ServiceResult<bool>.Failure("Token不能为空");

                // 使用JWT服务验证Token
                var claimsPrincipal = _jwtAuthenticationService.ValidateToken(token);
                
                if (claimsPrincipal == null)
                    return ServiceResult<bool>.Failure("Token无效或已过期");

                await Task.CompletedTask; // 保持异步接口一致性
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token验证失败: {Token}", token?[..Math.Min(token.Length, 20)] + "...");
                return ServiceResult<bool>.Failure("Token验证失败");
            }
        }

        /// <summary>
        /// 获取会话信息
        /// </summary>
        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return ServiceResult<object>.Failure("Token不能为空");

                // 验证Token
                var tokenValidation = await ValidateTokenAsync(token);
                if (!tokenValidation.IsSuccess)
                    return ServiceResult<object>.Failure(tokenValidation.ErrorMessage);

                // 从Token中提取用户ID
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<object>.Failure("无法从Token中提取用户信息");

                // 获取当前用户信息
                var userResult = await GetCurrentUserAsync(userId);
                if (!userResult.IsSuccess)
                    return ServiceResult<object>.Failure(userResult.ErrorMessage);

                // 构建会话信息
                var sessionInfo = new
                {
                    UserId = userId,
                    Username = userResult.Data?.Username,
                    Role = userResult.Data?.Role.ToString(),
                    IsAuthenticated = true,
                    TokenExpiry = DateTime.UtcNow.AddHours(8), // 假设8小时有效期
                    LoginTime = DateTime.UtcNow // 这里应该从实际会话记录中获取
                };

                return ServiceResult<object>.Success(sessionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取会话信息失败");
                return ServiceResult<object>.Failure("获取会话信息失败");
            }
        }

        /// <summary>
        /// 根据用户ID获取当前用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetCurrentUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");

                if (!Guid.TryParse(userId, out var userGuid))
                    return ServiceResult<UserDto>.Failure("用户ID格式无效");

                // 检查是否为系统管理员
                if (userId.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                {
                    var sysAdminUser = await _sysAdminHandler.GetSysAdminUserAsync("sysadmin");
                    if (sysAdminUser != null)
                    {
                        var sysAdminDto = _mapper.Map<UserDto>(sysAdminUser);
                        return ServiceResult<UserDto>.Success(sysAdminDto);
                    }
                }

                // 获取普通用户
                var user = await _authRepository.GetByUsernameAsync(userId); // 暂时使用用户名查询
                if (user == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                var userDto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前用户失败: {UserId}", userId);
                return ServiceResult<UserDto>.Failure("获取当前用户失败");
            }
        }

        /// <summary>
        /// 验证刷新令牌
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateRefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return ServiceResult<bool>.Failure("刷新令牌不能为空");

                // 这里应该验证刷新令牌的有效性
                // 可能需要检查数据库中存储的刷新令牌
                // 暂时简单验证格式
                if (refreshToken.Length < 32)
                    return ServiceResult<bool>.Failure("刷新令牌格式无效");

                await Task.CompletedTask; // 保持异步接口一致性
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证刷新令牌失败");
                return ServiceResult<bool>.Failure("验证刷新令牌失败");
            }
        }

        /// <summary>
        /// 检查会话过期状态
        /// </summary>
        public async Task<ServiceResult<bool>> CheckSessionExpirationAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return ServiceResult<bool>.Failure("Token不能为空");

                // 验证Token并检查过期时间
                var claimsPrincipal = _jwtAuthenticationService.ValidateToken(token);
                
                await Task.CompletedTask; // 保持异步接口一致性
                return ServiceResult<bool>.Success(claimsPrincipal != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查会话过期状态失败");
                return ServiceResult<bool>.Failure("检查会话过期状态失败");
            }
        }

        /// <summary>
        /// 验证会话有效性
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateSessionAsync(string token)
        {
            try
            {
                // 1. 验证Token格式和签名
                var tokenValidation = await ValidateTokenAsync(token);
                if (!tokenValidation.IsSuccess)
                    return tokenValidation;

                // 2. 检查会话是否过期
                var expirationCheck = await CheckSessionExpirationAsync(token);
                if (!expirationCheck.IsSuccess)
                    return expirationCheck;

                // 3. 可选：检查用户状态是否仍然有效
                var userId = ExtractUserIdFromToken(token);
                if (!string.IsNullOrEmpty(userId))
                {
                    var userResult = await GetCurrentUserAsync(userId);
                    if (!userResult.IsSuccess)
                        return ServiceResult<bool>.Failure("用户状态无效");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证会话有效性失败");
                return ServiceResult<bool>.Failure("验证会话有效性失败");
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 从Token中提取用户ID
        /// </summary>
        private string? ExtractUserIdFromToken(string token)
        {
            try
            {
                // 使用JWT服务提取用户ID
                // 这里需要根据实际的JWT实现来提取Claims
                // 暂时返回空，需要在实际JWT服务中实现
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Token提取用户ID失败");
                return null;
            }
        }

        #endregion
    }
}