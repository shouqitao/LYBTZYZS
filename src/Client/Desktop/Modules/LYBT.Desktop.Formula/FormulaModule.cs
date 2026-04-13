// OpenSpec: standardize-module-structure - Components已合并到Services
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Formula.Controls;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Shared.Models.Contracts.Formula;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Formula
{
    /// <summary>
    /// 验方管理模块 - Phase 2模块化架构
    /// </summary>
    [Module(ModuleName = nameof(FormulaModule))]
    [ModuleDependency("HerbsModule")] // 方剂依赖药材
    public class FormulaModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ViewModelLocationProvider.Register(typeof(FormulaMasterDetailControl).ToString(), typeof(ViewModels.FormulaMasterDetailViewModel));

            // SYNC-D02: IFormulaRepository 由 Shell DI 工厂注册 (根据 IConnectionModeProvider 选择实现)
            // 远程模式 -> FormulaRepository (Formula 模块)
            // 本地模式 -> LocalFormulaRepository (LocalData 模块)

            // D5-3: 跨模块验方搜索提供者，供 MedicalCase 模块使用
            containerRegistry.Register<IFormulaSearchProvider, Services.FormulaSearchProvider>();

            // OpenSpec: standardize-api-architecture - MappingService已删除，使用直接Mapper实例

            // OpenSpec: standardize-service-layer - 统一使用Service命名
            containerRegistry.Register<IFormulaService, Services.FormulaService>();
            containerRegistry.Register<Services.FormulaValidator>();
            containerRegistry.RegisterSingleton<Mappers.FormulaDetailModelMapper>();

            // OpenSpec: migrate-views-to-role-modules - FormulaDetailView/FormulaValidationView已删除（无调用）
            // OpenSpec: migrate-views-to-role-modules - EditFormulaDialog已删除（无调用）
            // OpenSpec: migrate-views-to-role-modules - FormulaDetailViewModel/FormulaValidationViewModel已删除（无调用）

            // OpenSpec: refactor-viewmodel-composition - V2组合模式ViewModel
            // 注册Formula模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<FormulaListDto, FormulaDetailModel>();

            // Handler DI注册
            containerRegistry.Register<ViewModels.Handlers.IFormulaStatusHandler, ViewModels.Handlers.FormulaStatusHandler>();

            // OpenSpec: refactor-admin-workspace - Control模式重构
            // FormulaMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.FormulaMasterDetailViewModel>();

            // Epic #1773 Task 4: 注册验方模块组件化组件（Scoped生命周期）
            containerRegistry.Register<ViewModels.FormulaEditorViewModel>();

            // 注册MasterDetail View用于导航
            containerRegistry.RegisterForNavigation<Views.FormulaMasterDetailView>();
        }
    }
}
