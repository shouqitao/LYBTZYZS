using LYBT.Desktop.Auth.Services;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Auth.Views;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Auth
{

    /// <summary>
    /// 认证模块 - UltraThink架构标准版本
    /// 注册模块化服务和依赖项，符合模块自包含原则
    /// </summary>
    [Module(ModuleName = nameof(AuthenticationModule))]
    // 认证模块是基础模块，无依赖
    public class AuthenticationModule : IModule
    {

        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
            var logger = containerProvider.Resolve<ILogger<AuthenticationModule>>();
            logger?.LogInformation("Auth模块初始化完成 - UltraThink架构");
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // UltraThink模块化架构：注册模块业务服务
            containerRegistry.RegisterSingleton<AuthService>();

            // 注册视图模型
            containerRegistry.Register<LoginViewModel>();

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<LoginView>();
        }
    }
}
