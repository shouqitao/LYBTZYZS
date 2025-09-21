using System.Security.Claims;

namespace LYBT.Shared.Utilities.Security
{
    /// <summary>
    /// Claims管理帮助类
    /// </summary>
    public static class ClaimsHelper
    {
        /// <summary>
        /// 创建标准化的Claims列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="username">用户名</param>
        /// <param name="role">角色</param>
        /// <param name="additionalClaims">额外的Claims</param>
        /// <returns>Claims列表</returns>
        public static List<Claim> CreateClaims(
            string userId,
            string username,
            string role,
            Dictionary<string, string>? additionalClaims = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, username),
                new("sub", userId),
                new("unique_name", username),
                new("jti", Guid.NewGuid().ToString()),
                new("iat", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // 规范化并添加角色Claim
            var normalizedRole = RoleHelper.NormalizeRole(role);
            claims.Add(new Claim(ClaimTypes.Role, normalizedRole));

            // 添加额外的Claims
            if (additionalClaims != null)
            {
                foreach (var (key, value) in additionalClaims)
                {
                    claims.Add(new Claim(key, value));
                }
            }

            return claims;
        }

        /// <summary>
        /// 从ClaimsPrincipal提取用户ID
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <returns>用户ID</returns>
        public static string? GetUserId(ClaimsPrincipal? principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return null;

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                   principal.FindFirst("sub")?.Value;
        }

        /// <summary>
        /// 从ClaimsPrincipal提取用户名
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <returns>用户名</returns>
        public static string? GetUsername(ClaimsPrincipal? principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return null;

            return principal.FindFirst(ClaimTypes.Name)?.Value ??
                   principal.FindFirst("unique_name")?.Value;
        }

        /// <summary>
        /// 从ClaimsPrincipal提取角色
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <returns>角色名称</returns>
        public static string? GetRole(ClaimsPrincipal? principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return null;

            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
            return RoleHelper.NormalizeRole(roleClaim);
        }

        /// <summary>
        /// 检查用户是否具有指定角色
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <param name="role">要检查的角色</param>
        /// <returns>是否具有该角色</returns>
        public static bool HasRole(ClaimsPrincipal? principal, string role)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return false;

            var userRole = GetRole(principal);
            var targetRole = RoleHelper.NormalizeRole(role);

            return string.Equals(userRole, targetRole, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查用户是否具有任一指定角色
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <param name="roles">要检查的角色列表</param>
        /// <returns>是否具有任一角色</returns>
        public static bool HasAnyRole(ClaimsPrincipal? principal, params string[] roles)
        {
            return roles.Any(role => HasRole(principal, role));
        }

        /// <summary>
        /// 检查用户是否为管理员
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <returns>是否为管理员</returns>
        public static bool IsAdmin(ClaimsPrincipal? principal)
        {
            return HasRole(principal, RoleHelper.Roles.Admin);
        }

        /// <summary>
        /// 检查用户是否为医生
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <returns>是否为医生</returns>
        public static bool IsDoctor(ClaimsPrincipal? principal)
        {
            return HasRole(principal, RoleHelper.Roles.Doctor);
        }

        /// <summary>
        /// 获取Claim值
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <param name="claimType">Claim类型</param>
        /// <returns>Claim值</returns>
        public static string? GetClaimValue(ClaimsPrincipal? principal, string claimType)
        {
            if (principal?.Identity?.IsAuthenticated != true)
                return null;

            return principal.FindFirst(claimType)?.Value;
        }

        /// <summary>
        /// 获取所有Claims作为字典
        /// </summary>
        /// <param name="principal">ClaimsPrincipal</param>
        /// <returns>Claims字典</returns>
        public static Dictionary<string, string> GetClaimsAsDictionary(ClaimsPrincipal? principal)
        {
            var result = new Dictionary<string, string>();

            if (principal?.Identity?.IsAuthenticated != true)
                return result;

            foreach (var claim in principal.Claims)
            {
                // 如果有重复的claim类型，只保留第一个
                if (!result.ContainsKey(claim.Type))
                {
                    result[claim.Type] = claim.Value;
                }
            }

            return result;
        }
    }
}