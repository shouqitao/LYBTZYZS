using LYBT.Shared.Models.Contracts.Common;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using LYBT.Desktop.Workbench.Receptionist.Views;
using LYBT.Desktop.Workbench.Receptionist.ViewModels;

namespace LYBT.Desktop.Workbench.Receptionist
{
    /// <summary>
    /// 前台工作台模块
    /// </summary>
    public class ReceptionistWorkbenchModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
            
            // 注册自定义的ViewModel映射
            ViewModelLocationProvider.Register<ReceptionistMainView, ReceptionistMainViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册前台工作台主视图
            containerRegistry.RegisterForNavigation<ReceptionistMainView>();
            
            // 注册占位视图
            containerRegistry.RegisterForNavigation<PatientReceptionView>();
            containerRegistry.RegisterForNavigation<AppointmentManagementView>();
            containerRegistry.RegisterForNavigation<BasicRegistrationView>();
            
            // TODO: 注册其他视图和服务
        }
    }
}