using System.Security.Claims;
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
    string GenerateToken(string userId, string userName, UserRole role);

    /// <summary>
    /// 生成JWT访问令牌（支持额外声明）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="role">用户角色</param>
    /// <param name="additionalClaims">额外的声明</param>
    /// <returns>JWT令牌字符串</returns>
    string GenerateToken(string userId, string userName, UserRole role, Dictionary<string, string> additionalClaims);

    /// <summary>
    /// 验证JWT令牌并返回Claims主体
    /// </summary>
    /// <param name="token">要验证的JWT令牌</param>
    /// <returns>Claims主体，验证失败返回null</returns>
    ClaimsPrincipal? ValidateToken(string token);
}
