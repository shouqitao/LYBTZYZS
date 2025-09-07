using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Patients.Views;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Patients
{

    /// <summary>
    /// 患者管理模块 - 独立业务模块
    /// 对应后端: LYBT.Module.Patients
    /// 遵循原始设计：前后端模块一一对应
    /// </summary>
    public class PatientsModule : IModule
    {

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后的操作
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // UltraThink双层架构服务注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Patients.Interfaces.IPatientQueryService, PatientQueryService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Patients.Interfaces.IPatientBusinessService, PatientBusinessService>();

            // UltraThink纯委托主服务注册
            containerRegistry.RegisterSingleton<Services.PatientModule>();
            containerRegistry.RegisterSingleton<IPatientService>(container => container.Resolve<Services.PatientModule>());

            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<PatientManagementView, PatientManagementViewModel>();
            containerRegistry.RegisterForNavigation<PatientAddEditDialog, PatientAddEditDialogViewModel>();
            containerRegistry.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();
            containerRegistry.RegisterForNavigation<PatientImportWizardView, PatientImportWizardViewModel>();
        }
    }
}
