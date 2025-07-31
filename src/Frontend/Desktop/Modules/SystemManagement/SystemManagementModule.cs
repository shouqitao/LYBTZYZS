using LYBT.WPF.Client.Modules.SystemManagement.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Users.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Herbs.Views;
// using LYBT.WPF.Client.Modules.SystemManagement.PrescriptionTemplates.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Patients.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Records.Views;
// using LYBT.Frontend.Desktop.Modules.SystemManagement.Prescriptions.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.Modules.SystemManagement
{
    /// <summary>
    /// 系统管理模块
    /// </summary>
    public class SystemManagementModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册系统管理主视图
            containerRegistry.RegisterForNavigation<SystemManagementView>();
            
            // 注册用户管理视图
            containerRegistry.RegisterForNavigation<UserManagementView>();
            
            // 注册中药材管理视图
            containerRegistry.RegisterForNavigation<HerbManagementView>();
            
            // 暂时注释处方模板管理视图 - 模块未实现
            // containerRegistry.RegisterForNavigation<PrescriptionTemplateManagementView>();
            
            // 暂时注释处方管理视图 - 模块未实现  
            // containerRegistry.RegisterForNavigation<PrescriptionManagementView>();
            
            // 注册患者管理视图
            containerRegistry.RegisterForNavigation<PatientManagementView>();
            
            // 注册病历管理视图
            containerRegistry.RegisterForNavigation<RecordManagementView>();
        }
    }
}