// SYNC-D02: IRegistrationRepository 已迁移到 LYBT.Desktop.Contracts.Repositories
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Registration.Repositories;
using LYBT.Desktop.Registration.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Registration;

/// <summary>
/// 挂号管理模块 -- Prism 模块入口
/// PRD: registration.md US-REG-001~006
/// </summary>
[Module(ModuleName = nameof(RegistrationModule))]
[ModuleDependency("AuthenticationModule")]
[ModuleDependency("PatientsModule")]
[ModuleDependency("UsersModule")]
public class RegistrationModule : IModule
{
    /// <inheritdoc/>
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化
    }

    /// <inheritdoc/>
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // IRegistrationRepository 由 Shell DI 注册 (Refit API)

        // OpenSpec: standardize-service-layer - 统一使用Service层
        containerRegistry.Register<IRegistrationService, RemoteRegistrationService>();

        // ViewModel
        containerRegistry.Register<ViewModels.RegistrationListViewModel>();

        // 导航视图
        containerRegistry.RegisterForNavigation<Views.RegistrationListView>();

        // 对话框
        containerRegistry.RegisterDialog<Dialogs.RegistrationCreateDialog, Dialogs.RegistrationCreateDialogViewModel>();
    }
}
