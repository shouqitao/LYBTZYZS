using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Patients
{
    /// <summary>
    /// 患者管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(PatientsModule))]
    [ModuleDependency("AuthenticationModule")]
    [ModuleDependency("UsersModule")] // 患者模块依赖用户和认证
    public class PatientsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Services由Core_New/Services统一注册，不在Module中注册

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.PatientDetailViewModel>();
            containerRegistry.Register<ViewModels.PatientImportWizardViewModel>();

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.PatientDetailView>();
            containerRegistry.RegisterForNavigation<Views.PatientImportWizardView>();
        }
    }
}
