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
/// 
/// DT-001修复: 移除IAuthenticationService接口实现，专注IAuthService业务API
/// 架构优化: 单一职责原则，避免接口职责混乱
/// 适配方案: UI层通过AuthServiceAdapter使用IAuthenticationService
/// 
/// 专注JWT认证、用户会话管理和权限控制，适配小型诊所认证需求
/// 集成企业级错误处理，支持自动登录和静默重认证功能
/// </summary>
public class AuthModule(
    IAuthQueryService queryService,
    IAuthBusinessService businessService) : IAuthService
{
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
    /// 用户登出 - IAuthService接口实现
    /// 委托BusinessService处理完整登出流程
    /// </summary>
    /// <param name="logoutRequest">登出请求信息</param>
    /// <returns>带布尔值的登出操作结果</returns>
    public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest logoutRequest)
    {
        var result = await _businessService.LogoutAsync();
        return result.IsSuccess
            ? ServiceResult<bool>.Success(true, result.Message ?? "登出成功")
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
    /// 修改系统管理员密码 - IAuthService接口实现
    /// 委托BusinessService处理完整密码修改流程，包括验证和安全检查
    /// </summary>
    /// <param name="request">密码修改请求</param>
    /// <returns>带布尔值的密码修改操作结果</returns>
    public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
    {
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
    public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request)
    {
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
    public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
    {
        var userResult = await _queryService.GetCurrentUser();
        return userResult.IsSuccess && userResult.Data != null
            ? ServiceResult<object>.Success(userResult.Data)
            : ServiceResult<object>.Failure("无法获取会话信息");
    }

    #endregion 基础认证操作 - 对应后端AuthController实际API
}
