using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.Repositories;
using LYBT.Shared.Models.Contracts.Users;
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
            containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

            // Issue #1785: 注册Users模块组件化组件（Epic #1773 Component-Based架构）
            // OpenSpec: standardize-service-layer - 统一使用Service命名
            containerRegistry.Register<ViewModels.Components.UserService>();

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.UserDetailView>();

            // Issue #1927 & #2168: CRUD统一架构 - UserDetailView支持Create/Edit/View三种模式
            // UserCreateView和UserEditView已删除，统一使用UserDetailView

            // Issue #1928: Sprint 2 - ResetPassword迁移为Navigation模式
            // Issue #2167: ResetPasswordView已删除（改用按钮触发直接API调用）

            // Issue #1929: Sprint 3 - ChangePassword/UserProfile迁移为Navigation模式
            containerRegistry.RegisterForNavigation<Views.ChangePasswordView, ViewModels.ChangePasswordViewModel>();
            containerRegistry.RegisterForNavigation<Views.UserProfileView, ViewModels.UserProfileViewModel>();

            // Epic #1926 Sprint 4: Dialog已全部迁移为Navigation模式，以下DI注册已移除：
            // - ChangePasswordDialog → ChangePasswordView
            // - ResetPasswordDialog → 重置密码移至列表操作
            // - UserProfileDialog → UserProfileView
            // - UserFormDialog → UserDetailView（Issue #2168：统一Create/Edit/View模式）

            // OpenSpec: refactor-viewmodel-composition - V2组合模式ViewModel
            // 注册Users模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<UserListDto, UserDetailModel>();

            // OpenSpec: refactor-admin-workspace - Control模式重构
            // UserMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.UserMasterDetailViewModel>();
        }
    }
}
