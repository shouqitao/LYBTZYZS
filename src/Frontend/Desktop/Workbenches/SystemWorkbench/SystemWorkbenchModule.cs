using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using LYBT.WPF.Client.Workbenches.SystemWorkbench.ViewModels;
using LYBT.WPF.Client.Workbenches.SystemWorkbench.Views;
using LYBT.WPF.Client.Workbenches.SystemWorkbench.Services;
using LYBT.WPF.Client.Workbenches.SystemWorkbench.Navigation;
using LYBT.WPF.Client.Workbenches.Core;

namespace LYBT.WPF.Client.Workbenches.SystemWorkbench
{
    /// <summary>
    /// 系统管理工作台模块
    /// 为管理员提供统一的管理界面
    /// </summary>
    public class SystemWorkbenchModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 注册ViewModel映射
            ViewModelLocationProvider.Register<SystemWorkbenchMainView, SystemWorkbenchMainViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册工作台导航器
            containerRegistry.RegisterSingleton<ISystemWorkbenchNavigator, SystemWorkbenchNavigator>();
            
            // 注册主视图
            containerRegistry.RegisterForNavigation<SystemWorkbenchMainView>();
            
            // 注册子视图（这些视图将由业务模块提供）
            // 用户管理、患者管理、药材管理等视图由各自的BusinessModules提供
        }
    }
}