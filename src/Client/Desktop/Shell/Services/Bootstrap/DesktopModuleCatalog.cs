using LYBT.Desktop.Admin;
using LYBT.Desktop.Admin.Sysadmin;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Catalog;
using LYBT.Desktop.Clinical;
using LYBT.Desktop.Infrastructure.CardReader;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.MedicalCase.Reports;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Printing;
using LYBT.Desktop.Registrations;
using LYBT.Desktop.Users;
using Prism.Modularity;

namespace LYBT.Desktop.Shell.Services.Bootstrap;

/// <summary>
/// 桌面模块目录配置（SSOT）——由 <c>App.ConfigureModuleCatalog</c> 调用。
/// </summary>
/// <remarks>
/// 独立为静态方法的原因：组合守卫测试 <c>PrintingModuleCompositionTests</c> 需要驱动
/// <b>真实</b>模块目录（含模块依赖声明）才能验证「打印入口宿主模块加载 → 打印服务可解析」，
/// 测试侧另抄一份清单会与生产目录漂移。
/// </remarks>
public static class DesktopModuleCatalog
{
    /// <summary>按角色策略注册全部业务模块到模块目录</summary>
    /// <param name="moduleCatalog">Prism 模块目录</param>
    public static void Configure(IModuleCatalog moduleCatalog)
    {
        // 核心模块 - 立即加载
        moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);
        // UsersModule 列为业务模块，按需加载（由 NavigationCoordinator 在首次导航时触发）
        moduleCatalog.AddModule<UsersModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<AdminModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<SysadminModule>(InitializationMode.WhenAvailable);

        // 业务模块 - 按需加载（首次导航到该模块视图时由 NavigationCoordinator 触发）
        // 注意：Prism 8.1.97 的 ModuleCatalog 不消费 [ModuleDependency] 特性（仅 DirectoryModuleCatalog 消费），
        // 依赖必须经 AddModule 的 dependsOn 参数显式声明才会进入 CompleteListWithDependencies；
        // ClinicalModule 上现有的 [ModuleDependency] 特性因此不产生加载顺序，实际顺序由角色 RequiredModules 列表保证。
        // F-06：ClinicalModule 的 MedicalCaseWorkspaceView「打印处方单/导出 PDF」经 PrescriptionPrintHandler
        // 消费 PrintingModule 注册的 IPrintService<PrescriptionPrintModel>——显式声明该依赖，
        // 使任意加载路径（角色预加载 / 启动装载 / 导航懒加载）都会先拉起 PrintingModule，
        // 否则打印入口恒提示「打印服务未配置」。
        moduleCatalog.AddModule<ClinicalModule>(InitializationMode.OnDemand, nameof(PrintingModule));
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<CatalogModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.OnDemand);

        // PRD: registration.md - 挂号管理模块
        moduleCatalog.AddModule<RegistrationModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<CardReaderModule>(InitializationMode.OnDemand);

        // 统计报表模块
        moduleCatalog.AddModule<ReportsModule>(InitializationMode.OnDemand);

        // 打印模块
        moduleCatalog.AddModule<PrintingModule>(InitializationMode.OnDemand);
    }
}
