using Prism.Ioc;
using Prism.DryIoc;
using System;
using System.Windows;
using System.Net.Http;
using LYBT.WPF.Client.Shell.Views;
using LYBT.WPF.Client.Shell.ViewModels;
using LYBT.WPF.Client.Shell.Extensions;
using LYBT.WPF.Client.Modules.Authentication;
using LYBT.WPF.Client.Modules.SystemManagement;
using LYBT.WPF.Client.Modules.Consultation;
using LYBT.WPF.Client.Modules.MedicalCase;
using Prism.Modularity;
using Prism.Mvvm;

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
            
            // 显式配置ViewModelLocator映射 - 解决ViewModelLocator.AutoWireViewModel失败的问题
            ConfigureViewModelLocator();
        }
        
        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();
            
            // 显式注册View和ViewModel的映射关系
            ViewModelLocationProvider.Register<HomeView, HomeViewModel>();
            ViewModelLocationProvider.Register<TestHomeView, TestHomeViewModel>();
            ViewModelLocationProvider.Register<DiagnosticHomeView, DiagnosticHomeViewModel>();
            
            // 也可以使用类型字符串注册（备用方案）
            ViewModelLocationProvider.Register(
                typeof(HomeView).ToString(),
                () => Container.Resolve<HomeViewModel>()
            );
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            // 初始化错误处理服务并注册全局异常处理器
            try
            {
                var errorHandlingService = Container.Resolve<LYBT.WPF.Client.Core.Interfaces.Services.IErrorHandlingService>();
                errorHandlingService.RegisterGlobalExceptionHandlers();
            }
            catch (Exception ex)
            {
                // 如果错误处理服务初始化失败，使用基本的错误处理
                System.Diagnostics.Debug.WriteLine($"初始化错误处理服务失败: {ex.Message}");
                MessageBox.Show($"系统初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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