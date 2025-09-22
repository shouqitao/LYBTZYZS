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
            // 服务注册已移至Shell/Extensions/ServiceCollectionExtensions.cs集中管理
            // 此处仅注册视图和视图模型用于导航
            containerRegistry.RegisterForNavigation<UserManagementView, UserManagementViewModel>();
            containerRegistry.RegisterForNavigation<UserAddEditDialog, UserAddEditDialogViewModel>();
        }
    }
}
