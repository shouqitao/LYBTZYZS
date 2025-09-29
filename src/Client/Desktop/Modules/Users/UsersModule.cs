using LYBT.Desktop.Users.Services;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Users
{
    /// <summary>
    /// 用户管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(UsersModule))]
    [ModuleDependency("AuthenticationModule")] // 用户模块依赖认证
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

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<UserManagementViewModel>();
            containerRegistry.Register<UserCreateViewModel>();
            containerRegistry.Register<UserEditViewModel>();

            // 注册实际存在的视图用于导航
            containerRegistry.RegisterForNavigation<Views.UserManagementView>();
            containerRegistry.RegisterForNavigation<Views.UserDetailView>();
            
            // 注册对话框视图
            containerRegistry.RegisterDialog<Views.ChangePasswordDialog>();
            containerRegistry.RegisterDialog<Views.ResetPasswordDialog>();
            containerRegistry.RegisterDialog<Views.UserProfileDialog>();
        }
    }
}
