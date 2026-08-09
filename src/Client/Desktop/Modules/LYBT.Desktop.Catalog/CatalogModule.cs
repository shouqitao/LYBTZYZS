using LYBT.Desktop.Catalog.Controls;
using LYBT.Desktop.Catalog.Models;
using LYBT.Desktop.Catalog.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Catalog
{
    /// <summary>
    /// 药房目录管理模块（Herbs + Formula 合并）
    /// </summary>
    [Module(ModuleName = nameof(CatalogModule))]
    [ModuleDependency("AuthenticationModule")] // 目录模块只依赖认证
    public class CatalogModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ViewModelLocationProvider.Register(typeof(HerbMasterDetailControl).ToString(), typeof(ViewModels.HerbMasterDetailViewModel));
            ViewModelLocationProvider.Register(typeof(FormulaMasterDetailControl).ToString(), typeof(ViewModels.FormulaMasterDetailViewModel));

            // IHerbRepository / IFormulaRepository 由 Shell DI 注册 (Refit API)
            containerRegistry.Register<IHerbService, Services.RemoteHerbService>();
            containerRegistry.Register<IFormulaService, Services.FormulaService>();

            // D5-3: 跨模块搜索提供者，供 MedicalCase 模块使用
            containerRegistry.Register<IHerbSearchProvider, Services.HerbSearchProvider>();
            containerRegistry.Register<IFormulaSearchProvider, Services.FormulaSearchProvider>();

            containerRegistry.Register<ViewModels.Handlers.IHerbStatusHandler, ViewModels.Handlers.HerbStatusHandler>();
            containerRegistry.Register<ViewModels.Handlers.IFormulaStatusHandler, ViewModels.Handlers.FormulaStatusHandler>();

            containerRegistry.RegisterSingleton<Mappers.FormulaDetailModelMapper>();

            containerRegistry.AddMasterDetailServices<HerbListDto, HerbDetailModel>();
            containerRegistry.AddMasterDetailServices<FormulaListDto, FormulaDetailModel>();

            containerRegistry.Register<ViewModels.HerbMasterDetailViewModel>();
            containerRegistry.Register<ViewModels.HerbEditorViewModel>();
            containerRegistry.Register<ViewModels.FormulaMasterDetailViewModel>();
            containerRegistry.Register<ViewModels.FormulaEditorViewModel>();
        }
    }
}
