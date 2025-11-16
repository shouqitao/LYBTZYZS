using LYBT.Desktop.Herbs.Components; // Epic #1773: 添加Component命名空间
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Repositories;
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
            // ADR-002 架构标准：
            // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
            // - Repository (数据访问层) 由各业务模块自行注册
            containerRegistry.RegisterSingleton<IHerbRepository, HerbRepository>();

            // Epic #1773: 注册Component组件（Issue #2149: 注册接口以支持跨模块访问）
            containerRegistry.Register<IHerbDataManager, HerbDataManager>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.HerbManagementViewModel>();
            containerRegistry.Register<ViewModels.HerbDetailViewModel>();
            containerRegistry.Register<ViewModels.HerbCreateViewModel>();

            // Phase 2: 启用 Region Navigation 注册（试点模块）
            containerRegistry.RegisterForNavigation<Views.HerbManagementView>();
            containerRegistry.RegisterForNavigation<Views.HerbDetailView>();
            containerRegistry.RegisterForNavigation<Views.HerbCreateView>();
        }
    }
}
