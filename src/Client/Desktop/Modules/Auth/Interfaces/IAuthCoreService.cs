using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Auth.Interfaces;

/// <summary>
/// 认证核心服务接口 - UltraThink三层架构核心操作层
/// 职责：API通信、基础认证操作、Token管理、数据验证
/// </summary>
public interface IAuthCoreService
{
    #region API通信操作
    
    /// <summary>
    /// 调用登录API
    /// </summary>
    Task<ServiceResult<LoginResponse>> CallLoginApiAsync(LoginRequest loginRequest);
    
    /// <summary>
    /// 调用登出API
    /// </summary>
    Task<ServiceResult> CallLogoutApiAsync();
    
    /// <summary>
    /// 调用Token刷新API
    /// </summary>
    Task<ServiceResult<LoginResponse>> CallRefreshTokenApiAsync();
    
    /// <summary>
    /// 检查API健康状态
    /// </summary>
    Task<ServiceResult<bool>> CheckApiHealthAsync();
    
    #endregion
    
    #region Token管理操作
    
    /// <summary>
    /// 获取当前Token
    /// </summary>
    string? GetToken();
    
    /// <summary>
    /// 设置Token
    /// </summary>
    void SetToken(string token);
    
    /// <summary>
    /// 清除Token
    /// </summary>
    void ClearToken();
    
    /// <summary>
    /// 验证Token格式
    /// </summary>
    ServiceResult ValidateToken(string? token);
    
    #endregion
    
    #region 基础数据验证
    
    /// <summary>
    /// 验证登录请求数据
    /// </summary>
    ServiceResult ValidateLoginRequest(LoginRequest loginRequest);
    
    /// <summary>
    /// 验证用户名格式
    /// </summary>
    ServiceResult ValidateUsername(string? username);
    
    /// <summary>
    /// 验证密码格式
    /// </summary>
    ServiceResult ValidatePassword(string? password);
    
    /// <summary>
    /// 验证用户认证状态
    /// </summary>
    ServiceResult<bool> ValidateAuthenticationState(UserDto? user, string? token);
    
    #endregion
    
    #region 认证状态管理
    
    /// <summary>
    /// 更新认证状态
    /// </summary>
    void UpdateAuthenticationState(bool isAuthenticated, UserDto? user, LoginResponse? loginResponse);
    
    /// <summary>
    /// 清除认证状态
    /// </summary>
    void ClearAuthenticationState();
    
    /// <summary>
    /// 获取认证状态
    /// </summary>
    ServiceResult<(bool IsAuthenticated, UserDto? User, LoginResponse? LoginResponse)> GetAuthenticationState();
    
    #endregion
    
    #region 缓存和性能优化
    
    /// <summary>
    /// 预热认证缓存
    /// </summary>
    Task<ServiceResult> PreWarmAuthCacheAsync();
    
    /// <summary>
    /// 清除认证缓存
    /// </summary>
    ServiceResult ClearAuthCache();
    
    #endregion
}