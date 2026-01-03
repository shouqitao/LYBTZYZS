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

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.AdminHomeView>();
            containerRegistry.RegisterForNavigation<Views.SystemSettingsView>();

            // OpenSpec: refactor-admin-workspace - 管理视图（薄包装，复用业务模块Control）
            // View在角色台，Control在业务模块
            containerRegistry.RegisterForNavigation<Views.HerbManagementView>();
            containerRegistry.RegisterForNavigation<Views.FormulaManagementView>();
            containerRegistry.RegisterForNavigation<Views.PatientManagementView>();
            containerRegistry.RegisterForNavigation<Views.MedicalCaseManagementView>();
            containerRegistry.RegisterForNavigation<Views.UserManagementView>();
        }
    }
}
