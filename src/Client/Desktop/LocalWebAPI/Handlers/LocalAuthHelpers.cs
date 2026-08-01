using LYBT.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;

namespace LYBT.LocalWebAPI.Handlers;

internal static class LocalAuthHelpers
{
    public static UserRole ParseUserRole(IList<string> roles)
    {
        if (roles.Count == 0) return UserRole.Doctor;
        var roleStr = roles[0];
        if (roleStr.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
            roleStr = RoleConstants.SuperAdmin;
        return Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role) ? role : UserRole.Doctor;
    }
}
