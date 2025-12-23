using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Roles.Definitions
{
    /// <summary>
    /// 前台接待角色定义
    /// refactor-auth-role-system Phase 2.2.1/2.3.3
    /// </summary>
    /// <remarks>
    /// 前台接待负责患者登记、预约管理
    /// 使用临床主页视图（ClinicalHomeView）
    /// 仅加载患者管理模块
    /// </remarks>
    public class ReceptionistRoleDefinition : RoleDefinitionBase
    {
        private static readonly string[] Modules = new[]
        {
            "PatientsModule"  // 前台仅需要患者管理功能
        };

        /// <inheritdoc/>
        public override UserRole Role => UserRole.Receptionist;

        /// <inheritdoc/>
        public override string DisplayName => "前台接待";

        /// <inheritdoc/>
        public override string Description => "患者登记、预约管理";

        /// <inheritdoc/>
        public override string HomeViewName => "ClinicalHomeView";

        /// <inheritdoc/>
        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
