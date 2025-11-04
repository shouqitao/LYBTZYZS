using LYBT.Desktop.MedicalCase.Components; // Epic #1773: 添加Component命名空间
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
    // ✅ 移除ConsultationModule依赖 - MedicalCase是聚合根，不应依赖子实体模块 (Issue #1463)
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

            // Epic #1773: 注册Component组件
            containerRegistry.Register<MedicalCaseDataManager>();

            // Issue #1806: 注册MedicalCaseFlowViewModel组件化服务（Epic #1805 Phase 2）
            containerRegistry.RegisterScoped<MedicalCaseFlowManager>();
            containerRegistry.RegisterScoped<MedicalCaseLifecycleHandler>();
            containerRegistry.RegisterScoped<MedicalCaseDataLoader>();

            // Issue #1790: 注册PrescriptionEditorViewModel组件化服务
            containerRegistry.Register<PrescriptionEditorHerbFilterManager>();
            containerRegistry.Register<PrescriptionEditorValidator>();

            // Issue #1807: 注册PrescriptionEditorViewModel组件化服务 Phase 2
            containerRegistry.Register<PrescriptionCalculator>();
            containerRegistry.Register<FormulaImportHandler>();
            containerRegistry.Register<HerbSelectionManager>();

            // Issue #1548: CreateMedicalCaseDialog已删除（由MedicalCaseFlowView的4步流程替代）
            // Phase 3.4: 启用 Prism Dialog 注册（已废弃）
            // containerRegistry.RegisterDialog<Views.CreateMedicalCaseDialog, ViewModels.CreateMedicalCaseDialogViewModel>();

            // 注册视图模型 - MVP核心功能
            // Issue #1549: MedicalCaseEntryViewModel已删除（由MedicalCaseFlowView的4步流程替代）
            // containerRegistry.Register<ViewModels.MedicalCaseEntryViewModel>();  // Issue #1463: 病案录入（已废弃）
            // Epic #1583: PatientSelectionViewModel已移至PatientsModule（三区域布局）
            containerRegistry.Register<ViewModels.MedicalCaseFlowViewModel>();   // Epic #1494 - Task #1496: 医案流程主视图模型
            containerRegistry.Register<ViewModels.PrescriptionEditorViewModel>();  // Task #1499: 处方编辑器
            containerRegistry.Register<ViewModels.CompletionViewModel>();        // Epic #1494 - Task #1500: Step 4 完成医案
            // Issue #1799: 删除OtherCasesQueryViewModel（违反AR-001聚合根约束）
            // Issue #1799: 删除MedicalCaseListViewModel（功能与ManagementView重复）
            containerRegistry.Register<ViewModels.MedicalCaseManagementViewModel>();  // Issue #1799: 保留作为唯一医案管理入口

            // 注册视图用于导航 - 需要对应视图文件存在
            // Issue #1549: MedicalCaseEntryView已删除（由MedicalCaseFlowView的4步流程替代）
            // containerRegistry.RegisterForNavigation<Views.MedicalCaseEntryView>();  // Issue #1463: 病案录入视图（已废弃）
            // Epic #1583: PatientSelectionView已移至PatientsModule（三区域布局）
            containerRegistry.RegisterForNavigation<Views.MedicalCaseFlowView>();   // Epic #1494 - Task #1496: 医案流程主视图
            containerRegistry.RegisterForNavigation<Views.PrescriptionEditorView>();  // Task #1499: 处方编辑器视图
            containerRegistry.RegisterForNavigation<Views.CompletionView>();        // Epic #1494 - Task #1500: Step 4 完成医案视图
            // Issue #1799: 删除OtherCasesQueryView（违反AR-001聚合根约束）
            // Issue #1799: 删除MedicalCaseListView（功能与ManagementView重复）
            containerRegistry.RegisterForNavigation<Views.MedicalCaseManagementView>();  // Issue #1799: 保留作为唯一医案管理入口
        }
    }
}
