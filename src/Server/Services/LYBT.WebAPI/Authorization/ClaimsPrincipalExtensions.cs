using System.Security.Claims;
using LYBT.Shared.Models.Enums;

namespace LYBT.WebAPI.Authorization;

/// <summary>
/// ClaimsPrincipal 扩展方法 - 从 JWT Claims 提取用户信息
/// DRY: 统一 FormulaAuthorizationHandler 和 MedicalCaseAuthorizationHandler 的用户提取逻辑
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 从 ClaimsPrincipal 提取用户ID和角色
    /// </summary>
    public static (Guid UserId, UserRole Role) ExtractUserInfo(this ClaimsPrincipal user)
    {
        var userId = Guid.Empty;
        var role = UserRole.Doctor; // 默认最低权限

        // 提取用户ID: 优先 NameIdentifier, 其次 sub
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                          ?? user.FindFirst("sub");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        // 提取角色: 优先 Role claim, 其次 role claim
        var roleClaim = user.FindFirst(ClaimTypes.Role)
                        ?? user.FindFirst("role");
        if (roleClaim != null)
        {
            // 处理遗留命名 (SysAdmin -> SuperAdmin)
            var roleValue = roleClaim.Value;
            if (roleValue.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
            {
                role = UserRole.SuperAdmin;
            }
            else if (Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var parsedRole))
            {
                role = parsedRole;
            }
        }

        return (userId, role);
    }
}
