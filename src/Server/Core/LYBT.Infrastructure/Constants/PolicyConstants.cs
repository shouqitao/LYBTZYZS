namespace LYBT.Infrastructure.Constants;

/// <summary>
/// 授权策略名称常量
/// </summary>
public static class PolicyConstants
{
    public const string AdminOnly = "AdminOnly";
    public const string DoctorOrAdmin = "DoctorOrAdmin";
    public const string PatientAccess = "PatientAccess";
    public const string SuperAdminOnly = "SuperAdminOnly";
}
