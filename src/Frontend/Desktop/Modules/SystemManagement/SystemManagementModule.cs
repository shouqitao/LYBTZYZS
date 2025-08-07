using LYBT.WPF.Client.Modules.SystemManagement.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Users.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Users.ViewModels;
using LYBT.WPF.Client.Modules.SystemManagement.Herbs.Views;
using LYBT.WPF.Client.Modules.SystemManagement.PrescriptionTemplates.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Patients.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Prescriptions.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Formulas.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Formulas.ViewModels;
using LYBT.WPF.Client.Modules.SystemManagement.Consultations.Views;
using LYBT.WPF.Client.Modules.SystemManagement.Consultations.ViewModels;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

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
            
            // 注册自定义的ViewModel映射
            ViewModelLocationProvider.Register<UserManagementView, UserManagementViewModelSimple>();
            ViewModelLocationProvider.Register<FormulaManagementView, FormulaManagementViewModel>();
            ViewModelLocationProvider.Register<HerbManagementView, Herbs.ViewModels.HerbManagementViewModelRefactored>();
            ViewModelLocationProvider.Register<PatientManagementView, Patients.ViewModels.PatientManagementViewModelRefactored>();
            ViewModelLocationProvider.Register<ConsultationManagementView, ConsultationManagementViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册系统管理主视图
            containerRegistry.RegisterForNavigation<AdminMainView>();
            
            // 注册用户管理视图
            containerRegistry.RegisterForNavigation<UserManagementView>();
            
            // 注册患者管理视图
            containerRegistry.RegisterForNavigation<PatientManagementView>();
            
            // 注册看诊记录管理视图
            containerRegistry.RegisterForNavigation<ConsultationManagementView>();
            
            
            // 注册中药材管理视图
            containerRegistry.RegisterForNavigation<HerbManagementView>();
            
            // 注册处方模板管理视图
            containerRegistry.RegisterForNavigation<PrescriptionTemplatesView>();
            
            // 注册处方管理视图
            containerRegistry.RegisterForNavigation<PrescriptionManagementView>();
            
            // 注册验方管理视图
            containerRegistry.RegisterForNavigation<FormulaManagementView>();
            
            // 注册对话框
            RegisterDialogs(containerRegistry);
        }
        
        private void RegisterDialogs(IContainerRegistry containerRegistry)
        {
            // 角色管理对话框
            // containerRegistry.RegisterDialog<ViewRoleDialog, ViewRoleDialogViewModel>(); // Temporarily disabled - IDialogAware not implemented
            
            // 中药材管理对话框
            // containerRegistry.RegisterDialog<Herbs.Views.ViewHerbDialog, Herbs.ViewModels.ViewHerbDialogViewModel>(); // Temporarily disabled - IDialogAware not implemented
            
            // 医生管理对话框
            // containerRegistry.RegisterDialog<Doctors.Views.ViewDoctorDialog, Doctors.ViewModels.ViewDoctorDialogViewModel>(); // Temporarily disabled - IDialogAware not implemented
            
            // 挂号管理对话框
            // containerRegistry.RegisterDialog<Registrations.Views.ViewRegistrationDialog, Registrations.ViewModels.ViewRegistrationDialogViewModel>(); // Temporarily disabled - IDialogAware not implemented
            
            // 病历管理对话框
            // containerRegistry.RegisterDialog<Records.Views.ViewRecordDialog, Records.ViewModels.ViewRecordDialogViewModel>(); // Temporarily disabled - IDialogAware not implemented
            
            // 验方模板管理对话框
            // containerRegistry.RegisterDialog<Formulas.Views.ViewFormulaDialog, Formulas.ViewModels.ViewFormulaDialogViewModel>(); // Temporarily disabled - IDialogAware not implemented
            
            // 处方管理对话框
            // containerRegistry.RegisterDialog<Prescriptions.Views.ViewPrescriptionDialog, Prescriptions.ViewModels.ViewPrescriptionDialogViewModel>(); // Temporarily disabled - IDialogAware not implemented
            
            // TODO: 注册其他对话框
        }
    }
}