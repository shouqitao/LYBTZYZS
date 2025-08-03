using LYBT.WPF.Client.Modules.Examples.Controls.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace LYBT.WPF.Client.Modules.Examples
{
    /// <summary>
    /// 示例模块
    /// </summary>
    public class ExamplesModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public ExamplesModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化时可以注册视图到区域
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册导航视图
            containerRegistry.RegisterForNavigation<ControlExamplesView>();
        }
    }
}