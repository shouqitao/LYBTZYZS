using System;
using System.Threading.Tasks;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// Auth模块 - UltraThink双层架构纯委托层
/// 职责：统一服务入口，请求路由分发
/// 现已实现共享IAuthService接口，与后端完全对齐
/// </summary>
public class AuthModule(
    IAuthQueryService queryService,
    IAuthBusinessService businessService) : IAuthModule, IAuthService, IAuthenticationService
{
    private readonly IAuthQueryService _queryService = queryService;
    private readonly IAuthBusinessService _businessService = businessService;
    #region 基础认证操作 - 对应后端AuthController实际API

    /// <summary>
    /// 用户登录 (对应 POST /auth/login)
    /// </summary>
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)
        => await _businessService.LoginAsync(loginRequest);

    /// <summary>
    /// 用户登出 (对应 POST /auth/logout) - IAuthModule版本
    /// </summary>
    public async Task<ServiceResult> LogoutAsync(LogoutRequest logoutRequest)
    {
        return await LogoutAsync(); // 委托给无参数版本
    }
    
    /// <summary>
    /// 用户登出 (对应 POST /auth/logout) - IAuthService版本
    /// </summary>
    async Task<ServiceResult<bool>> IAuthService.LogoutAsync(LogoutRequest logoutRequest)
    {
        var result = await LogoutAsync(); // 委托给无参数版本
        return result.IsSuccess ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(result.ErrorMessage ?? "登出失败");
    }

    /// <summary>
    /// 刷新Token (对应 POST /auth/refresh)
    /// </summary>
    public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        => await _businessService.RefreshTokenAsync();

    /// <summary>
    /// 验证Token (对应 POST /auth/validate)
    /// </summary>
    public Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        => Task.FromResult(ServiceResult<bool>.Success(false)); // 简单诊所版本简化实现

    /// <summary>
    /// 修改系统管理员密码 (对应 POST /auth/changeSysAdminPassword) - IAuthModule版本
    /// </summary>
    public Task<ServiceResult> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        => Task.FromResult(ServiceResult.Failure("简单诊所版本暂不支持密码修改"));
        
    /// <summary>
    /// 修改系统管理员密码 (对应 POST /auth/changeSysAdminPassword) - IAuthService版本
    /// </summary>
    Task<ServiceResult<bool>> IAuthService.ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        => Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持密码修改"));

    /// <summary>
    /// 验证用户凭据 - IAuthService接口实现
    /// </summary>
    public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
    {
        var loginResult = await LoginAsync(request);
        if (loginResult.IsSuccess && loginResult.Data != null)
            return ServiceResult<string>.Success(loginResult.Data.Token);
        return ServiceResult<string>.Failure(loginResult.ErrorMessage ?? "凭据验证失败");
    }

    /// <summary>
    /// 获取用户会话信息 - IAuthService接口实现
    /// </summary>
    public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
    {
        var userResult = await GetCurrentUserAsync();
        if (userResult.IsSuccess && userResult.Data != null)
            return ServiceResult<object>.Success(userResult.Data);
        return ServiceResult<object>.Failure("无法获取会话信息");
    }

    #endregion

    #region 基础状态管理 - 简化版本

    /// <summary>
    /// 认证状态查询
    /// </summary>
    public bool IsLoggedIn => _queryService.IsLoggedIn;

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    public async Task<ServiceResult<UserDto?>> GetCurrentUserAsync()
        => await _queryService.GetCurrentUserAsync();

    /// <summary>
    /// Token管理
    /// </summary>
    public string? GetToken() => null; // 简化实现

    public void SetToken(string token) 
    {
        // 简化实现 - 不保存token
    }

    public void ClearToken() 
    {
        // 简化实现
    }

    /// <summary>
    /// 清除认证状态
    /// </summary>
    public void ClearAuthenticationState() 
    {
        // 简化实现
    }

    #endregion

    #region IAuthenticationService兼容方法
    
    public void ClearAuthInfo() => ClearAuthenticationState();

    public async Task<bool> CheckConnectionAsync()
        => (await _queryService.CheckConnectionAsync()).IsSuccess;

    /// <summary>
    /// IAuthenticationService.LogoutAsync() - 无参数版本
    /// </summary>
    public async Task<ServiceResult> LogoutAsync()
        => await _businessService.LogoutAsync();

    /// <summary>
    /// IAuthenticationService.GetCurrentUserAsync() - 返回UserDto?
    /// </summary>
    async Task<UserDto?> IAuthenticationService.GetCurrentUserAsync()
    {
        var result = await _queryService.GetCurrentUserAsync();
        return result.IsSuccess ? result.Data : null;
    }

    #endregion
    #region 简化的不支持方法（UltraThink简化版）

    public Task<ServiceResult<LoginResponse>> AutoLoginAsync()
    {
        return Task.FromResult(ServiceResult<LoginResponse>.Failure("简单诊所版本不支持自动登录功能"));
    }

    public Task<ServiceResult<bool>> SilentReauthenticationAsync()
    {
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本不支持静默重新认证功能"));
    }

    #endregion
    #region 资源清理

    public void Dispose()
    {
        // 清理资源，当前无需特殊清理操作
        GC.SuppressFinalize(this);
    }

    #endregion
}