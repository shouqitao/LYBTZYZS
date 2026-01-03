using FluentValidation;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.DependencyInjection;
// OpenSpec: standardize-module-structure - Components已合并到Services
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Validators.Patients;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Patients
{
    /// <summary>
    /// 患者管理模块 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    [Module(ModuleName = nameof(PatientsModule))]
    [ModuleDependency("AuthenticationModule")]
    [ModuleDependency("UsersModule")]
    public class PatientsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Phase 2：Repository由模块自己注册
            containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();

            // 注册FluentValidation验证器
            containerRegistry.Register<IValidator<PatientInputDto>, PatientInputDtoValidator>();

            // Issue #1781 Task 8 Phase 1: 注册Excel解析服务（Singleton生命周期）
            containerRegistry.RegisterSingleton<IExcelParserService, ExcelParserService>();

            // Issue #1790: 注册患者导入服务
            containerRegistry.RegisterSingleton<Services.PatientImportDataMapper>();
            containerRegistry.Register<Services.PatientImportExecutor>();

            // Issue #1790: 注册PatientSelectionViewModel组件化服务
            containerRegistry.Register<PatientSearchManager>();
            containerRegistry.RegisterSingleton<IPatientSearchCache, PatientSearchCache>();  // OpenSpec: refactor-patient-selection - 搜索缓存
            containerRegistry.Register<UnfinishedCaseHandler>();
            containerRegistry.Register<PendingQueueManager>();

            // OpenSpec: cleanup-ui-layer Phase 1.2 - 注册医案启动协调器
            containerRegistry.Register<ViewModels.Components.MedicalCaseStartCoordinator>();

            // Epic #1773 Task 4: 注册患者模块组件化组件（Scoped生命周期）
            containerRegistry.Register<ViewModels.Components.PatientStateManager>();
            containerRegistry.Register<ViewModels.Components.PatientService>();
            containerRegistry.Register<ViewModels.Components.PatientValidator>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.PatientDetailViewModel>();
            // Issue #2167: PatientImportWizardViewModel已删除（改用直接API调用）
            // [已移除] PatientSelectionViewModel - 已迁移到LYBT.Desktop.Clinical模块
            // OpenSpec: refactor-clinical-workflow
            // Issue #2168: CRUD统一架构 - PatientCreateViewModel和PatientEditViewModel已删除

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.PatientDetailView>();
            // Issue #2167: PatientImportWizardView已删除（改用直接API调用）
            // [已移除] PatientSelectionView - 已迁移到LYBT.Desktop.Clinical模块
            // OpenSpec: refactor-clinical-workflow
            // Issue #2168: CRUD统一架构 - PatientDetailView支持Create/Edit/View三种模式
            // PatientCreateView和PatientEditView已删除，统一使用PatientDetailView

            // Issue #1547: PatientSelectionDialog已删除（由MedicalCaseFlowView的Step 1替代）
            // containerRegistry.RegisterDialog<Views.PatientSelectionDialog, ViewModels.PatientSelectionDialogViewModel>();

            // Issue #1487: 快速创建患者对话框
            containerRegistry.RegisterDialog<Views.QuickCreatePatientDialog, ViewModels.QuickCreatePatientDialogViewModel>();

            // OpenSpec: refactor-viewmodel-composition - V2组合模式ViewModel
            // 注册Patients模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<PatientListDto, PatientDetailModel>();

            // OpenSpec: refactor-admin-workspace - Control模式重构
            // PatientMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.PatientMasterDetailViewModel>();
        }
    }
}
