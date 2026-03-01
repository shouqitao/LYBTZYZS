using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Mappers;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Herbs.Repositories;
// OpenSpec: simplify-desktop-data-layer - HerbService已删除
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
        }
    }
}
