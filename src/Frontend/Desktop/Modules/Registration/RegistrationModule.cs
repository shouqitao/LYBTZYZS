using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using LYBT.WPF.Client.Registration.Views;
using LYBT.WPF.Client.Registration.ViewModels;
using LYBT.WPF.Client.Registration.Services;
using LYBT.WPF.Client.Registration.Services.Interfaces;

namespace LYBT.WPF.Client.Registration
{
    /// <summary>
    /// 挂号管理模块
    /// </summary>
    public class RegistrationModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public RegistrationModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化逻辑
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册服务
            containerRegistry.RegisterScoped<IRegistrationApiService, RegistrationApiService>();
            containerRegistry.RegisterScoped<IRegistrationService, RegistrationService>();

            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<RegistrationMainView, RegistrationMainViewModel>();
            containerRegistry.RegisterForNavigation<AddRegistrationDialog, AddRegistrationDialogViewModel>();
            containerRegistry.RegisterForNavigation<RegistrationDetailDialog, RegistrationDetailDialogViewModel>();
            containerRegistry.RegisterForNavigation<CancelRegistrationDialog, CancelRegistrationDialogViewModel>();

            // 注册对话框
            containerRegistry.RegisterDialog<AddRegistrationDialog, AddRegistrationDialogViewModel>();
            containerRegistry.RegisterDialog<RegistrationDetailDialog, RegistrationDetailDialogViewModel>();
            containerRegistry.RegisterDialog<CancelRegistrationDialog, CancelRegistrationDialogViewModel>();
        }
    }
}