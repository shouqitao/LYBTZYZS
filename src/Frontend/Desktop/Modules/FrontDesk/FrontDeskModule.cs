using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.Modules.FrontDesk.Views;

namespace LYBT.WPF.Client.Modules.FrontDesk
{
    /// <summary>
    /// 前台模块
    /// </summary>
    public class FrontDeskModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // TODO: 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册前台主界面视图
            containerRegistry.RegisterForNavigation<FrontDeskMainView>();
        }
    }
}