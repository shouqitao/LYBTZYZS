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

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.AdminHomeView>();
        }
    }
}
