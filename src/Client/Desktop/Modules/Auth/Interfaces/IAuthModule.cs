using System;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Auth.Interfaces;

/// <summary>
/// 认证模块统一接口 - UltraThink三层架构模块入口
/// 继承IAuthenticationService保持兼容性，同时提供完整的三层架构接口访问
/// </summary>
/// <summary>
/// 认证模块主接口 - UltraThink前端简化版，对应后端实际API
/// 职责：统一服务入口，纯委托模式，无业务逻辑
/// 移除过度开发功能，仅保留后端支持的基本认证功能
/// </summary>
public interface IAuthModule : IAuthenticationService
{
    #region 基础认证操作 - 对应后端AuthController实际API
    
    /// <summary>
    /// 用户登录 (对应 POST /auth/login)
    /// </summary>
    new Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest);
    
    /// <summary>
    /// 用户登出 (对应 POST /auth/logout)
    /// </summary>
    Task<ServiceResult> LogoutAsync(LogoutRequest logoutRequest);
    
    /// <summary>
    /// 刷新Token (对应 POST /auth/refresh)
    /// </summary>
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// 验证Token (对应 POST /auth/validate)
    /// </summary>
    Task<ServiceResult<bool>> ValidateTokenAsync(string token);
    
    /// <summary>
    /// 修改系统管理员密码 (对应 POST /auth/changeSysAdminPassword)
    /// </summary>
    Task<ServiceResult> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request);
    
    #endregion
    
    #region 基础状态管理 - 简化版本
    
    /// <summary>
    /// 认证状态查询
    /// </summary>
    new bool IsLoggedIn { get; }
    
    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    new Task<ServiceResult<UserDto?>> GetCurrentUserAsync();
    
    /// <summary>
    /// Token管理
    /// </summary>
    new string? GetToken();
    void SetToken(string token);
    void ClearToken();
    
    /// <summary>
    /// 清除认证状态
    /// </summary>
    void ClearAuthenticationState();
    
    #endregion
}