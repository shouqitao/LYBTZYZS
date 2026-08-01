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
    /// 获取当前用户角色
    /// </summary>
    public static UserRole GetCurrentUserRole(ClaimsPrincipal user)
        => Enum.TryParse<UserRole>(user.FindFirst(ClaimTypes.Role)?.Value, out var role) ? role : UserRole.Receptionist;

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

    /// <summary>
    /// 将 UserRole 枚举映射为 Identity 角色字符串
    /// </summary>
    public static string MapUserRoleToString(UserRole role)
    {
        return role switch
        {
            UserRole.SuperAdmin => RoleConstants.SuperAdmin,
            UserRole.Admin => RoleConstants.Admin,
            UserRole.Doctor => RoleConstants.Doctor,
            UserRole.Receptionist => RoleConstants.Receptionist,
            _ => RoleConstants.Receptionist
        };
    }

    /// <summary>
    /// 检查当前用户是否有权管理目标用户
    /// </summary>
    public static bool CanManageUser(UserRole current, UserRole target, bool isTargetSysAdmin)
    {
        // Sysadmin 不可被管理
        if (isTargetSysAdmin)
            return false;

        // SuperAdmin 可管理所有人
        if (current == UserRole.SuperAdmin)
            return true;

        // Admin 可管理 Doctor 和 Receptionist
        if (current == UserRole.Admin)
            return target == UserRole.Doctor || target == UserRole.Receptionist;

        // 其他角色不可管理用户
        return false;
    }
}


