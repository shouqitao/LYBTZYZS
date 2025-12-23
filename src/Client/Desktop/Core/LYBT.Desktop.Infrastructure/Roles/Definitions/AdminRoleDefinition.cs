using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Roles.Definitions
{
    /// <summary>
    /// 管理员角色定义
    /// refactor-auth-role-system Phase 2.1.4
    /// </summary>
    /// <remarks>
    /// 管理员负责系统管理、用户管理、系统配置
    /// 可以管理Doctor但不能管理Admin
    /// </remarks>
    public class AdminRoleDefinition : RoleDefinitionBase
    {
        private static readonly string[] Modules = new[]
        {
            "PatientsModule",
            "HerbsModule",
            "FormulaModule",
            "MedicalCaseModule",
            "PrescriptionsModule"
        };

        /// <inheritdoc/>
        public override UserRole Role => UserRole.Admin;

        /// <inheritdoc/>
        public override string DisplayName => "管理员";

        /// <inheritdoc/>
        public override string Description => "系统管理、用户管理、系统配置";

        /// <inheritdoc/>
        public override string HomeViewName => "AdminHomeView";

        /// <inheritdoc/>
        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
