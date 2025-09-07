using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using AuthContracts = LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 认证服务适配器 - 解决DT-001服务接口职责混乱问题
/// 
/// 架构职责: 将IAuthService业务API接口适配为IAuthenticationService前端认证接口
/// 设计模式: 适配器模式 (Adapter Pattern)
/// 优化效果: 
/// - 职责分离: AuthModule专注IAuthService业务API，适配器专注UI认证
/// - 降低耦合: UI层只依赖IAuthenticationService，不直接依赖业务API
/// - 简化接口: 消除双接口实现的复杂适配逻辑
/// 
/// 适用场景: 小型诊所20人以下场景，简化认证流程
/// </summary>
public class AuthServiceAdapter(IAuthService authService) : IAuthenticationService
{
    private readonly IAuthService _authService = authService ?? throw new ArgumentNullException(nameof(authService));

    #region 核心认证操作适配

    /// <summary>
    /// 用户登录 - 适配器模式实现
    /// 将IAuthService.LoginAsync适配为IAuthenticationService要求的签名
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <returns>登录响应</returns>
    public async Task<ServiceResult<AuthContracts.LoginResponse>> LoginAsync(AuthContracts.LoginRequest request)
        => await _authService.LoginAsync(request);

    /// <summary>
    /// 用户登出 - 接口适配
    /// 将IAuthService带参数的LogoutAsync适配为无参数版本
    /// </summary>
    /// <returns>登出操作结果</returns>
    public async Task<ServiceResult> LogoutAsync()
    {
        // 创建空的登出请求对象
        var logoutRequest = new AuthContracts.LogoutRequest();
        var result = await _authService.LogoutAsync(logoutRequest);
        
        // 将ServiceResult<bool>转换为ServiceResult
        return result.IsSuccess 
            ? ServiceResult.Success("登出成功")
            : ServiceResult.Failure(result.ErrorMessage ?? "登出失败");
    }

    #endregion 核心认证操作适配

    #region 状态查询适配

    /// <summary>
    /// 检查登录状态 - 简化实现
    /// 小型诊所版本: 暂时返回false，后续可根据需要完善
    /// </summary>
    public bool IsLoggedIn => false; // 简化实现

    /// <summary>
    /// 获取当前用户信息 - 简化实现
    /// 小型诊所版本: 暂时返回null，后续可根据需要完善
    /// </summary>
    /// <returns>当前用户信息</returns>
    public Task<UserDto?> GetCurrentUserAsync()
        => Task.FromResult<UserDto?>(null); // 简化实现

    /// <summary>
    /// 获取JWT令牌 - 简化实现
    /// 小型诊所版本: 暂不实现令牌存储
    /// </summary>
    /// <returns>JWT令牌</returns>
    public string? GetToken() => null; // 简化实现

    /// <summary>
    /// 清除认证信息 - 简化实现
    /// 小型诊所版本: 无需复杂的状态清理
    /// </summary>
    public void ClearAuthInfo()
    {
        // 简化实现 - 无需特殊操作
    }

    #endregion 状态查询适配

    #region 连接检查适配

    /// <summary>
    /// 检查API连接状态 - 基于会话信息判断
    /// 通过尝试获取会话信息来判断API连接是否正常
    /// </summary>
    /// <returns>连接状态</returns>
    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            // 使用虚拟token测试连接
            var result = await _authService.GetSessionInfoAsync("test-token");
            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    #endregion 连接检查适配
}
