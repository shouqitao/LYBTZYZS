using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Roles.Definitions
{
    /// <summary>
    /// 超级管理员角色定义
    /// refactor-auth-role-system Phase 2.1.4
    /// </summary>
    /// <remarks>
    /// 超级管理员拥有最高权限，可以管理所有Admin用户
    /// 与Admin共享同一主页视图（AdminHomeView）
    /// </remarks>
    public class SuperAdminRoleDefinition : RoleDefinitionBase
    {
        private static readonly string[] Modules = new[]
        {
            "UsersModule",       // 用户管理（个人资料、修改密码）
            "PatientsModule",
            "HerbsModule",
            "FormulaModule",
            "MedicalCaseModule",
            "PrescriptionsModule"
        };

        /// <inheritdoc/>
        public override UserRole Role => UserRole.SuperAdmin;

        /// <inheritdoc/>
        public override string DisplayName => "超级管理员";

        /// <inheritdoc/>
        public override string Description => "系统最高权限，可管理所有用户和系统配置";

        /// <inheritdoc/>
        public override string HomeViewName => "AdminHomeView";

        /// <inheritdoc/>
        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
