namespace LYBT.Infrastructure.Constants;

/// <summary>
/// 系统角色名称常量
/// </summary>
public static class RoleConstants
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Receptionist = "Receptionist";

    /// <summary>
    /// SuperAdmin 的 userType 小写形式 (用于 JWT Claims)
    /// </summary>
    public const string SuperAdminUserType = "superadmin";

    /// <summary>
    /// 默认 userType
    /// </summary>
    public const string DefaultUserType = "user";
}
