using System.Security.Claims;

namespace LYBT.Module.Auth.Interfaces
{

    /// <summary>
    /// 授权服务接口
    /// </summary>
    public interface IAuthorizationService
    {

        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        /// <param name="principal">用户主体</param>        /// <param name="permission">权限标识</param>        /// <returns>是否有权限</returns>
        bool HasPermission(ClaimsPrincipal principal, string permission);

        /// <summary>
        /// 检查用户是否有指定角色
        /// </summary>        /// <param name="principal">用户主体</param>        /// <param name="role">角色标识</param>        /// <returns>是否有角色</returns>
        bool HasRole(ClaimsPrincipal principal, string role);

        /// <summary>
        /// 检查用户是否有任一指定角色
        /// </summary>        /// <param name="principal">用户主体</param>        /// <param name="roles">角色列表</param>        /// <returns>是否有任一角色</returns>
        bool HasAnyRole(ClaimsPrincipal principal, params string[] roles);

        /// <summary>
        /// 检查用户是否拥有所有指定角色
        /// </summary>        /// <param name="principal">用户主体</param>        /// <param name="roles">角色列表</param>        /// <returns>是否拥有所有角色</returns>
        bool HasAllRoles(ClaimsPrincipal principal, params string[] roles);

        /// <summary>
        /// 获取用户ID
        /// </summary>        /// <param name="principal">用户主体</param>        /// <returns>用户ID</returns>
        string? GetUserId(ClaimsPrincipal principal);

        /// <summary>
        /// 获取用户名
        /// </summary>        /// <param name="principal">用户主体</param>        /// <returns>用户名</returns>
        string? GetUserName(ClaimsPrincipal principal);

        /// <summary>
        /// 获取用户角色列表
        /// </summary>        /// <param name="principal">用户主体</param>
        /// <returns>角色列表</returns>
        IEnumerable<string> GetUserRoles(ClaimsPrincipal principal);
    }
}
