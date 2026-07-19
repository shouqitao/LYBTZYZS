using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LYBT.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// 从 ClaimsPrincipal 提取操作者信息 — 支持多种 JWT Claims 标准
/// </summary>
public static class OperatorAccessor
{
    public record OperatorInfo(Guid Id, string Name, UserRole Role);

    public static OperatorInfo GetOperator(ClaimsPrincipal? user, ILogger logger)
    {
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? user?.FindFirst("sub")?.Value;

        var userName = user?.Identity?.Name
                      ?? user?.FindFirst(ClaimTypes.Name)?.Value
                      ?? user?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                      ?? user?.FindFirst("unique_name")?.Value
                      ?? user?.FindFirst("name")?.Value;

        var roleStr = user?.FindFirst(ClaimTypes.Role)?.Value
                     ?? user?.FindFirst("role")?.Value
                     ?? user?.FindFirst("roles")?.Value
                     ?? user?.FindFirst(RoleConstants.Admin)?.Value;

        if (Guid.TryParse(userId, out var opId) && opId != Guid.Empty && !string.IsNullOrEmpty(userName))
        {
            var role = ParseUserRole(roleStr, logger);
            return new OperatorInfo(opId, userName, role);
        }

        logger.LogWarning("GetOperator失败: userId={UserId}, userName={UserName}, opId={OpId}, opIdIsEmpty={OpIdIsEmpty}",
            userId, userName, opId, opId == Guid.Empty);

        throw new UnauthorizedAccessException("未登录或用户信息无效");
    }

    public static UserRole ParseUserRole(string? roleStr, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(roleStr))
        {
            logger.LogWarning("角色值为空，默认使用Doctor");
            return UserRole.Doctor;
        }

        if (roleStr.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
            roleStr = RoleConstants.SuperAdmin;

        if (Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
            return role;

        logger.LogWarning("无效的角色值: {RoleString}，默认使用Doctor", roleStr);
        return UserRole.Doctor;
    }
}
