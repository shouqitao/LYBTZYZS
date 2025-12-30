using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.Herbs.Services;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Shared.Models.Contracts.Herbs;
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

            // OpenSpec: standardize-service-layer - 统一使用Service命名
            containerRegistry.RegisterScoped<IHerbService, HerbService>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.HerbDetailViewModel>();
            // Issue #2168: CRUD统一架构 - HerbCreateViewModel已删除，统一使用HerbDetailViewModel

            // Phase 2: 启用 Region Navigation 注册（试点模块）
            containerRegistry.RegisterForNavigation<Views.HerbDetailView>();

            // Issue #2168: CRUD统一架构 - HerbDetailView支持Create/Detail两种模式
            // HerbCreateView已删除，统一使用HerbDetailView

            // OpenSpec: refactor-master-detail-layout - Master-Detail合并视图
            // 显式指定View-ViewModel映射，确保正确的ViewModel绑定
            containerRegistry.RegisterForNavigation<Views.HerbMasterDetailView, ViewModels.HerbMasterDetailViewModel>();

            // OpenSpec: refactor-viewmodel-composition - V2组合模式ViewModel
            // 注册Herbs模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<HerbListDto, HerbDetailModel>();
            containerRegistry.RegisterForNavigation<Views.HerbMasterDetailView, ViewModels.HerbMasterDetailViewModel>();
        }
    }
}
