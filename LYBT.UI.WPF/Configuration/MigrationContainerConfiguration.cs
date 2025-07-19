using LYBT.UI.WPF.ViewModels.Components;
using LYBT.UI.WPF.ViewModels.Main;
using LYBT.UI.WPF.Views.Components;
using LYBT.UI.WPF.Views.Main;
using Prism.Ioc;

namespace LYBT.UI.WPF.Configuration {
    /// <summary>
    /// 迁移用的依赖注入配置 - 保持原有结构的同时添加新组件
    /// </summary>
    public static class MigrationContainerConfiguration {
        /// <summary>
        /// 配置迁移 - 在现有配置基础上添加新组件
        /// </summary>
        public static void ConfigureMigration(this IContainerRegistry containerRegistry) {
            // 注册新的组件视图模型
            containerRegistry.RegisterSingleton<NavigationDrawerViewModel>();
            containerRegistry.RegisterSingleton<WelcomePanelViewModel>();
            containerRegistry.RegisterSingleton<StatusBarViewModel>();

            // 注册主布局视图模型
            containerRegistry.RegisterSingleton<IntegratedMainLayoutViewModel>();

            // 注册新组件视图
            containerRegistry.Register<NavigationDrawer>();
            containerRegistry.Register<WelcomePanel>();
            containerRegistry.Register<StatusBar>();

            // 注册主布局视图用于导航
            containerRegistry.RegisterForNavigation<IntegratedMainLayout>();

            // 保持原有视图注册不变
            containerRegistry.RegisterForNavigation<LoginView>();
            containerRegistry.RegisterForNavigation<ChangePasswordView>();
            containerRegistry.RegisterForNavigation<ChangeProfileView>();
            containerRegistry.RegisterForNavigation<DoctorProfileView>();
            containerRegistry.RegisterForNavigation<HomeView>(); // 保留原有HomeView，可以作为fallback

            // 注意：不需要重新注册MainWindow和MainWindowViewModel，
            // 它们已经在原有配置中注册，我们只是更新了实现
        }

        /// <summary>
        /// 验证迁移配置 - 确保所有必要的服务都已注册
        /// </summary>
        public static void ValidateMigrationConfiguration(IContainerProvider container) {
            try {
                // 验证主要组件可以正确解析
                var mainWindowViewModel = container.Resolve<MainWindowViewModel>();
                var integratedLayout = container.Resolve<IntegratedMainLayoutViewModel>();
                var navigationDrawer = container.Resolve<NavigationDrawerViewModel>();

                System.Diagnostics.Debug.WriteLine("Migration configuration validation passed");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Migration configuration validation failed: {ex.Message}");
                throw new InvalidOperationException("Migration configuration is invalid", ex);
            }
        }
    }
}