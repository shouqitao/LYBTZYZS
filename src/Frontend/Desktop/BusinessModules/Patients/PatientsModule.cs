using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.BusinessModules.Patients.ViewModels;
using LYBT.WPF.Client.BusinessModules.Patients.Views;
using LYBT.WPF.Client.BusinessModules.Patients.Services;
using LYBT.WPF.Client.BusinessModules.Shared;

namespace LYBT.WPF.Client.BusinessModules.Patients
{
    /// <summary>
    /// 患者管理模�?- 独立业务模块
    /// 对应后端: LYBT.Module.Patients  
    /// 遵循原始设计：前后端模块一一对应
    /// </summary>
    public class PatientsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后的操�?
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册共享服务
            containerRegistry.RegisterSingleton<ISharedPatientService, SharedPatientService>();
            
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<PatientManagementView, PatientManagementViewModelSimple>();
            containerRegistry.RegisterForNavigation<PatientAddEditDialog, PatientAddEditDialogViewModel>();
        }
    }
}