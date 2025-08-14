using LYBT.Desktop.Admin.Views;
using LYBT.Desktop.Admin.Herbs.Views;
using LYBT.Desktop.Admin.PrescriptionTemplates.Views;
using LYBT.Desktop.Admin.Prescriptions.Views;
using LYBT.Desktop.Admin.Formulas.Views;
using LYBT.Desktop.Admin.Formulas.ViewModels;
using LYBT.Desktop.Admin.Consultations.Views;
using LYBT.Desktop.Admin.Consultations.ViewModels;
using LYBT.Desktop.MedicalCase.Views;
using LYBT.Desktop.MedicalCase.ViewModels;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Admin
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
            ViewModelLocationProvider.Register<FormulaManagementView, FormulaManagementViewModel>();
            ViewModelLocationProvider.Register<HerbManagementView, Herbs.ViewModels.HerbManagementViewModelRefactored>();
            ViewModelLocationProvider.Register<ConsultationManagementView, ConsultationManagementViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册系统管理主视图
            containerRegistry.RegisterForNavigation<AdminMainView>();

            // 注册医疗案例管理视图 (使用MedicalCase模块的实现)
            containerRegistry.RegisterForNavigation<MedicalCaseListView>("MedicalCaseManagementView");

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

            // 验方模板管理对话框
            // containerRegistry.RegisterDialog<Formulas.Views.ViewFormulaDialog, Formulas.ViewModels.ViewFormulaDialogViewModel>(); // Temporarily disabled - IDialogAware not implemented

            // 处方管理对话框
            // containerRegistry.RegisterDialog<Prescriptions.Views.ViewPrescriptionDialog, Prescriptions.ViewModels.ViewPrescriptionDialogViewModel>(); // Temporarily disabled - IDialogAware not implemented

            // TODO: 注册其他对话框
        }
    }
}