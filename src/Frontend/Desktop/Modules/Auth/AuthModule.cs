using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.Modules.Auth.ViewModels;
using LYBT.WPF.Client.Modules.Auth.Views;

namespace LYBT.WPF.Client.Modules.Auth
{
    /// <summary>
    /// 认证授权模块
    /// </summary>
    public class AuthModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化后的操作
            // 注意：认证模块通常在应用启动时就需要，所以可能需要特殊处理
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
            containerRegistry.RegisterForNavigation<ChangePasswordDialog, ChangePasswordDialogViewModel>();
            
            // 注册其他服务（如果需要模块特定的服务）
        }
    }
}