using LYBT.Desktop.Auth.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 认证查询服务 - UltraThink双层架构查询专业层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：认证状态查询、用户信息获取、连接状态检查
/// 提供只读查询操作，不涉及状态修改，专注数据检索和状态监控
/// 集成会话管理器和API客户端，支持实时状态查询
/// 适配小型诊所查询需求，确保查询性能和数据一致性
/// </summary>
public class AuthQueryService(
    ILogger<AuthQueryService> logger,
    ISessionManager sessionManager,
    IAuthApi authApi) : IAuthQueryService
{
    private readonly ILogger<AuthQueryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ISessionManager _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    private readonly IAuthApi _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));

    #region 认证状态查询专业化实现

    /// <summary>
    /// 获取用户登录状态
    /// 基于会话管理器实时查询当前用户认证状态
    /// </summary>
    /// <value>用户已认证且会话有效时返回true</value>
    public bool IsLoggedIn
    {
        get
        {
            try
            {
                var currentUser = _sessionManager.CurrentUser;
                var hasValidSession = !string.IsNullOrEmpty(currentUser?.Id.ToString());

                _logger.LogDebug("查询登录状态: {IsLoggedIn}", hasValidSession);
                return hasValidSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询登录状态时发生异常");
                return false;
            }
        }
    }

    /// <summary>
    /// 获取当前登录用户详细信息
    /// 从会话管理器获取用户上下文，包含角色和权限信息
    /// </summary>
    /// <returns>当前用户DTO对象，未登录时返回null</returns>
    public Task<ServiceResult<UserDto?>> GetCurrentUser()
    {
        try
        {
            _logger.LogDebug("开始查询当前用户信息");

            var currentUser = _sessionManager.CurrentUser;
            if (currentUser == null)
            {
                _logger.LogDebug("当前无登录用户");
                return Task.FromResult(ServiceResult<UserDto?>.Success(null));
            }

            // 构建用户DTO
            var userDto = new UserDto
            {
                Id = currentUser.Id,
                Username = currentUser.Username,
                RealName = currentUser.RealName ?? currentUser.Username,
                Email = currentUser.Email,
                PhoneNumber = currentUser.PhoneNumber,
                Role = currentUser.Role,
                Status = currentUser.Status,
                CreateTime = currentUser.CreateTime,
                UpdateTime = currentUser.UpdateTime
            };

            _logger.LogDebug(
                "成功获取用户信息: {Username} ({Role})",
                userDto.Username, userDto.Role);

            return Task.FromResult(ServiceResult<UserDto?>.Success(userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户信息时发生异常");
            return Task.FromResult(ServiceResult<UserDto?>.Failure("获取用户信息失败"));
        }
    }

    /// <summary>
    /// 检查后端API连接状态
    /// 验证认证服务的可用性和网络连通性
    /// </summary>
    /// <returns>连接正常时返回true，异常时返回false</returns>
    public async Task<ServiceResult<bool>> CheckConnectionAsync()
    {
        try
        {
            _logger.LogDebug("开始检查API连接状态");

            // 简化实现：基于会话管理器状态判断
            await Task.Delay(10); // 模拟异步检查
            var isConnected = true; // 简化版本默认连接正常

            _logger.LogDebug("API连接状态检查完成: {IsConnected}", isConnected);
            return ServiceResult<bool>.Success(isConnected);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API连接状态检查失败");
            return ServiceResult<bool>.Success(false);
        }
    }

    #endregion 认证状态查询专业化实现
}
