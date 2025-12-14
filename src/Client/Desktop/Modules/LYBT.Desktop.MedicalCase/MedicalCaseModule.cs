// OpenSpec: standardize-module-structure - Components已合并到Services
using LYBT.Desktop.MedicalCase.Dialogs; // Issue #2246: 弹窗组件
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.MedicalCase.Services; // Issue #1790: 引入Manager服务
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.MedicalCase
{
    /// <summary>
    /// 医疗案例管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(MedicalCaseModule))]
    [ModuleDependency("PatientsModule")] // 病历依赖患者
    [ModuleDependency("PrescriptionsModule")] // Task #1499: 处方编辑器依赖处方模块
    //  移除ConsultationModule依赖 - MedicalCase是聚合根，不应依赖子实体模块 (Issue #1463)
    public class MedicalCaseModule : IModule
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
            containerRegistry.RegisterSingleton<IMedicalCaseRepository, MedicalCaseRepository>();

            // OpenSpec: medicalcase-management-ui-refactor (EDITMODE-010) - 审计需求检查器
            containerRegistry.RegisterSingleton<IAuditRequirementChecker, AuditRequirementChecker>();

            // Epic #1773: 注册Component组件
            containerRegistry.Register<MedicalCaseDataManager>();

            // Issue #1806: 注册MedicalCaseFlowViewModel组件化服务（Epic #1805 Phase 2）
            // [已移除] MedicalCaseFlowManager - 三步流程已取消
            containerRegistry.RegisterScoped<MedicalCaseLifecycleHandler>();
            containerRegistry.RegisterScoped<MedicalCaseDataLoader>();

            // OpenSpec: refactor-viewmodel-layer - 工作区协调器
            containerRegistry.RegisterScoped<ViewModels.Components.MedicalCaseWorkspaceCoordinator>();

            // OpenSpec: refactor-viewmodel-layer Phase 1 - 编辑模式状态机
            containerRegistry.RegisterScoped<ViewModels.Components.MedicalCaseEditModeStateMachine>();

            // OpenSpec: refactor-viewmodel-layer Phase 5 - 导航处理器
            containerRegistry.RegisterScoped<MedicalCaseNavigationHandler>();

            // OpenSpec: cleanup-ui-layer - Phase 1.1 PrescriptionPanel Components
            containerRegistry.Register<ViewModels.Components.PrescriptionCalculator>();
            containerRegistry.Register<ViewModels.Components.PrescriptionValidator>();
            containerRegistry.Register<ViewModels.Components.PrescriptionItemHandler>();
            containerRegistry.Register<ViewModels.Components.PrescriptionSaveHandler>();
            containerRegistry.Register<ViewModels.Components.PrescriptionImportHandler>();
            containerRegistry.Register<ViewModels.Components.PrescriptionDataLoader>();

            // Issue #1807: 注册PrescriptionEditorViewModel组件化服务 Phase 2
            containerRegistry.Register<PrescriptionCalculator>();
            // [已移除] FormulaImportHandler - 死代码，功能已由FormulaImportDialog + PrescriptionImportHandler实现
            containerRegistry.Register<HerbSelectionManager>();

            // Issue #1548: CreateMedicalCaseDialog已删除（由MedicalCaseFlowView的4步流程替代）
            // Phase 3.4: 启用 Prism Dialog 注册（已废弃）
            // containerRegistry.RegisterDialog<Views.CreateMedicalCaseDialog, ViewModels.CreateMedicalCaseDialogViewModel>();

            // 注册视图模型 - MVP核心功能
            // Issue #1549: MedicalCaseEntryViewModel已删除（由MedicalCaseFlowView的4步流程替代）
            // Epic #1583: PatientSelectionViewModel已移至PatientsModule（三区域布局）
            containerRegistry.Register<ViewModels.PrescriptionItemViewModel>();  // Epic #2175 BF-002 Task 3.5: 处方药材项ViewModel
            // OpenSpec: refactor-medicalcase-ui - 废弃注册已清理

            // Epic #2210 Phase 4: 4:6统一工作区视图模型
            containerRegistry.Register<ViewModels.MedicalCaseWorkspaceViewModel>();
            containerRegistry.Register<ViewModels.ConsultationPanelViewModel>();
            containerRegistry.Register<ViewModels.PrescriptionPanelViewModel>();

            // [已移除] PrescriptionEditorViewModel - 死代码，从未导航到（OpenSpec: refactor-viewmodel-layer）
            // Issue #1799: 删除OtherCasesQueryViewModel（违反AR-001聚合根约束）
            // Issue #1799: 删除MedicalCaseListViewModel（功能与ManagementView重复）
            containerRegistry.Register<ViewModels.MedicalCaseManagementViewModel>();  // Issue #1799: 保留作为唯一医案管理入口
            containerRegistry.Register<ViewModels.MedicalCaseDetailViewModel>();  // Issue #2167: 医案详情视图模型

            // [已删除] FormulaSelectionDialog - 过时代码，已被FormulaImportDialog替代

            // Epic #2175 BF-002 Task 3.9: 注册历史处方选择对话框
            containerRegistry.RegisterDialog<Views.HistoryPrescriptionSelectionDialog, ViewModels.HistoryPrescriptionSelectionDialogViewModel>();

            // Epic #2175 BF-002 Task 3.10: 注册重复药材聚合提醒对话框
            containerRegistry.RegisterDialog<Views.DuplicateHerbAlertDialog, ViewModels.DuplicateHerbAlertDialogViewModel>();

            // Issue #2246: 注册处方面板专用弹窗（带预览功能）
            containerRegistry.RegisterDialog<FormulaImportDialog, FormulaImportDialogViewModel>();
            containerRegistry.RegisterDialog<HistoryCopyDialog, HistoryCopyDialogViewModel>();

            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 审计日志对话框
            containerRegistry.RegisterDialog<AuditLogDialog, AuditLogDialogViewModel>();

            // OpenSpec: medicalcase-management-ui-refactor (EDITMODE-008) - 未保存修改确认对话框
            containerRegistry.RegisterDialog<UnsavedChangesDialog, UnsavedChangesDialogViewModel>();

            // OpenSpec: medicalcase-management-ui-refactor (EDITMODE-011) - 审计理由对话框
            containerRegistry.RegisterDialog<AuditReasonDialog, AuditReasonDialogViewModel>();

            // 注册视图用于导航
            // Issue #1549: MedicalCaseEntryView已删除（由MedicalCaseFlowView的4步流程替代）
            // Epic #1583: PatientSelectionView已移至PatientsModule（三区域布局）
            // OpenSpec: refactor-medicalcase-ui - 废弃视图注册已清理（MedicalCaseFlowView, MedicalCaseEditorView, CompletionView）

            // Epic #2210 Phase 4: 4:6统一工作区视图（唯一的看诊入口）
            containerRegistry.RegisterForNavigation<Views.MedicalCaseWorkspaceView>();
            // [已移除] PrescriptionEditorView - 死代码，从未导航到（OpenSpec: refactor-viewmodel-layer）
            // Issue #1799: 删除OtherCasesQueryView（违反AR-001聚合根约束）
            // Issue #1799: 删除MedicalCaseListView（功能与ManagementView重复）
            containerRegistry.RegisterForNavigation<Views.MedicalCaseManagementView>();  // Issue #1799: 保留作为唯一医案管理入口
            containerRegistry.RegisterForNavigation<Views.MedicalCaseDetailView>();  // Issue #2167: 医案详情视图
        }
    }
}
