namespace LYBT.Infrastructure.Constants;

public static class PolicyConstants
{
    public const string AdminBusinessOnly = "AdminBusinessOnly";
    public const string DoctorOnly = "DoctorOnly";
    public const string DoctorOrAdmin = "DoctorOrAdmin";
    public const string AdminOrSuperAdmin = "AdminOrSuperAdmin";

    /// <summary>仅系统管理员（SuperAdmin）——配置中心/重启等系统运维端点（SHELL-018 Phase 1）</summary>
    public const string SysAdminOnly = "SysAdminOnly";
    public const string DoctorOrReceptionist = "DoctorOrReceptionist";
    public const string ReceptionistOnly = "ReceptionistOnly";
    public const string DoctorOrAdminOrReceptionist = "DoctorOrAdminOrReceptionist";
}


