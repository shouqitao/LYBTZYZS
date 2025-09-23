using LYBT.Desktop.Users.Services;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Desktop.Users.Views;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Users
{

    /// <summary>
    /// 用户管理模块 - 独立业务模块
    /// 对应后端: LYBT.Module.Users
    /// 遵循原始设计：前后端模块一一对应
    /// </summary>
    public class UsersModule : IModule
    {

        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后的操作
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Prism 8.x最佳实践：模块自己注册完整的服务体系

            // UltraThink双层架构服务注册
            containerRegistry.Register<LYBT.Desktop.Users.Interfaces.IUserQueryService,
                LYBT.Desktop.Users.Services.UserQueryService>();
            containerRegistry.Register<LYBT.Desktop.Users.Interfaces.IUserBusinessService,
                LYBT.Desktop.Users.Services.UserBusinessService>();

            // 主要模块服务（单例模式，整个应用生命周期内唯一）
            containerRegistry.RegisterSingleton<UserService>();
            containerRegistry.RegisterSingleton<IUserService>(container => container.Resolve<UserService>());

            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<UserManagementView, UserManagementViewModel>();
            containerRegistry.RegisterForNavigation<UserAddEditDialog, UserAddEditDialogViewModel>();
        }
    }
}
