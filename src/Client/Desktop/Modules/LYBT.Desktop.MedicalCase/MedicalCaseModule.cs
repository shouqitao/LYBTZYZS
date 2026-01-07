// OpenSpec: standardize-module-structure - Components已合并到Services
using LYBT.Desktop.Infrastructure.DependencyInjection;
// OpenSpec: migrate-views-to-role-modules - AuditLogDialog/AuditReasonDialog已删除，审计功能后续单独规划
using LYBT.Desktop.MedicalCase.Dialogs;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.MedicalCase.Services; // Issue #1790: 引入Manager服务
using LYBT.Desktop.Modules.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.MedicalCase
{
    /// <summary>
    /// 医疗案例管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(MedicalCaseModule))]
    [ModuleDependency("PatientsModule")] // 病历依赖患者
    // [已移除] PrescriptionsModule依赖 - 所有功能已迁移到本模块
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

            // OpenSpec: standardize-api-architecture - MappingService已删除，使用直接Mapper实例

            // OpenSpec: create-printing-module - 处方打印服务已迁移到独立Printing模块
            // [已移除] IPrescriptionPrintService, PrescriptionPrintService

            // OpenSpec: migrate-views-to-role-modules - 审计功能后续单独规划
            // [已删除] IAuditRequirementChecker, AuditRequirementChecker

            // Epic #1773: 注册Component组件
            // OpenSpec: simplify-medicalcase-api - 注册为接口供Consultation模块使用
            // OpenSpec: standardize-service-layer - 统一使用Service命名
            containerRegistry.Register<IMedicalCaseService, MedicalCaseService>();

            // Issue #1806: 注册MedicalCaseFlowViewModel组件化服务（Epic #1805 Phase 2）
            // [已移除] MedicalCaseFlowManager - 三步流程已取消
            // OpenSpec: simplify-medicalcase-module - MedicalCaseLifecycleHandler已合并到IMedicalCaseService
            containerRegistry.RegisterScoped<MedicalCaseDataLoader>();

            // OpenSpec: refactor-viewmodel-layer - 工作区协调器
            containerRegistry.RegisterScoped<ViewModels.Components.MedicalCaseWorkspaceCoordinator>();

            // OpenSpec: refactor-viewmodel-layer Phase 1 - 编辑模式状态机
            containerRegistry.RegisterScoped<ViewModels.Components.MedicalCaseEditModeStateMachine>();

            // OpenSpec: refactor-viewmodel-layer Phase 5 - 导航处理器
            containerRegistry.RegisterScoped<MedicalCaseNavigationHandler>();

            // OpenSpec: cleanup-ui-layer - Phase 1.1 PrescriptionPanel Components
            // OpenSpec: slim-medicalcase-workspace-viewmodel - Phase 5 移除 PrescriptionItemHandler
            // [已移除] PrescriptionCalculator - 死代码，价格计算已由PrescriptionItem内部实现 (OpenSpec: simplify-medicalcase-module)
            // [已移除] PrescriptionValidator - 死代码，功能由ViewModel内部Adapter实现 (OpenSpec: simplify-medicalcase-module)
            // [已移除] PrescriptionItemHandler - 功能由 HerbListControl 内部处理
            // [已移除] PrescriptionSaveHandler - 死代码，从未被使用 (OpenSpec: simplify-medicalcase-module)
            containerRegistry.Register<ViewModels.Components.PrescriptionImportHandler>();
            // [已移除] PrescriptionDataLoader - 死代码，从未被使用 (OpenSpec: simplify-medicalcase-module)

            // Issue #1807: 注册PrescriptionEditorViewModel组件化服务 Phase 2
            // [已移除] PrescriptionCalculator重复注册 - 上方已注册
            // [已移除] FormulaImportHandler - 死代码，功能已由FormulaImportDialog + PrescriptionImportHandler实现
            // [已移除] HerbSelectionManager - 死代码，从未被使用

            // Issue #1548: CreateMedicalCaseDialog已删除（由MedicalCaseFlowView的4步流程替代）
            // Phase 3.4: 启用 Prism Dialog 注册（已废弃）
            // containerRegistry.RegisterDialog<Views.CreateMedicalCaseDialog, ViewModels.CreateMedicalCaseDialogViewModel>();

            // 注册视图模型 - MVP核心功能
            // Issue #1549: MedicalCaseEntryViewModel已删除（由MedicalCaseFlowView的4步流程替代）
            // Epic #1583: PatientSelectionViewModel已移至PatientsModule（三区域布局）
            // OpenSpec: unify-frontend-backend-types Phase 8 - PrescriptionItemViewModel已合并到PrescriptionHerbItem
            // PrescriptionHerbItem已被HerbItemDto替代，由HerbListControl内部管理
            // OpenSpec: refactor-medicalcase-ui - 废弃注册已清理

            // Epic #2210 Phase 4: 4:6统一工作区视图模型
            // OpenSpec: refactor-clinical-workflow - MedicalCaseWorkspaceViewModel已迁移到ClinicalModule
            // OpenSpec: consolidate-panel-viewmodels - ConsultationPanelViewModel和PrescriptionPanelViewModel已删除，改用Item模式

            // [已移除] PrescriptionEditorViewModel - 死代码，从未导航到（OpenSpec: refactor-viewmodel-layer）
            // Issue #1799: 删除OtherCasesQueryViewModel（违反AR-001聚合根约束）
            // Issue #1799: 删除MedicalCaseListViewModel（功能与ManagementView重复）
            // OpenSpec: migrate-views-to-role-modules - MedicalCaseDetailViewModel已删除（无调用）

            // OpenSpec: refactor-medicalcase-management - Master-Detail视图模型
            containerRegistry.Register<ViewModels.MedicalCaseMasterDetailViewModel>();

            // [已删除] FormulaSelectionDialog - 过时代码，已被FormulaImportDialog替代

            // OpenSpec: migrate-views-to-role-modules - HistoryPrescriptionSelectionDialog/DuplicateHerbAlertDialog已删除（无调用）

            // Issue #2246: 注册处方面板专用弹窗（带预览功能）
            containerRegistry.RegisterDialog<FormulaImportDialog, FormulaImportDialogViewModel>();
            containerRegistry.RegisterDialog<HistoryCopyDialog, HistoryCopyDialogViewModel>();

            // OpenSpec: medicalcase-management-ui-refactor (EDITMODE-008) - 未保存修改确认对话框
            containerRegistry.RegisterDialog<UnsavedChangesDialog, UnsavedChangesDialogViewModel>();

            // OpenSpec: migrate-views-to-role-modules - 审计功能将来单独规划，临时移除
            // [已删除] AuditLogDialog, AuditReasonDialog

            // 注册视图用于导航
            // Issue #1549: MedicalCaseEntryView已删除（由MedicalCaseFlowView的4步流程替代）
            // Epic #1583: PatientSelectionView已移至PatientsModule（三区域布局）
            // OpenSpec: refactor-medicalcase-ui - 废弃视图注册已清理（MedicalCaseFlowView, MedicalCaseEditorView, CompletionView）

            // Epic #2210 Phase 4: 4:6统一工作区视图
            // OpenSpec: refactor-clinical-workflow - MedicalCaseWorkspaceView已迁移到ClinicalModule
            // [已移除] PrescriptionEditorView - 死代码，从未导航到（OpenSpec: refactor-viewmodel-layer）
            // Issue #1799: 删除OtherCasesQueryView（违反AR-001聚合根约束）
            // Issue #1799: 删除MedicalCaseListView（功能与ManagementView重复）
            // OpenSpec: migrate-views-to-role-modules - MedicalCaseDetailView/MedicalCaseWorkspaceView已删除（无调用/重复）

            // OpenSpec: refactor-viewmodel-composition - V2组合模式ViewModel
            // 注册MedicalCase模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel>();

            // OpenSpec: refactor-admin-workspace - Control模式重构
            // MedicalCaseMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.MedicalCaseMasterDetailViewModel>();
        }
    }
}
