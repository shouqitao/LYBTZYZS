using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Herbs.Controls;
using LYBT.Desktop.Herbs.Mappers;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Herbs.Repositories;
// OpenSpec: simplify-desktop-data-layer - HerbService已删除
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Shared.Models.Contracts.Herbs;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

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
            ViewModelLocationProvider.Register(typeof(HerbMasterDetailControl).ToString(), typeof(ViewModels.HerbMasterDetailViewModel));

            // SYNC-D02: IHerbRepository 由 Shell DI 工厂注册 (根据 IConnectionModeProvider 选择实现)
            // 远程模式 -> HerbRepository (Herbs 模块)
            // 本地模式 -> LocalHerbRepository (LocalData 模块)

            // OpenSpec: standardize-service-layer - 统一使用Service层
            containerRegistry.Register<IHerbService, Services.RemoteHerbService>();

            // D5-3: 跨模块药材搜索提供者，供 MedicalCase 模块使用
            containerRegistry.Register<IHerbSearchProvider, Services.HerbSearchProvider>();

            // OpenSpec: standardize-api-architecture - MappingService已删除，使用直接Mapper实例

            // OpenSpec: simplify-desktop-data-layer - HerbService已删除，功能合并到Repository

            // OpenSpec: migrate-views-to-role-modules - HerbDetailView/HerbDetailViewModel已删除（无调用）
            // Issue #2168: CRUD统一架构 - HerbCreateViewModel已删除

            // Handler 组件
            containerRegistry.Register<ViewModels.Handlers.IHerbStatusHandler, ViewModels.Handlers.HerbStatusHandler>();
            containerRegistry.Register<ViewModels.Handlers.IHerbImportExportHandler, ViewModels.Handlers.HerbImportExportHandler>();

            // OpenSpec: refactor-viewmodel-composition - V2组合模式ViewModel
            containerRegistry.AddMasterDetailServices<HerbListDto, HerbDetailModel>();
            containerRegistry.Register<ViewModels.HerbMasterDetailViewModel>();
            
            // 注册MasterDetail View用于导航
            containerRegistry.RegisterForNavigation<Views.HerbMasterDetailView>();
        }
    }
}
