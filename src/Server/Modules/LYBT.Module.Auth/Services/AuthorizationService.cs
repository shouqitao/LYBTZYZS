using LYBT.Module.Auth.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LYBT.Module.Auth.Services
{

    /// <summary>
    /// 授权服务实现
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {

        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        public bool HasPermission(ClaimsPrincipal principal, string permission)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            // 检查权限声明
            return principal.HasClaim("permission", permission);
        }

        /// <summary>
        /// 检查用户是否有指定角色
        /// </summary>
        public bool HasRole(ClaimsPrincipal principal, string role)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            return principal.IsInRole(role);
        }

        /// <summary>
        /// 检查用户是否有任一指定角色
        /// </summary>
        public bool HasAnyRole(ClaimsPrincipal principal, params string[] roles)
        {
            if (principal?.Identity?.IsAuthenticated != true || roles.Length == 0)
            {
                return false;
            }

            return roles.Any(role => principal.IsInRole(role));
        }

        /// <summary>
        /// 检查用户是否拥有所有指定角色
        /// </summary>
        public bool HasAllRoles(ClaimsPrincipal principal, params string[] roles)
        {
            if (principal?.Identity?.IsAuthenticated != true || roles.Length == 0)
            {
                return false;
            }

            return roles.All(role => principal.IsInRole(role));
        }

        /// <summary>
        /// 获取用户ID
        /// </summary>
        public string? GetUserId(ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                   principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// 获取用户名
        /// </summary>
        public string? GetUserName(ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ??
                   principal.FindFirst(ClaimTypes.Name)?.Value ??
                   principal.Identity.Name;
        }

        /// <summary>
        /// 获取用户角色列表
        /// </summary>
        public IEnumerable<string> GetUserRoles(ClaimsPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return Enumerable.Empty<string>();
            }

            return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        }
    }
}