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

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.PatientDetailViewModel>();
            containerRegistry.Register<ViewModels.PatientImportWizardViewModel>();

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.PatientDetailView>();
            containerRegistry.RegisterForNavigation<Views.PatientImportWizardView>();
        }
    }
}
