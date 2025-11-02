using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Patients.Components;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Repositories;
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

            // Issue #1781 Task 8 Phase 1: 注册Excel解析服务（Singleton生命周期）
            containerRegistry.RegisterSingleton<IExcelParserService, ExcelParserService>();

            // Epic #1773 Task 4: 注册患者模块组件化组件（Scoped生命周期）
            containerRegistry.Register<ViewModels.Components.PatientDataManager>();
            containerRegistry.Register<ViewModels.Components.PatientCommandHandler>();
            containerRegistry.Register<ViewModels.Components.PatientValidator>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.PatientDetailViewModel>();
            containerRegistry.Register<ViewModels.PatientImportWizardViewModel>();
            containerRegistry.Register<ViewModels.PatientSelectionViewModel>();  // Issue #1557: 看诊流程Step 1

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.PatientDetailView>();
            containerRegistry.RegisterForNavigation<Views.PatientImportWizardView>();
            containerRegistry.RegisterForNavigation<Views.PatientSelectionView>();  // Issue #1557: 看诊流程Step 1（Region导航）

            // Issue #1547: PatientSelectionDialog已删除（由MedicalCaseFlowView的Step 1替代）
            // containerRegistry.RegisterDialog<Views.PatientSelectionDialog, ViewModels.PatientSelectionDialogViewModel>();

            // Issue #1487: 快速创建患者对话框
            containerRegistry.RegisterDialog<Views.QuickCreatePatientDialog, ViewModels.QuickCreatePatientDialogViewModel>();
        }
    }
}
