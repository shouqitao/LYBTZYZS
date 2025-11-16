using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.Repositories;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Formula
{
    /// <summary>
    /// 验方管理模块 - 简化版
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
            // ADR-002 架构标准：
            // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
            // - Repository (数据访问层) 由各业务模块自行注册
            containerRegistry.RegisterSingleton<IFormulaRepository, FormulaRepository>();

            // Issue #1787: 注册Formula模块组件化组件（Epic #1773 Component-Based架构）
            containerRegistry.Register<IFormulaDataManager, ViewModels.Components.FormulaDataManager>();
            containerRegistry.Register<IFormulaCommandHandler, ViewModels.Components.FormulaCommandHandler>();
            containerRegistry.Register<ViewModels.Components.FormulaValidator>();
            containerRegistry.Register<ViewModels.Components.FormulaCalculator>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.FormulaManagementViewModel>();
            containerRegistry.Register<ViewModels.FormulaDetailViewModel>();
            containerRegistry.Register<ViewModels.FormulaValidationViewModel>(); // FORMULA-10: 验方校验视图模型

            // Phase 2: 启用 Region Navigation 注册
            containerRegistry.RegisterForNavigation<Views.FormulaManagementView>();
            containerRegistry.RegisterForNavigation<Views.FormulaDetailView>();
            containerRegistry.RegisterForNavigation<Views.FormulaValidationView>(); // FORMULA-10: 验方校验视图

            // Phase 3: 启用 Prism Dialog 注册
            containerRegistry.RegisterDialog<Views.EditFormulaDialog, ViewModels.EditFormulaDialogViewModel>();
            // Issue #1802: ViewFormulaDialog已删除（改用FormulaDetailView进行只读查看）
        }
    }
}
