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
            // Services由Core_New/Services统一注册，不在Module中注册

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.FormulaManagementViewModel>();
            containerRegistry.Register<ViewModels.FormulaDetailViewModel>();

            // Phase 2: 启用 Region Navigation 注册
            containerRegistry.RegisterForNavigation<Views.FormulaManagementView>();
            containerRegistry.RegisterForNavigation<Views.FormulaDetailView>();

            // Phase 3: 启用 Prism Dialog 注册
            containerRegistry.RegisterDialog<Views.EditFormulaDialog, ViewModels.EditFormulaDialogViewModel>();
            containerRegistry.RegisterDialog<Views.ViewFormulaDialog, ViewModels.ViewFormulaDialogViewModel>();
        }
    }
}
