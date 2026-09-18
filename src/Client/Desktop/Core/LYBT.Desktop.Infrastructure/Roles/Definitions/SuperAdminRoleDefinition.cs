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
            "SysadminModule",
            // D-5: SysadminHome 面板依赖 AdminModule 注册的 IServerConfigurationService 等服务
            "AdminModule",
            // DI 修复：SysadminHomeViewModel → CardReaderDiagnosticsViewModel → ICardReaderDiagnostics
            // 注册于 OnDemand 的 CardReaderModule；不含此模块则超管登录后解析 SysadminHomeView 失败
            // （An unexpected error occurred while resolving 'System.Object' ... 'SysadminHomeView'）
            "CardReaderModule",
            // 设计原则：角色可访问的管理视图所属模块必须在 RequiredModules
            // Herb/Formula/Patient 管理薄包装注册于 ClinicalModule
            "ClinicalModule",
            // ReportsHome 注册于 ReportsModule
            "ReportsModule",
            // AuditLog / MedicalCaseMasterDetail 注册于 MedicalCaseModule
            "MedicalCaseModule"
        };

        public override UserRole Role => UserRole.SuperAdmin;

        public override string DisplayName => "系统运维";

        public override string Description => "系统运维：平台部署、配置、诊断、数据库维护";

        public override string HomeViewName => ViewNames.SysadminHome;

        public override IReadOnlyList<string> RequiredModules => Modules;
    }
}
