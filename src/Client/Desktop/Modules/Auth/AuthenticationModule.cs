using LYBT.Shared.Models.Contracts.Common;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using LYBT.Desktop.Auth.Views;

namespace LYBT.Desktop.Auth
{
    /// <summary>
    /// 认证模块
    /// </summary>
    public class AuthenticationModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // TODO: 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<LoginView>();
        }
    }
}