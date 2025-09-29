using LYBT.Desktop.Herbs.Services;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Herbs
{
    /// <summary>
    /// 药材管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(HerbsModule))]
    [ModuleDependency("AuthenticationModule")] // 药材模块只依赖认证
    public class HerbsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册简化的服务
            containerRegistry.RegisterSingleton<IHerbService, HerbService>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.HerbManagementViewModel>();
            containerRegistry.Register<ViewModels.HerbDetailViewModel>();

            // 注册视图用于导航 - 需要对应视图文件存在
            // containerRegistry.RegisterForNavigation<Views.HerbManagementView>();
            // containerRegistry.RegisterForNavigation<Views.HerbDetailView>();
        }
    }
}