using Prism.Ioc;
using Prism.DryIoc;
using System;
using System.Windows;
using System.Net.Http;
using LYBT.Desktop.Shell.Views;
using LYBT.Desktop.Shell.ViewModels;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Admin;
using LYBT.Desktop.Consultation;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.Shared;
using LYBT.Desktop.Users;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Workbench.Admin;
using LYBT.Desktop.Workbench.Consultation;
using LYBT.Desktop.Workbench.Core;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Shell
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
            
            // 注册工作台路由器
            containerRegistry.RegisterSingleton<IWorkbenchRouter, WorkbenchRouter>();
            
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
                var errorHandlingService = Container.Resolve<LYBT.Desktop.Core.Interfaces.Services.IErrorHandlingService>();
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
            // 配置模块目录 - 按依赖顺序注册
            
            // 1. 核心业务模块（独立，无依赖）
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(UsersModule),
                ModuleType = typeof(UsersModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(PatientsModule),
                ModuleType = typeof(PatientsModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            // 2. 传统模块（保持向后兼容）
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

            // 3. 工作台模块（依赖业务模块，最后初始化）
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(SystemWorkbenchModule),
                ModuleType = typeof(SystemWorkbenchModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable,
                DependsOn = new System.Collections.ObjectModel.Collection<string> { nameof(UsersModule), nameof(PatientsModule) }
            });

            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(ConsultationWorkbenchModule),
                ModuleType = typeof(ConsultationWorkbenchModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable,
                DependsOn = new System.Collections.ObjectModel.Collection<string> { nameof(PatientsModule), nameof(ConsultationModule), nameof(MedicalCaseModule) }
            });

            base.ConfigureModuleCatalog(moduleCatalog);
        }
    }
}