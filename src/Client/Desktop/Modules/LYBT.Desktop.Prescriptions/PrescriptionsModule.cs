using LYBT.Desktop.Modules.Prescriptions.ViewModels;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Desktop.Prescriptions.Repositories;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Prescriptions
{
    /// <summary>
    /// 处方管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(PrescriptionsModule))]
    [ModuleDependency("ConsultationModule")] // 处方依赖诊疗
    [ModuleDependency("HerbsModule")] // 处方依赖药材
    [ModuleDependency("FormulaModule")] // 处方依赖方剂
    public class PrescriptionsModule : IModule
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
            containerRegistry.RegisterSingleton<IPrescriptionRepository, PrescriptionRepository>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<PrescriptionManagementViewModel>();
            containerRegistry.Register<PrescriptionsMainViewModel>();
            containerRegistry.Register<PrescriptionViewModel>(); // Issue #1461

            // Phase 2: 启用 Region Navigation 注册
            containerRegistry.RegisterForNavigation<Views.PrescriptionManagementView>();
            containerRegistry.RegisterForNavigation<Views.PrescriptionsMainView>();
            containerRegistry.RegisterForNavigation<Views.PrescriptionView>(); // Issue #1461

            // Phase 3: 启用 Prism Dialog 注册
            containerRegistry.RegisterDialog<Views.FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
            containerRegistry.RegisterDialog<Views.HerbSelectionDialog, HerbSelectionDialogViewModel>();
            containerRegistry.RegisterDialog<Views.PrescriptionEditorDialog, PrescriptionEditorDialogViewModel>();
            containerRegistry.RegisterDialog<Views.SelectFormulaDialog, SelectFormulaDialogViewModel>();
        }
    }
}
