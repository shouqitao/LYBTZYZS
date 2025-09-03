using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 认证业务服务 - UltraThink双层架构统一API版
/// 职责：基础认证操作，使用统一API客户端管理器
/// </summary>
public class AuthBusinessService(
    ILogger<AuthBusinessService> logger,
    IAuthApi authApi) : IAuthBusinessService
{
    private readonly ILogger<AuthBusinessService> _logger = logger;
    private readonly IAuthApi _authApi = authApi;

    #region 基础认证流程 - 统一API实现

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)
    {
        try
        {
            var response = await _authApi.LoginAsync(loginRequest);
            
            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("用户登录成功: {Username}", loginRequest.Username);
                return ServiceResult<LoginResponse>.Success(response.Data);
            }
            
            _logger.LogWarning("用户登录失败: {Username}, 消息: {Message}", 
                loginRequest.Username, response.Message);
            return ServiceResult<LoginResponse>.Failure("登录失败，请检查用户名和密码");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录异常: {Username}", loginRequest.Username);
            return ServiceResult<LoginResponse>.Failure($"登录过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    public async Task<ServiceResult> LogoutAsync()
    {
        try
        {
            var response = await _authApi.LogoutAsync();
            
            if (response.Success)
            {
                _logger.LogInformation("用户登出成功");
                return ServiceResult.Success("登出成功");
            }
            
            _logger.LogWarning("用户登出失败, 消息: {Message}", response.Message);
            return ServiceResult.Failure("登出失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登出异常");
            return ServiceResult.Failure($"登出过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// Token刷新
    /// </summary>
    public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync()
    {
        try
        {
            var response = await _authApi.RefreshTokenAsync();
            
            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Token刷新成功");
                return ServiceResult<LoginResponse>.Success(response.Data);
            }
            
            _logger.LogWarning("Token刷新失败, 消息: {Message}", response.Message);
            return ServiceResult<LoginResponse>.Failure("Token刷新失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token刷新异常");
            return ServiceResult<LoginResponse>.Failure($"Token刷新过程发生错误: {ex.Message}");
        }
    }

    #endregion
}