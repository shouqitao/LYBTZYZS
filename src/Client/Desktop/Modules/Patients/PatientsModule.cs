using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Patients.Views;
using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Patients.Coordinators;

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
            // UltraThink模块化架构：注册模块核心业务服务
            containerRegistry.RegisterSingleton<PatientModule>();
            
            // UltraThink P1重构：注册模块业务协调器
            containerRegistry.RegisterSingleton<PatientCoordinator>();
            
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<PatientManagementView, PatientManagementViewModel>();
            containerRegistry.RegisterForNavigation<PatientAddEditDialog, PatientAddEditDialogViewModel>();
            containerRegistry.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();
        }
    }
}