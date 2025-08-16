using LYBT.Shared.Models.Contracts.Common;
using Prism.Ioc;
using Prism.DryIoc;
using System;
using System.Windows;
using System.Net.Http;
using LYBT.Desktop.Shell.Views;
using LYBT.Desktop.Shell.ViewModels;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Auth;
// using LYBT.Desktop.Admin; // 已整合到AdminWorkbench
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
            // UltraThink 性能优化：实现模块延迟加载策略
            // 只有认证模块在启动时加载，其他模块按需加载
            
            // 1. 启动必需模块（立即加载）
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(AuthenticationModule),
                ModuleType = typeof(AuthenticationModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            // 2. 核心业务模块（按需加载）
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(UsersModule),
                ModuleType = typeof(UsersModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.OnDemand
            });

            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(PatientsModule),
                ModuleType = typeof(PatientsModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.OnDemand
            });

            // 3. 功能模块（延迟加载）
            // SystemManagementModule已整合到SystemWorkbenchModule

            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(ConsultationModule),
                ModuleType = typeof(ConsultationModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.OnDemand
            });

            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(MedicalCaseModule),
                ModuleType = typeof(MedicalCaseModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.OnDemand
            });

            // 4. 工作台模块（延迟加载 + 依赖管理）
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(SystemWorkbenchModule),
                ModuleType = typeof(SystemWorkbenchModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.OnDemand,
                DependsOn = new System.Collections.ObjectModel.Collection<string> { nameof(UsersModule), nameof(PatientsModule) }
            });

            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(ConsultationWorkbenchModule),
                ModuleType = typeof(ConsultationWorkbenchModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.OnDemand,
                DependsOn = new System.Collections.ObjectModel.Collection<string> { nameof(PatientsModule), nameof(ConsultationModule), nameof(MedicalCaseModule) }
            });

            base.ConfigureModuleCatalog(moduleCatalog);
        }
    }
}