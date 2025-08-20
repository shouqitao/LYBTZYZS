using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Desktop.Users.Views;
using LYBT.Desktop.Users.Services;
using LYBT.Desktop.Users.Services.Interfaces;

namespace LYBT.Desktop.Users
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
            // UltraThink模块化架构：注册模块业务服务
            containerRegistry.RegisterSingleton<IUserModuleService, UserModuleService>();
            
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<UserManagementView, UserManagementViewModel>();
            containerRegistry.RegisterForNavigation<UserAddEditDialog, UserAddEditDialogViewModel>();
        }
    }
}