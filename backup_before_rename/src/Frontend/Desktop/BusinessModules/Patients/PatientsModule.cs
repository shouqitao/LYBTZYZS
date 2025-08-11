using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Patients.Shared.ViewModels;
using LYBT.Desktop.Patients.Shared.Views;
using LYBT.Desktop.Patients.Shared.Services;
using LYBT.Desktop.Shared;

namespace LYBT.Desktop.Patients.Shared
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