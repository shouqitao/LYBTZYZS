using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Receptionist;

/// <summary>
/// 前台角色模块
/// 功能：前台工作台主页，患者登记与挂号管理入口
/// OpenSpec: create-receptionist-workspace
/// </summary>
[Module(ModuleName = nameof(ReceptionistModule))]
[ModuleDependency("PatientsModule")]
[ModuleDependency("RegistrationModule")]
[ModuleDependency("CardReaderModule")]
public class ReceptionistModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册视图模型
        containerRegistry.Register<ViewModels.ReceptionistHomeViewModel>();
        
        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.ReceptionistHomeView>();
    }
}