using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.Modules.Users.ViewModels;
using LYBT.WPF.Client.Modules.Users.Views;

namespace LYBT.WPF.Client.Modules.Users
{
    /// <summary>
    /// 用户管理模块 - 独立业务模块
    /// 对应后端: LYBT.Module.Users
    /// 遵循原始设计：前后端模块一一对应
    /// </summary>
    public class UsersModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后的操作
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<UserManagementView, UserManagementViewModelSimple>();
            containerRegistry.RegisterForNavigation<UserAddEditDialog, UserAddEditDialogViewModel>();
        }
    }
}