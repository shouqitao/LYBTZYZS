using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.MedicalCase.Controls;
using LYBT.Desktop.MedicalCase.Dialogs;
// SYNC-D02: IMedicalCaseRepository 已迁移到 Contracts.Repositories
using LYBT.Desktop.Contracts.Repositories;
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
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase
{
    /// <summary>
    /// 医疗案例管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(MedicalCaseModule))]
    [ModuleDependency("PatientsModule")] // 医案依赖患者
    [ModuleDependency("HerbsModule")] // 运行时需要 IHerbSearchProvider 已注册
    [ModuleDependency("FormulaModule")] // 运行时需要 IFormulaSearchProvider 已注册
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
            ViewModelLocationProvider.Register(typeof(MedicalCaseMasterDetailControl).ToString(), typeof(ViewModels.MedicalCaseMasterDetailViewModel));

            // IMedicalCaseRepository 由 Shell DI 注册 (Refit API)
            // [已移除] IPrescriptionPrintService, PrescriptionPrintService

            // Epic #1773: 注册Component组件
            containerRegistry.Register<IMedicalCaseService, MedicalCaseService>();
            // 遵循依赖倒置原则：其他模块可按需依赖特定职责的接口
            containerRegistry.Register<IMedicalCaseQueryService, MedicalCaseService>();
            containerRegistry.Register<IMedicalCaseCommandService, MedicalCaseService>();
            containerRegistry.Register<IMedicalCaseLifecycleService, MedicalCaseService>();

            // Issue #1806: 注册MedicalCaseFlowViewModel组件化服务（Epic #1805 Phase 2）
            // [已移除] MedicalCaseFlowManager - 三步流程已取消

            // StateMachine 已内联到 MedicalCaseWorkspaceViewModel
            // [已移除] PrescriptionItemHandler - 功能由 HerbListControl 内部处理

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

            // Epic #2210 Phase 4: 4:6统一工作区视图模型
            // Issue #1799: 删除OtherCasesQueryViewModel（违反AR-001聚合根约束）
            // Issue #1799: 删除MedicalCaseListViewModel（功能与ManagementView重复）
            containerRegistry.RegisterSingleton<MedicalCaseDetailModelMapper>();
            containerRegistry.Register<ViewModels.MedicalCaseMasterDetailViewModel>();

            // [已删除] FormulaSelectionDialog - 过时代码，已被FormulaImportDialog替代

            // Issue #2246: 注册处方面板专用弹窗（带预览功能）
            containerRegistry.RegisterDialog<FormulaImportDialog, FormulaImportDialogViewModel>();
            containerRegistry.RegisterDialog<HistoryCopyDialog, HistoryCopyDialogViewModel>();
            containerRegistry.RegisterDialog<UnsavedChangesDialog, UnsavedChangesDialogViewModel>();

            // 注册视图用于导航
            // Issue #1549: MedicalCaseEntryView已删除（由MedicalCaseFlowView的4步流程替代）
            // Epic #1583: PatientSelectionView已移至PatientsModule（三区域布局）

            // Epic #2210 Phase 4: 4:6统一工作区视图
            // Issue #1799: 删除OtherCasesQueryView（违反AR-001聚合根约束）
            // Issue #1799: 删除MedicalCaseListView（功能与ManagementView重复）
            // 注册MedicalCase模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel>();
            // MedicalCaseMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.MedicalCaseMasterDetailViewModel>();
            
            // 注册MasterDetail View用于导航
            containerRegistry.RegisterForNavigation<Views.MedicalCaseMasterDetailView>();

            // 审计日志视图
            containerRegistry.Register<ViewModels.AuditLogViewModel>();
            containerRegistry.RegisterForNavigation<Views.AuditLogView>();
        }
    }
}
