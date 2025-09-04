using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Interfaces.Repositories;
using LYBT.Module.Auth.Interfaces;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Module.Auth.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 认证查询服务 - UltraThink架构
    /// 职责：用户查询、Token验证、会话信息获取
    /// </summary>
    public class AuthQueryService : IAuthQueryService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtAuthenticationService _jwtAuthenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthQueryService> _logger;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthQueryService(
            IAuthRepository authRepository,
            IJwtAuthenticationService jwtAuthenticationService,
            IMapper mapper,
            ILogger<AuthQueryService> logger,
            SysAdminHandler sysAdminHandler)
        {
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
            _jwtAuthenticationService = jwtAuthenticationService ?? throw new ArgumentNullException(nameof(jwtAuthenticationService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sysAdminHandler = sysAdminHandler ?? throw new ArgumentNullException(nameof(sysAdminHandler));
        }

        /// <summary>
        /// 根据用户名获取用户信息（用于认证）
        /// </summary>
        public async Task<ServiceResult<User>> GetUserForAuthenticationAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<User>.Failure("用户名不能为空");
                }

                // 检查是否为系统管理员
                if (username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase))
                {
                    var sysAdminUser = await _sysAdminHandler.GetSysAdminUserAsync("sysadmin");
                    if (sysAdminUser != null)
                    {
                        return ServiceResult<User>.Success(sysAdminUser);
                    }
                }

                // 查询普通用户
                var user = await _authRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    return ServiceResult<User>.Failure("用户不存在");
                }

                return ServiceResult<User>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户认证信息失败: {Username}", username);
                return ServiceResult<User>.Failure("获取用户信息失败");
            }
        }

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return ServiceResult<bool>.Failure("Token不能为空");
                }

                // 使用JWT服务验证Token
                var claimsPrincipal = _jwtAuthenticationService.ValidateToken(token);
                
                await Task.CompletedTask; // 保持异步接口一致性
                return ServiceResult<bool>.Success(claimsPrincipal != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token验证失败");
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
                {
                    return ServiceResult<object>.Failure("Token不能为空");
                }

                // 验证Token
                var tokenValidation = await ValidateTokenAsync(token);
                if (!tokenValidation.IsSuccess)
                {
                    return ServiceResult<object>.Failure("Token验证失败");
                }

                // 从Token中提取用户ID
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<object>.Failure("无法从Token中提取用户信息");
                }

                // 获取当前用户信息
                var userResult = await GetCurrentUserAsync(userId);
                if (!userResult.IsSuccess)
                {
                    return ServiceResult<object>.Failure("获取用户信息失败");
                }

                // 构建会话信息
                var sessionInfo = new
                {
                    UserId = userId,
                    Username = userResult.Data?.Username,
                    Role = userResult.Data?.Role,
                    IsAuthenticated = true,
                    TokenExpiry = DateTime.UtcNow.AddHours(8),
                    LoginTime = DateTime.UtcNow
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
                {
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");
                }

                if (!Guid.TryParse(userId, out var userGuid))
                {
                    return ServiceResult<UserDto>.Failure("用户ID格式无效");
                }

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
                var user = await _authRepository.GetByUsernameAsync(userId);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

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
        /// 从Token中提取用户ID
        /// </summary>
        public string ExtractUserIdFromToken(string token)
        {
            try
            {
                var claimsPrincipal = _jwtAuthenticationService.ValidateToken(token);
                return claimsPrincipal?.FindFirst("sub")?.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Token中提取用户ID失败");
                return string.Empty;
            }
        }
    }
}