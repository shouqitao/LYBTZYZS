using LYBT.Desktop.AdminWorkstation.ViewModels;
using LYBT.Desktop.AdminWorkstation.Views;
using LYBT.Desktop.Services.ErrorHandling;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.AdminWorkstation
{
    /// <summary>
    /// 管理工作台模块
    /// 提供管理员的工作台界面和相关功能
    /// </summary>
    [Module(ModuleName = nameof(AdminWorkstationModule))]
    [ModuleDependency("AuthenticationModule")] // 依赖认证模块
    public class AdminWorkstationModule : IModule
    {
        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
            var logger = containerProvider.Resolve<ILogger<AdminWorkstationModule>>();
            logger?.LogInformation("管理工作台模块初始化完成");
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图模型
            containerRegistry.Register<AdminWorkstationViewModel>();

            // 注册主视图用于导航
            containerRegistry.RegisterForNavigation<AdminWorkstationView>();

            // 注册管理功能子视图（后续根据需要添加）
            // containerRegistry.RegisterForNavigation<UserManagementView>();
            // containerRegistry.RegisterForNavigation<HerbManagementView>();
            // containerRegistry.RegisterForNavigation<PatientManagementView>();
            // containerRegistry.RegisterForNavigation<FormulaManagementView>();
            // containerRegistry.RegisterForNavigation<MedicalCaseManagementView>();
            // containerRegistry.RegisterForNavigation<SystemSettingsView>();
        }
    }
}