using Prism.Ioc;
using Prism.DryIoc;
using System;
using System.Windows;
using System.Net.Http;
using LYBT.WPF.Client.Shell.Views;
using LYBT.WPF.Client.Shell.Extensions;
using LYBT.WPF.Client.Modules.Authentication;
using LYBT.WPF.Client.Modules.SystemManagement;
using LYBT.WPF.Client.Modules.Consultation;
using LYBT.WPF.Client.Modules.MedicalCase;
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
            // 使用扩展方法统一注册所有服务
            containerRegistry.RegisterAllServices();
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
                ModuleName = nameof(ConsultationModule),
                ModuleType = typeof(ConsultationModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(MedicalCaseModule),
                ModuleType = typeof(MedicalCaseModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            base.ConfigureModuleCatalog(moduleCatalog);
        }
    }
}