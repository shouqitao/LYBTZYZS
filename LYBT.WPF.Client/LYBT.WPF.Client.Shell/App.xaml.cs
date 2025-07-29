using Prism.Ioc;
using Prism.DryIoc;
using System.Windows;
using LYBT.WPF.Client.Shell.Views;
using LYBT.WPF.Client.Modules.Authentication;
using LYBT.WPF.Client.Modules.SystemManagement;
using LYBT.WPF.Client.Modules.FrontDesk;
using LYBT.WPF.Client.Modules.Doctor;
using LYBT.WPF.Client.Modules.Cashier;
using Prism.Modularity;

namespace LYBT.WPF.Client.Shell
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册 MockAuthenticationService 为单例
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.IAuthenticationService, LYBT.WPF.Client.Services.MockAuthenticationService>();
        }

        protected override void ConfigureModuleCatalog(Prism.Modularity.IModuleCatalog moduleCatalog)
        {
            // 配置模块目录
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(AuthenticationModule),
                ModuleType = typeof(AuthenticationModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(SystemManagementModule),
                ModuleType = typeof(SystemManagementModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(FrontDeskModule),
                ModuleType = typeof(FrontDeskModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(DoctorModule),
                ModuleType = typeof(DoctorModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(CashierModule),
                ModuleType = typeof(CashierModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            base.ConfigureModuleCatalog(moduleCatalog);
        }
    }
}