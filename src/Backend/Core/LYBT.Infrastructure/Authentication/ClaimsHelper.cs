using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LYBT.Infrastructure.Authentication
{

    /// <summary>
    /// 声明助手类
    /// </summary>
    public static class ClaimsHelper
    {

        /// <summary>
        /// 创建用户基本声明
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="userName">用户名</param>
        /// <returns>声明列表</returns>
        public static List<Claim> CreateBasicClaims(string userId, string userName)
        {
            return new List<Claim> {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.UniqueName, userName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };
        }

        /// <summary>
        /// 添加角色声明
        /// </summary>
        /// <param name="claims">现有声明列表</param>
        /// <param name="roles">角色列表</param>
        public static void AddRoleClaims(List<Claim> claims, IEnumerable<string> roles)
        {
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        /// <summary>
        /// 添加权限声明
        /// </summary>
        /// <param name="claims">现有声明列表</param>
        /// <param name="permissions">权限列表</param>
        public static void AddPermissionClaims(List<Claim> claims, IEnumerable<string> permissions)
        {
            claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        }

        /// <summary>
        /// 添加自定义声明
        /// </summary>
        /// <param name="claims">现有声明列表</param>
        /// <param name="claimType">声明类型</param>
        /// <param name="claimValue">声明值</param>
        public static void AddCustomClaim(List<Claim> claims, string claimType, string claimValue)
        {
            if (!string.IsNullOrEmpty(claimType) && !string.IsNullOrEmpty(claimValue))
            {
                claims.Add(new Claim(claimType, claimValue));
            }
        }

        /// <summary>
        /// 从主体中提取用户ID
        /// </summary>
        /// <param name="principal">用户主体</param>
        /// <returns>用户ID</returns>
        public static string? ExtractUserId(ClaimsPrincipal principal)
        {
            return principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                   principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// 从主体中提取用户名
        /// </summary>
        /// <param name="principal">用户主体</param>
        /// <returns>用户名</returns>
        public static string? ExtractUserName(ClaimsPrincipal principal)
        {
            return principal?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ??
                   principal?.FindFirst(ClaimTypes.Name)?.Value ??
                   principal?.Identity?.Name;
        }

        /// <summary>
        /// 从主体中提取角色列表
        /// </summary>
        /// <param name="principal">用户主体</param>
        /// <returns>角色列表</returns>
        public static IEnumerable<string> ExtractRoles(ClaimsPrincipal principal)
        {
            return principal?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// 从主体中提取权限列表
        /// </summary>
        /// <param name="principal">用户主体</param>
        /// <returns>权限列表</returns>
        public static IEnumerable<string> ExtractPermissions(ClaimsPrincipal principal)
        {
            return principal?.FindAll("permission")?.Select(c => c.Value) ?? Enumerable.Empty<string>();
        }
    }
}