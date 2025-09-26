using LYBT.Desktop.Users.Services;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Users
{
    /// <summary>
    /// 用户管理模块 - 简化版
    /// </summary>
    public class UsersModule : IModule
    {
        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册简化的服务
            containerRegistry.RegisterSingleton<IUserService, UserService>();

            // TODO: 注册简化后的视图和视图模型
        }
    }
}
