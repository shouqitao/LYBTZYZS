using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Modules.Prescriptions.ViewModels;
using LYBT.Desktop.Prescriptions.Services;
using LYBT.Desktop.Services.Print;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Prescriptions
{
    /// <summary>
    /// 处方管理模块 - 简化版
    /// Issue #1606 Phase 3: 移除IPrescriptionRepository（已迁移至MedicalCaseRepository聚合根）
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
            // Issue #1606 Phase 3: IPrescriptionRepository已删除，所有Write操作通过MedicalCaseRepository聚合根
            // 原：containerRegistry.RegisterSingleton<IPrescriptionRepository, PrescriptionRepository>(); // 已删除

            // 注册打印服务（Issue #1381: PRINT-4）
            containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();

            // Epic #1540: 注册处方编辑器服务（方案B - 包装模式）
            // 实现依赖倒置：MedicalCase模块依赖IPrescriptionEditorService接口
            containerRegistry.RegisterSingleton<IPrescriptionEditorService, PrescriptionEditorService>();

            // ⚠️ Issue #1606 Phase 3: 临时注释，待Issue #1608重构这些ViewModel
            // 原因：依赖已删除的IPrescriptionRepository
            // 注册视图模型 - MVP核心功能
            // containerRegistry.Register<PrescriptionManagementViewModel>();  // 待重构 Issue #1608
            // containerRegistry.Register<PrescriptionsMainViewModel>();  // 待重构 Issue #1608
            // containerRegistry.Register<PrescriptionViewModel>();  // 待重构 Issue #1608 (Issue #1461)

            // Phase 2: 启用 Region Navigation 注册
            // ⚠️ Issue #1606 Phase 3: 临时注释，待Issue #1608重构
            // containerRegistry.RegisterForNavigation<Views.PrescriptionManagementView>();  // 待重构 Issue #1608
            // containerRegistry.RegisterForNavigation<Views.PrescriptionsMainView>();  // 待重构 Issue #1608
            // containerRegistry.RegisterForNavigation<Views.PrescriptionView>();  // 待重构 Issue #1608 (Issue #1461)

            // Phase 3: 启用 Prism Dialog 注册
            containerRegistry.RegisterDialog<Views.FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
            containerRegistry.RegisterDialog<Views.HerbSelectionDialog, HerbSelectionDialogViewModel>();
            // ⚠️ Issue #1606 Phase 3: 临时注释，待Issue #1608重构
            // containerRegistry.RegisterDialog<Views.PrescriptionEditorDialog, PrescriptionEditorDialogViewModel>();  // 待重构 Issue #1608
            containerRegistry.RegisterDialog<Views.SelectFormulaDialog, SelectFormulaDialogViewModel>();
            // Epic #1676 Phase 2: PrescriptionDeleteConfirmDialog已替换为全局ConfirmationDialog（Shell）
            // containerRegistry.RegisterDialog<Views.PrescriptionDeleteConfirmDialog, ViewModels.PrescriptionDeleteConfirmDialogViewModel>();
        }
    }
}
