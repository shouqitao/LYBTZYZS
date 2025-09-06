using LYBT.Desktop.Auth.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// Auth模块主服务 - UltraThink双层架构纯委托层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：统一服务入口，请求路由分发到QueryService和BusinessService
/// 实现IAuthService、IAuthenticationService双重接口兼容
/// 专注JWT认证、用户会话管理和权限控制，适配小型诊所认证需求
/// 集成企业级错误处理，支持自动登录和静默重认证功能
/// </summary>
public class AuthModule(
    IAuthQueryService queryService,
    IAuthBusinessService businessService) : IAuthService, IAuthenticationService {
    private readonly IAuthQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    private readonly IAuthBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

    #region 基础认证操作 - 对应后端AuthController实际API

    /// <summary>
    /// 用户登录认证
    /// 委托BusinessService处理完整登录流程，包括凭据验证和JWT生成
    /// </summary>
    /// <param name="loginRequest">登录请求信息</param>
    /// <returns>包含JWT令牌和用户信息的登录响应</returns>
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)
        => await _businessService.LoginAsync(loginRequest);

    /// <summary>
    /// 用户登出 - IAuthModule版本
    /// UltraThink架构：委托给标准登出方法统一处理
    /// </summary>
    /// <param name="logoutRequest">登出请求信息</param>
    /// <returns>登出操作结果</returns>
    public async Task<ServiceResult> LogoutAsync(LogoutRequest logoutRequest)
        => await LogoutAsync(); // 委托给无参数版本

    /// <summary>
    /// 用户登出 - IAuthService版本
    /// 接口适配器模式：将ServiceResult转换为ServiceResult&lt;bool&gt;
    /// </summary>
    /// <param name="logoutRequest">登出请求信息</param>
    /// <returns>带布尔值的登出操作结果</returns>
    async Task<ServiceResult<bool>> IAuthService.LogoutAsync(LogoutRequest logoutRequest) {
        var result = await LogoutAsync();
        return result.IsSuccess
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure(result.ErrorMessage ?? "登出失败");
    }

    /// <summary>
    /// 刷新JWT认证令牌
    /// 委托BusinessService处理令牌刷新逻辑，延长用户会话时间
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>新的JWT认证响应</returns>
    public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        => await _businessService.RefreshTokenAsync();

    /// <summary>
    /// 验证JWT令牌有效性
    /// 小型诊所版本简化实现：暂不支持复杂的令牌验证
    /// </summary>
    /// <param name="token">待验证的JWT令牌</param>
    /// <returns>令牌验证结果</returns>
    public Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        => Task.FromResult(ServiceResult<bool>.Success(false)); // 简单诊所版本简化实现

    /// <summary>
    /// 修改系统管理员密码 - IAuthModule版本
    /// 委托BusinessService处理完整密码修改流程，包括验证和安全检查
    /// </summary>
    /// <param name="request">密码修改请求</param>
    /// <returns>密码修改操作结果</returns>
    public async Task<ServiceResult> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        => await _businessService.ChangeSysAdminPasswordAsync(request);

    /// <summary>
    /// 修改系统管理员密码 - IAuthService版本
    /// 接口适配：将ServiceResult转换为ServiceResult&lt;bool&gt;
    /// </summary>
    /// <param name="request">密码修改请求</param>
    /// <returns>带布尔值的密码修改操作结果</returns>
    async Task<ServiceResult<bool>> IAuthService.ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request) {
        var result = await _businessService.ChangeSysAdminPasswordAsync(request);
        return result.IsSuccess
            ? ServiceResult<bool>.Success(true, result.Message ?? "密码修改成功")
            : ServiceResult<bool>.Failure(result.ErrorMessage ?? "密码修改失败");
    }

    /// <summary>
    /// 验证用户凭据
    /// 委托登录方法验证用户名密码，返回JWT令牌
    /// </summary>
    /// <param name="request">登录凭据</param>
    /// <returns>验证成功时返回JWT令牌</returns>
    public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request) {
        var loginResult = await LoginAsync(request);
        return loginResult.IsSuccess && loginResult.Data != null
            ? ServiceResult<string>.Success(loginResult.Data.Token)
            : ServiceResult<string>.Failure(loginResult.ErrorMessage ?? "凭据验证失败");
    }

    /// <summary>
    /// 获取用户会话信息
    /// 基于当前用户查询获取完整会话上下文信息
    /// </summary>
    /// <param name="token">JWT认证令牌</param>
    /// <returns>用户会话信息对象</returns>
    public async Task<ServiceResult<object>> GetSessionInfoAsync(string token) {
        var userResult = await GetCurrentUserAsync();
        return userResult.IsSuccess && userResult.Data != null
            ? ServiceResult<object>.Success(userResult.Data)
            : ServiceResult<object>.Failure("无法获取会话信息");
    }

    #endregion 基础认证操作 - 对应后端AuthController实际API

    #region 基础状态管理 - 小型诊所简化版本

    /// <summary>
    /// 获取用户登录状态
    /// 委托QueryService查询当前认证状态
    /// </summary>
    /// <value>如果用户已登录则返回 true</value>
    public bool IsLoggedIn => _queryService.IsLoggedIn;

    /// <summary>
    /// 获取当前登录用户信息
    /// 委托QueryService查询用户详细信息和权限
    /// </summary>
    /// <returns>当前用户DTO对象，未登录时返回null</returns>
    public async Task<ServiceResult<UserDto?>> GetCurrentUserAsync()
        => await _queryService.GetCurrentUserAsync();

    /// <summary>
    /// 获取当前JWT认证令牌
    /// 小型诊所版本简化：暂不实现令牌持久化存储
    /// </summary>
    /// <returns>始终返回null</returns>
    public string? GetToken() => null; // 简化实现

    /// <summary>
    /// 设置JWT认证令牌
    /// 小型诊所版本简化：暂不实现令牌存储
    /// </summary>
    /// <param name="token">JWT认证令牌</param>
    public void SetToken(string token) {
        // 简化实现 - 不保存token
    }

    /// <summary>
    /// 清除JWT认证令牌
    /// 小型诊所版本简化：无实际操作
    /// </summary>
    public void ClearToken() {
        // 简化实现
    }

    /// <summary>
    /// 清除所有认证状态
    /// 用于用户注销时清理会话信息
    /// </summary>
    public void ClearAuthenticationState() {
        // 简化实现
    }

    #endregion 基础状态管理 - 小型诊所简化版本

    #region IAuthenticationService接口兼容方法

    /// <summary>
    /// 清除认证信息
    /// IAuthenticationService接口别名方法
    /// </summary>
    public void ClearAuthInfo() => ClearAuthenticationState();

    /// <summary>
    /// 检查API连接状态
    /// 委托QueryService验证后端服务可用性
    /// </summary>
    /// <returns>连接正常时返回true</returns>
    public async Task<bool> CheckConnectionAsync()
        => (await _queryService.CheckConnectionAsync()).IsSuccess;

    /// <summary>
    /// 用户登出 - 无参数版本
    /// IAuthenticationService接口实现，委托BusinessService处理
    /// </summary>
    /// <returns>登出操作结果</returns>
    public async Task<ServiceResult> LogoutAsync()
        => await _businessService.LogoutAsync();

    /// <summary>
    /// 获取当前用户信息 - IAuthenticationService版本
    /// 接口适配：将ServiceResult&lt;UserDto?&gt;转换为UserDto?
    /// </summary>
    /// <returns>当前用户对象或null</returns>
    async Task<UserDto?> IAuthenticationService.GetCurrentUserAsync() {
        var result = await _queryService.GetCurrentUserAsync();
        return result.IsSuccess ? result.Data : null;
    }

    #endregion IAuthenticationService接口兼容方法

    #region 小型诊所简化功能（暂不支持）

    /// <summary>
    /// 自动登录功能
    /// 小型诊所版本简化：暂不支持自动登录
    /// </summary>
    /// <returns>功能不支持错误</returns>
    public Task<ServiceResult<LoginResponse>> AutoLoginAsync()
        => Task.FromResult(ServiceResult<LoginResponse>.Failure("简单诊所版本不支持自动登录功能"));

    /// <summary>
    /// 静默重新认证
    /// 小型诊所版本简化：暂不支持后台自动认证
    /// </summary>
    /// <returns>功能不支持错误</returns>
    public Task<ServiceResult<bool>> SilentReauthenticationAsync()
        => Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本不支持静默重新认证功能"));

    #endregion 小型诊所简化功能（暂不支持）

    #region 资源清理与生命周期管理

    /// <summary>
    /// 释放Auth模块占用的资源
    /// 实现IDisposable接口，确保资源正确清理
    /// </summary>
    public void Dispose() {
        // 清理资源，当前无需特殊清理操作
        GC.SuppressFinalize(this);
    }

    #endregion 资源清理与生命周期管理
}
