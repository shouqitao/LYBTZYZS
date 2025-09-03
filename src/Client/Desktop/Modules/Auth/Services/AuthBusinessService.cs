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
/// 认证业务服务 - UltraThink双层架构业务逻辑层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：处理认证业务逻辑、JWT令牌管理、会话状态控制
/// 集成统一API客户端管理器，提供企业级错误处理和审计日志
/// 支持用户登录认证、安全登出、令牌刷新等核心认证功能
/// 适配小型诊所认证需求，确保系统安全性和用户体验
/// </summary>
public class AuthBusinessService(
    ILogger<AuthBusinessService> logger,
    IAuthApi authApi) : IAuthBusinessService
{
    private readonly ILogger<AuthBusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IAuthApi _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));

    #region 核心认证业务逻辑 - 企业级实现

    /// <summary>
    /// 用户登录认证处理
    /// 执行完整登录业务流程：凭据验证、JWT生成、会话建立、审计记录
    /// </summary>
    /// <param name="loginRequest">登录请求，包含用户名和密码</param>
    /// <returns>包含JWT令牌和用户信息的登录响应</returns>
    /// <exception cref="ArgumentNullException">当登录请求为空时抛出</exception>
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)
    {
        ArgumentNullException.ThrowIfNull(loginRequest, nameof(loginRequest));

        try
        {
            _logger.LogInformation("开始处理用户登录: {Username}", loginRequest.Username);
            
            var response = await _authApi.LoginAsync(loginRequest);
            
            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("用户登录成功: {Username}, 角色: {Role}", 
                    loginRequest.Username, response.Data.User?.Role);
                return ServiceResult<LoginResponse>.Success(response.Data);
            }
            
            _logger.LogWarning("用户登录失败: {Username}, 错误: {Message}", 
                loginRequest.Username, response.Message);
            return ServiceResult<LoginResponse>.Failure("登录失败，请检查用户名和密码");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录过程发生异常: {Username}", loginRequest.Username);
            return ServiceResult<LoginResponse>.Failure($"登录过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 用户安全登出处理
    /// 执行完整登出业务流程：会话清理、令牌失效、状态重置、审计记录
    /// </summary>
    /// <returns>登出操作结果</returns>
    public async Task<ServiceResult> LogoutAsync()
    {
        try
        {
            _logger.LogInformation("开始处理用户登出");
            
            var response = await _authApi.LogoutAsync();
            
            if (response.Success)
            {
                _logger.LogInformation("用户登出成功，会话已清理");
                return ServiceResult.Success("登出成功");
            }
            
            _logger.LogWarning("用户登出失败: {Message}", response.Message);
            return ServiceResult.Failure(response.Message ?? "登出失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登出过程发生异常");
            return ServiceResult.Failure($"登出过程发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// JWT认证令牌刷新处理
    /// 延长用户会话时间，避免频繁重新登录，提升用户体验
    /// </summary>
    /// <returns>包含新JWT令牌的认证响应</returns>
    public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync()
    {
        try
        {
            _logger.LogInformation("开始处理JWT令牌刷新");
            
            var response = await _authApi.RefreshTokenAsync();
            
            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("JWT令牌刷新成功，会话已延长");
                return ServiceResult<LoginResponse>.Success(response.Data);
            }
            
            _logger.LogWarning("JWT令牌刷新失败: {Message}", response.Message);
            return ServiceResult<LoginResponse>.Failure(response.Message ?? "令牌刷新失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT令牌刷新过程发生异常");
            return ServiceResult<LoginResponse>.Failure($"令牌刷新过程发生错误: {ex.Message}");
        }
    }

    #endregion
}