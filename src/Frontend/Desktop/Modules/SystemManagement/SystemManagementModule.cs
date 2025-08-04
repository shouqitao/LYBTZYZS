using LYBT.WPF.Client.Modules.SystemManagement.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Users.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Herbs.Views;
using LYBT.WPF.Client.Modules.SystemManagement.PrescriptionTemplates.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Patients.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Records.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Prescriptions.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Roles.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Settings.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Backup.Views;
// using LYBT.WPF.Client.Modules.SystemManagement.Logs.Views; // TODO: 待实现
using LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Registrations.Views;
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
            containerRegistry.RegisterForNavigation<AdminMainView>();
            
            // 注册用户管理视图
            containerRegistry.RegisterForNavigation<UserManagementView>();
            
            // 注册患者管理视图
            containerRegistry.RegisterForNavigation<PatientManagementView>();
            
            // 注册病历管理视图
            containerRegistry.RegisterForNavigation<RecordManagementView>();
            
            // 注册角色权限管理视图
            containerRegistry.RegisterForNavigation<RoleManagementView>();
            
            // 注册系统设置视图
            containerRegistry.RegisterForNavigation<SystemSettingsView>();
            
            // 注册数据备份视图
            containerRegistry.RegisterForNavigation<BackupView>();
            
            // 注册系统日志视图
            // containerRegistry.RegisterForNavigation<SystemLogsView>(); // TODO: 待实现
            
            // 注册中药材管理视图
            containerRegistry.RegisterForNavigation<HerbManagementView>();
            
            // 注册处方模板管理视图
            containerRegistry.RegisterForNavigation<PrescriptionTemplatesView>();
            
            // 注册处方管理视图
            containerRegistry.RegisterForNavigation<PrescriptionManagementView>();
            
            // 注册验方模板管理视图
            containerRegistry.RegisterForNavigation<FormulaTemplateManagementView>();
            
            // 注册挂号管理视图
            containerRegistry.RegisterForNavigation<RegistrationManagementView>();
        }
    }
}