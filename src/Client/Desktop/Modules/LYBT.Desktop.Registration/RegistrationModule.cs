// SYNC-D02: IRegistrationRepository 已迁移到 LYBT.Desktop.Contracts.Repositories
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Registration.Repositories;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Registration;

/// <summary>
/// 挂号管理模块 -- Prism 模块入口
/// PRD: registration.md US-REG-001~006
/// 依赖: AuthenticationModule (权限), PatientsModule (患者查询), UsersModule (医生列表)
/// </summary>
[Module(ModuleName = nameof(RegistrationModule))]
[ModuleDependency("AuthenticationModule")]
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
        // SYNC-D02: IRegistrationRepository 由 Shell DI 工厂注册 (根据 IConnectionModeProvider 选择实现)
        // 远程模式 -> RegistrationRepository (Registration 模块)
        // 本地模式 -> LocalRegistrationRepository (LocalData 模块)

        // ViewModel
        containerRegistry.Register<ViewModels.RegistrationListViewModel>();

        // 导航视图
        containerRegistry.RegisterForNavigation<Views.RegistrationListView>();
    }
}
