using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Admin
{
    /// <summary>
    /// 管理员角色模块
    /// 功能：管理员工作台主页，提供系统管理功能导航入口
    /// </summary>
    [Module(ModuleName = nameof(AdminModule))]
    public class AdminModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图模型
            containerRegistry.Register<ViewModels.AdminHomeViewModel>();
            containerRegistry.Register<ViewModels.SystemSettingsViewModel>();
            containerRegistry.Register<ViewModels.UserManagementViewModel>();
            // D6: DP10 收口——SystemSettingsViewModel 经服务门面访问 IApiClient.Configuration
            containerRegistry.Register<Services.IServerConfigurationService, Services.ServerConfigurationService>();

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.AdminHomeView>();
            containerRegistry.RegisterForNavigation<Views.SystemSettingsView>();
            // View在角色台，Control在业务模块；UserManagementViewModel 消费 DefaultRoleFilter
            containerRegistry.RegisterForNavigation<Views.UserManagementView, ViewModels.UserManagementViewModel>();
        }
    }
}
