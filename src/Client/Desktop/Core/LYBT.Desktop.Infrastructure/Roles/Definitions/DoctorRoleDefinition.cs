using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Roles.Definitions
{
    /// <summary>
    /// 医生角色定义
    /// refactor-auth-role-system Phase 2.1.4
    /// </summary>
    /// <remarks>
    /// 医生负责诊疗、记录、查询等业务操作
    /// 使用临床主页视图（ClinicalHomeView）
    /// </remarks>
    public class DoctorRoleDefinition : RoleDefinitionBase
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
        public override UserRole Role => UserRole.Doctor;

        /// <inheritdoc/>
        public override string DisplayName => "医生";

        /// <inheritdoc/>
        public override string Description => "诊疗、记录、查询等业务操作";

        /// <inheritdoc/>
        /// <remarks>OpenSpec: unify-navigation-architecture - 使用ViewNames常量</remarks>
        public override string HomeViewName => ViewNames.ClinicalHome;

        /// <inheritdoc/>
        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
