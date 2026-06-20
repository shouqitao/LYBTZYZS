using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Roles.Definitions
{
    /// <summary>
    /// 超级管理员角色定义
    /// refactor-auth-role-system Phase 2.1.4
    /// </summary>
    /// <remarks>
    /// SuperAdmin = 系统运维。负责平台正常运行，包括部署、配置、诊断、数据库维护。
    /// 非业务角色，仅在系统需要维护时使用。
    /// 使用管理主页视图（AdminHomeView）。
    /// </remarks>
    public class SuperAdminRoleDefinition : RoleDefinitionBase
    {
        private static readonly string[] Modules = new[]
        {
            "UsersModule",       // 用户管理（个人资料、修改密码）
            "PatientsModule",
            "HerbsModule",
            "FormulaModule",
            "MedicalCaseModule"
            // [已删除] "PrescriptionsModule" - 空壳模块已移除
        };

        /// <inheritdoc/>
        public override UserRole Role => UserRole.SuperAdmin;

        /// <inheritdoc/>
        public override string DisplayName => "超级管理员";

        /// <inheritdoc/>
        public override string Description => "系统运维：平台部署、配置、诊断、数据库维护";

        /// <inheritdoc/>
        public override string HomeViewName => ViewNames.AdminHome;

        /// <inheritdoc/>
        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
