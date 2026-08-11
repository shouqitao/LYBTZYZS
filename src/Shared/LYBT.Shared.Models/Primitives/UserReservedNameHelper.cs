namespace LYBT.Shared.Models.Primitives;

/// <summary>
/// 保留用户名校验（T4 P1#10: 防 admin/administrator/root/system/superadmin 等系统账号被占用）。
/// CreateUserValidator 与 CreateUserCommandHandler 共用，单一清单避免漂移。
/// </summary>
public static class UserReservedNameHelper
{
    /// <summary>保留用户名清单（不区分大小写）</summary>
    private static readonly HashSet<string> ReservedUserNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "administrator",
        "root",
        "system",
        "superadmin",
        UserConstants.SysAdminUsername
    };

    /// <summary>判断用户名是否保留</summary>
    public static bool IsReserved(string? userName)
        => !string.IsNullOrEmpty(userName) && ReservedUserNames.Contains(userName);
}
