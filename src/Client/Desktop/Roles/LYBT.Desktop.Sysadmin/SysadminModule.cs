using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Sysadmin;

/// <summary>
/// 系统运维控制台模块 - 为 sysadmin 用户提供独立的暗色仪表盘体验
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
        containerRegistry.Register<ViewModels.LogLevelControlViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.SysadminHomeView>();
        containerRegistry.RegisterForNavigation<Views.LogLevelControlView>();
    }
}
