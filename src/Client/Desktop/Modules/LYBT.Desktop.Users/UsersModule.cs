using LYBT.Desktop.Infrastructure.DependencyInjection;
// SYNC-D02: IUserRepository 迁移到 Contracts.Repositories
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Users.Mappers;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.Models.Items;
using LYBT.Desktop.Users.Repositories;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.ViewModels.Handlers;
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
            // SYNC-D02: IUserRepository 由 Shell DI 工厂注册 (根据 IConnectionModeProvider 选择实现)
            // 远程模式 -> UserRepository (Users 模块)
            // 本地模式 -> LocalUserRepository (LocalData 模块)

            // OpenSpec: standardize-api-architecture - MappingService已删除，使用直接Mapper实例

            // Issue #1785: 注册Users模块组件化组件（Epic #1773 Component-Based架构）
            // OpenSpec: standardize-service-layer - 统一使用Service命名
            containerRegistry.Register<IUserService, Services.RemoteUserService>();

            // OpenSpec: refactor-frontend-srp-patterns - 注册Handler组件
            containerRegistry.Register<IUserPasswordHandler, UserPasswordHandler>();
            containerRegistry.Register<IUserStatusHandler, UserStatusHandler>();
            containerRegistry.Register<IUserImportExportHandler, UserImportExportHandler>();

            // OpenSpec: migrate-views-to-role-modules - UserDetailView已删除（无调用）

            // Issue #1928: Sprint 2 - ResetPassword迁移为Navigation模式
            // Issue #2167: ResetPasswordView已删除（改用按钮触发直接API调用）

            // OpenSpec: migrate-views-to-role-modules - ChangePasswordView/UserProfileView已迁移到Shell
            // 合并为AccountSettingsView（TabControl形式），由Shell/ViewModels/AccountSettingsViewModel统一管理

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
            
            // 注册MasterDetail View用于导航
            containerRegistry.RegisterForNavigation<Views.UserMasterDetailView>();
        }
    }
}
