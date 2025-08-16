using LYBT.Shared.Models.Contracts.Common;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using LYBT.Desktop.Workbench.Admin.ViewModels;
using LYBT.Desktop.Workbench.Admin.Views;
using LYBT.Desktop.Workbench.Admin.Views.Management.Users;
using LYBT.Desktop.Workbench.Admin.Views.Management.Herbs;
using LYBT.Desktop.Workbench.Admin.Views.Management.Formulas;
using LYBT.Desktop.Workbench.Admin.ViewModels.Management.Users;
using LYBT.Desktop.Workbench.Admin.ViewModels.Management.Herbs;
using LYBT.Desktop.Workbench.Admin.ViewModels.Management.Formulas;
using LYBT.Desktop.Workbench.Admin.Services;
using LYBT.Desktop.Workbench.Core;

namespace LYBT.Desktop.Workbench.Admin
{
    /// <summary>
    /// 系统管理工作台模块
    /// 为管理员提供统一的管理界面
    /// </summary>
    public class SystemWorkbenchModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 注册ViewModel映射
            ViewModelLocationProvider.Register<SystemWorkbenchMainView, SystemWorkbenchMainViewModel>();
            
            // 注册管理模块ViewModel映射
            ViewModelLocationProvider.Register<UserManagementView, UserManagementViewModel>();
            ViewModelLocationProvider.Register<HerbManagementView, HerbManagementViewModel>();
            ViewModelLocationProvider.Register<FormulaManagementView, FormulaManagementViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册工作台导航器
            containerRegistry.RegisterSingleton<ISystemWorkbenchNavigator, SystemWorkbenchNavigator>();
            
            // 注册主视图
            containerRegistry.RegisterForNavigation<SystemWorkbenchMainView>();
            
            // 注册业务管理模块视图
            containerRegistry.RegisterForNavigation<UserManagementView>("UserManagementView");
            containerRegistry.RegisterForNavigation<HerbManagementView>("HerbManagementView");
            containerRegistry.RegisterForNavigation<FormulaManagementView>("FormulaManagementView");
            
            // 注册系统管理功能视图（占位符实现）
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("RoleManagementView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("SystemSettingsView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("BackupManagementView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("SystemLogsView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("SystemInfoView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("LicenseManagementView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("ReportsView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("DashboardView");
            
            // 注册其他业务模块的占位符视图
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("PatientManagementView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("PatientReceptionView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("MedicalCaseManagementView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("ConsultationManagementView");
            containerRegistry.RegisterForNavigation<SystemFunctionPlaceholderView>("PrescriptionManagementView");
        }
    }
}