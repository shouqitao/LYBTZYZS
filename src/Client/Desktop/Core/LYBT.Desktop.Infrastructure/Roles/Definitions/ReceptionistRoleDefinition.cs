using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Roles.Definitions
{
    /// <summary>
    /// 前台接待角色定义
    /// refactor-auth-role-system Phase 2.2.1/2.3.3
    /// </summary>
    /// <remarks>
    /// Receptionist = 前台。专注患者挂号和相关信息维护。
    /// 使用前台工作台视图（ReceptionistHomeView）。
    /// 仅加载 UsersModule + PatientsModule + RegistrationModule（3个模块，最精简）。
    /// </remarks>
    public class ReceptionistRoleDefinition : RoleDefinitionBase
    {
        private static readonly string[] Modules = new[]
        {
            "UsersModule",        // 用户管理（个人资料、修改密码）
            "PatientsModule",     // 前台需要患者管理功能
            "RegistrationModule"  // PRD: registration.md - 挂号管理（前台核心功能）
        };

        /// <inheritdoc/>
        public override UserRole Role => UserRole.Receptionist;

        /// <inheritdoc/>
        public override string DisplayName => "前台接待";

        /// <inheritdoc/>
        public override string Description => "前台挂号：患者登记、信息维护";

        /// <inheritdoc/>
        public override string HomeViewName => ViewNames.ReceptionistHome;

        /// <inheritdoc/>
        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
