using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Repositories;
using LYBT.Desktop.Users.ViewModels;
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
            // Issue #1128: 注册模块内 Repository（覆盖 Shell 层的旧 Repository）
            containerRegistry.RegisterRepository<IUserRepository, UserRepository>();

            // Issue #1785: 注册Users模块组件化组件（Epic #1773 Component-Based架构）
            containerRegistry.Register<ViewModels.Components.UserCommandHandler>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<UserManagementViewModel>();

            // 注册实际存在的视图用于导航
            containerRegistry.RegisterForNavigation<Views.UserManagementView>();
            containerRegistry.RegisterForNavigation<Views.UserDetailView>();

            // Issue #1927: Sprint 1 - Dialog迁移为Navigation模式
            containerRegistry.RegisterForNavigation<Views.UserCreateView, ViewModels.UserCreateViewModel>();
            containerRegistry.RegisterForNavigation<Views.UserEditView, ViewModels.UserEditViewModel>();

            // Issue #1928: Sprint 2 - ResetPassword迁移为Navigation模式
            containerRegistry.RegisterForNavigation<Views.ResetPasswordView, ViewModels.ResetPasswordViewModel>();

            // Issue #1929: Sprint 3 - ChangePassword/UserProfile迁移为Navigation模式
            containerRegistry.RegisterForNavigation<Views.ChangePasswordView, ViewModels.ChangePasswordViewModel>();
            containerRegistry.RegisterForNavigation<Views.UserProfileView, ViewModels.UserProfileViewModel>();

            // Phase 3: 启用 Prism Dialog 注册
            containerRegistry.RegisterDialog<Views.ChangePasswordDialog, ViewModels.ChangePasswordDialogViewModel>();
            containerRegistry.RegisterDialog<Views.ResetPasswordDialog, ViewModels.ResetPasswordDialogViewModel>();
            containerRegistry.RegisterDialog<Views.UserProfileDialog, ViewModels.UserProfileDialogViewModel>();

            // Issue #1798: 用户表单对话框（合并创建和编辑功能）
            containerRegistry.RegisterDialog<Views.UserFormDialog, ViewModels.UserFormDialogViewModel>();
        }
    }
}
