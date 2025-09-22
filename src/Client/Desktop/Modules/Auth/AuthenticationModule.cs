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
            // 服务注册已移至Shell/Extensions/ServiceCollectionExtensions.cs集中管理
            // 此处仅注册视图和视图模型用于导航
            containerRegistry.Register<LoginViewModel>();
            containerRegistry.RegisterForNavigation<LoginView>();
        }
    }
}
