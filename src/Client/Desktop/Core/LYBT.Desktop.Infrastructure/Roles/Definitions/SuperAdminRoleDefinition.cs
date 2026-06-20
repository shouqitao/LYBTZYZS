using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Roles.Definitions
{
    /// <summary>
    /// 系统运维角色定义 — sysadmin 独立用户使用此定义
    /// </summary>
    /// <remarks>
    /// sysadmin 是独立用户（IsSysAdmin=true），不是普通角色。
    /// Desktop 端使用此定义来加载模块和导航主页，配置与 Admin 一致。
    /// </remarks>
    public class SuperAdminRoleDefinition : RoleDefinitionBase
    {
        private static readonly string[] Modules = new[]
        {
            "UsersModule",
            "PatientsModule",
            "HerbsModule",
            "FormulaModule",
            "MedicalCaseModule"
        };

        public override UserRole Role => UserRole.SuperAdmin;

        public override string DisplayName => "系统运维";

        public override string Description => "系统运维：平台部署、配置、诊断、数据库维护";

        public override string HomeViewName => ViewNames.AdminHome;

        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
