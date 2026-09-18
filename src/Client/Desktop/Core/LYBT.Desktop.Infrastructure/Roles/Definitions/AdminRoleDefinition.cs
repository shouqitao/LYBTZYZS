using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Roles.Definitions
{
    /// <summary>
    /// 管理员角色定义
    /// refactor-auth-role-system Phase 2.1.4
    /// </summary>
    /// <remarks>
    /// Admin = 业务管理员。负责用户管理、药材管理、验方管理、业务配置。
    /// 不可创建医案（仅 Doctor 可创建）。
    /// 使用管理主页视图（AdminHomeView）。
    /// </remarks>
    public class AdminRoleDefinition : RoleDefinitionBase
    {
        private static readonly string[] Modules = new[]
        {
            "UsersModule",       // 用户管理（个人资料、修改密码）
            "PatientsModule",
            "CatalogModule",
            "MedicalCaseModule",
            "ReportsModule",
            // 设计原则：角色 Home 所属模块必须在 RequiredModules（AdminHomeView 注册于 AdminModule）
            "AdminModule",
            // Admin 可访问 Herb/Formula/Patient/MedicalCase 管理薄包装（注册于 ClinicalModule）
            "ClinicalModule"
        };

        /// <inheritdoc/>
        public override UserRole Role => UserRole.Admin;

        /// <inheritdoc/>
        public override string DisplayName => "管理员";

        /// <inheritdoc/>
        public override string Description => "业务管理：用户管理、药材/验方管理、业务配置、医案审核";

        /// <inheritdoc/>
        public override string HomeViewName => ViewNames.AdminHome;

        /// <inheritdoc/>
        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
