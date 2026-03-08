using LYBT.Desktop.Registration.Interfaces;
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
        // Repository (DataSource 抽象层，支持 Local/Remote)
        containerRegistry.RegisterSingleton<IRegistrationRepository, RegistrationRepository>();

        // ViewModel
        containerRegistry.Register<ViewModels.RegistrationListViewModel>();

        // 导航视图
        containerRegistry.RegisterForNavigation<Views.RegistrationListView>();
    }
}
