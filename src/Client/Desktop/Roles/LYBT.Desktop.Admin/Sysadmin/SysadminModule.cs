using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Admin.Sysadmin;

/// <summary>
/// 系统运维设置模块 - 为 sysadmin 用户提供独立的配置运维体验
/// </summary>
[Module(ModuleName = nameof(SysadminModule))]
public class SysadminModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化完成回调
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册 VM（Prism ViewModelLocator 需要从容器解析）
        containerRegistry.Register<ViewModels.SysadminHomeViewModel>();
        containerRegistry.Register<ViewModels.ConfigurationCenterViewModel>();
        containerRegistry.Register<ViewModels.ServerConfigSectionViewModel>();
        containerRegistry.Register<ViewModels.CardReaderDiagnosticsViewModel>();
        containerRegistry.Register<ViewModels.LogLevelControlViewModel>();
        containerRegistry.Register<ViewModels.DeploymentViewModel>();
        containerRegistry.Register<ViewModels.BackupManagementViewModel>();
        containerRegistry.Register<ViewModels.SecurityAuditLogViewModel>();
        // US-SHELL-016: 配置导入导出
        containerRegistry.Register<ViewModels.ConfigExportImportViewModel>();

        // 注册服务
        containerRegistry.Register<IAuthHealthService, Services.AuthHealthService>();
        // D6: DP10 收口——Sysadmin VM 经服务门面访问 IApiClient 子域
        containerRegistry.Register<IDeploymentService, DeploymentService>();
        containerRegistry.Register<IDiagnosticsService, DiagnosticsService>();
        // B-06: 备份/恢复服务门面（BackupManagementViewModel → IApiClient.Backup）
        containerRegistry.Register<IBackupManagementService, Services.BackupManagementService>();
        // ServerConfigSectionViewModel 依赖 IServerConfigurationService（原仅注册于 AdminModule）
        containerRegistry.Register<IServerConfigurationService, ServerConfigurationService>();
        containerRegistry.Register<ISecurityAuditQueryService, Services.SecurityAuditQueryService>();
        // US-SHELL-016: 配置导入导出服务（导出/导入 JSON 配置包）
        containerRegistry.Register<IConfigurationPackageService, Services.ConfigurationPackageService>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.SysadminHomeView>();
        containerRegistry.RegisterForNavigation<Views.LogLevelControlView>();
        containerRegistry.RegisterForNavigation<Views.DeploymentView>();
        containerRegistry.RegisterForNavigation<Views.BackupManagementView>();
        containerRegistry.RegisterForNavigation<Views.SecurityAuditLogView>();
        containerRegistry.RegisterForNavigation<Views.ConfigExportImportView>();
    }
}
