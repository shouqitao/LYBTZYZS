using LYBT.Desktop.Infrastructure.Constants;
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
            "UsersModule",       // 用户管理（个人资料、修改密码）
            "PatientsModule",
            "HerbsModule",
            "FormulaModule",
            "MedicalCaseModule"
            // [已删除] "PrescriptionsModule" - 空壳模块已移除
        };

        /// <inheritdoc/>
        public override UserRole Role => UserRole.Admin;

        /// <inheritdoc/>
        public override string DisplayName => "管理员";

        /// <inheritdoc/>
        public override string Description => "系统管理、用户管理、系统配置";

        /// <inheritdoc/>
        /// <remarks>OpenSpec: unify-navigation-architecture - 使用ViewNames常量</remarks>
        public override string HomeViewName => ViewNames.AdminHome;

        /// <inheritdoc/>
        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
