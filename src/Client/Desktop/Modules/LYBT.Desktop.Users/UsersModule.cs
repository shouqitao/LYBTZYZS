using LYBT.Desktop.Infrastructure.DependencyInjection;
// SYNC-D02: IUserRepository 迁移到 Contracts.Repositories
using LYBT.Desktop.Users.Controls;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.ViewModels.Handlers;
using LYBT.Shared.Models.Contracts.Users;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

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
            // Fix: Register ViewModel mapping for UserMasterDetailControl
            // Prism's default convention looks for UserMasterDetailControlViewModel (doesn't exist)
            // Map it to UserMasterDetailViewModel instead
            ViewModelLocationProvider.Register(typeof(UserMasterDetailControl).ToString(), typeof(ViewModels.UserMasterDetailViewModel));

            // IUserRepository 由 Shell DI 注册 (Refit API)

            // Issue #1785: 注册Users模块组件化组件（Epic #1773 Component-Based架构）
            containerRegistry.Register<IUserService, Services.RemoteUserService>();
            containerRegistry.Register<IUserPasswordHandler, UserPasswordHandler>();
            containerRegistry.Register<IUserStatusHandler, UserStatusHandler>();

            // Issue #1928: Sprint 2 - ResetPassword迁移为Navigation模式
            // Issue #2167: ResetPasswordView已删除（改用按钮触发直接API调用）
            // 合并为AccountSettingsView（TabControl形式），由Shell/ViewModels/AccountSettingsViewModel统一管理

            // Epic #1926 Sprint 4: Dialog已全部迁移为Navigation模式，以下DI注册已移除：
            // - ChangePasswordDialog → ChangePasswordView
            // - ResetPasswordDialog → 重置密码移至列表操作
            // - UserProfileDialog → UserProfileView
            // - UserFormDialog → UserDetailView（Issue #2168：统一Create/Edit/View模式）
            // 注册Users模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<UserListDto, UserDetailModel>();
            containerRegistry.Register<ViewModels.UserEditorViewModel>();
            // UserMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.UserMasterDetailViewModel>();
            

        }
    }
}
