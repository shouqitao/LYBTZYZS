using FluentValidation;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Patients.Components;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Patients.Services; // Issue #1790: 引入Manager服务
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
            containerRegistry.Register<UnfinishedCaseHandler>();
            containerRegistry.Register<PendingQueueManager>();

            // Epic #1773 Task 4: 注册患者模块组件化组件（Scoped生命周期）
            containerRegistry.Register<ViewModels.Components.PatientDataManager>();
            containerRegistry.Register<ViewModels.Components.PatientCommandHandler>();
            containerRegistry.Register<ViewModels.Components.PatientValidator>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.PatientDetailViewModel>();
            // Issue #2167: PatientImportWizardViewModel已删除（改用直接API调用）
            containerRegistry.Register<ViewModels.PatientSelectionViewModel>();  // Issue #1557: 看诊流程Step 1
            containerRegistry.Register<ViewModels.PatientManagementViewModel>();  // 患者管理视图模型
            // Issue #2168: CRUD统一架构 - PatientCreateViewModel和PatientEditViewModel已删除

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.PatientDetailView>();
            // Issue #2167: PatientImportWizardView已删除（改用直接API调用）
            containerRegistry.RegisterForNavigation<Views.PatientSelectionView>();  // Issue #1557: 看诊流程Step 1（Region导航）
            containerRegistry.RegisterForNavigation<Views.PatientManagementView>();  // 患者管理视图

            // Issue #2168: CRUD统一架构 - PatientDetailView支持Create/Edit/View三种模式
            // PatientCreateView和PatientEditView已删除，统一使用PatientDetailView

            // Issue #1547: PatientSelectionDialog已删除（由MedicalCaseFlowView的Step 1替代）
            // containerRegistry.RegisterDialog<Views.PatientSelectionDialog, ViewModels.PatientSelectionDialogViewModel>();

            // Issue #1487: 快速创建患者对话框
            containerRegistry.RegisterDialog<Views.QuickCreatePatientDialog, ViewModels.QuickCreatePatientDialogViewModel>();
        }
    }
}
