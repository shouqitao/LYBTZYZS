using System.Security.Claims;
using LYBT.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// Claims 提取和角色映射辅助类
/// 供 WebAPI 和 LocalWebAPI 共享
/// </summary>
public static class BaseClaimsHelper
{
    /// <summary>
    /// 从 Claims 提取当前用户 ID
    /// </summary>
    public static Guid GetCurrentUserId(ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    /// <summary>
    /// 检查当前用户是否为管理员
    /// </summary>
    public static bool IsAdmin(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        return role == RoleConstants.Admin || role == RoleConstants.SuperAdmin;
    }

    /// <summary>
    /// 解析 Identity 角色字符串为 UserRole 枚举
    /// </summary>
    public static UserRole ParseUserRole(IList<string> identityRoles)
    {
        if (identityRoles == null || identityRoles.Count == 0)
            return UserRole.Receptionist;

        var roleString = identityRoles[0];
        return roleString switch
        {
            RoleConstants.SuperAdmin => UserRole.SuperAdmin,
            RoleConstants.Admin => UserRole.Admin,
            RoleConstants.Doctor => UserRole.Doctor,
            RoleConstants.Receptionist => UserRole.Receptionist,
            _ => UserRole.Receptionist
        };
    }

}


