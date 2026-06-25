using System.Security.Claims;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Interfaces;

/// <summary>
/// 简化的JWT服务接口
/// 遵循适度设计原则，仅提供必要的认证功能
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 生成JWT访问令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="role">用户角色</param>
    /// <returns>JWT令牌字符串</returns>
    string GenerateToken(string userId, string userName, UserRole role, string userType = "user");

    /// <summary>
    /// 生成JWT访问令牌（支持额外声明）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="role">用户角色</param>
    /// <param name="additionalClaims">额外的声明</param>
    /// <returns>JWT令牌字符串</returns>
    string GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims, string userType = "user");

    /// <summary>
    /// 验证JWT令牌并返回Claims主体
    /// </summary>
    /// <param name="token">要验证的JWT令牌</param>
    /// <returns>Claims主体，验证失败返回null</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// 刷新JWT令牌 - 接受过期但签名有效的令牌，返回新的登录响应
    /// </summary>
    /// <param name="expiredToken">已过期的JWT令牌</param>
    /// <returns>新的登录响应</returns>
    Result<LoginResponse> RefreshToken(string expiredToken);

    /// <summary>
    /// 验证自动登录令牌 - 接受客户端存储的长生命周期令牌，返回新的登录响应
    /// </summary>
    /// <param name="autoLoginToken">自动登录令牌</param>
    /// <returns>新的登录响应</returns>
    Result<LoginResponse> ValidateAutoLoginToken(string autoLoginToken);
}
