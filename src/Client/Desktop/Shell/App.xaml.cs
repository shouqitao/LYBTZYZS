using LYBT.Shared.Models.Contracts.Common;
using Prism.Ioc;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
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
using LYBT.Desktop.Users;
using LYBT.Desktop.Patients;
// UltraThink架构师修复：添加缺失模块的命名空间
using LYBT.Desktop.Herbs;
using LYBT.Desktop.Prescriptions;
using LYBT.Desktop.Formula;
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

            // UltraThink Phase 9: 初始化模块加载协调器和性能监控
            try
            {
                InitializeModuleLoadingCoordinator();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化模块加载协调器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// UltraThink Phase 9: 初始化模块加载协调器
        /// </summary>
        private void InitializeModuleLoadingCoordinator()
        {
            try
            {
                var moduleLoadingCoordinator = Container.Resolve<LYBT.Desktop.Core.Services.Performance.IModuleLoadingCoordinator>();
                var logger = Container.Resolve<Microsoft.Extensions.Logging.ILogger<App>>();
                
                logger.LogInformation("UltraThink Phase 9: 模块加载协调器初始化完成");

                // 订阅模块管理器事件以追踪模块初始化性能
                var moduleManager = Container.Resolve<Prism.Modularity.IModuleManager>();
                SubscribeToModuleEvents(moduleManager, moduleLoadingCoordinator, logger);
                
                logger.LogDebug("UltraThink: 模块性能监控已启动");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"模块加载协调器初始化异常: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 订阅模块管理器事件进行性能追踪
        /// </summary>
        private void SubscribeToModuleEvents(Prism.Modularity.IModuleManager moduleManager, 
            LYBT.Desktop.Core.Services.Performance.IModuleLoadingCoordinator coordinator, 
            Microsoft.Extensions.Logging.ILogger<App> logger)
        {
            var moduleInitTimes = new Dictionary<string, DateTime>();

            // 模块开始加载事件
            moduleManager.ModuleDownloadProgressChanged += (sender, e) =>
            {
                if (e.ProgressPercentage == 0) // 开始加载
                {
                    moduleInitTimes[e.ModuleInfo.ModuleName] = DateTime.Now;
                    logger.LogDebug("UltraThink追踪: 模块 {ModuleName} 开始加载", e.ModuleInfo.ModuleName);
                }
            };

            // 模块加载完成事件
            moduleManager.LoadModuleCompleted += (sender, e) =>
            {
                var moduleName = e.ModuleInfo.ModuleName;
                if (moduleInitTimes.TryGetValue(moduleName, out var startTime))
                {
                    var initializationTime = DateTime.Now - startTime;
                    coordinator.TrackModuleInitialization(moduleName, initializationTime);
                    moduleInitTimes.Remove(moduleName);

                    logger.LogInformation("UltraThink追踪: 模块 {ModuleName} 加载完成，耗时 {Duration}ms", 
                        moduleName, initializationTime.TotalMilliseconds);
                }

                if (!e.IsErrorHandled && e.Error != null)
                {
                    logger.LogError(e.Error, "模块 {ModuleName} 加载失败", e.ModuleInfo.ModuleName);
                }
            };

            logger.LogDebug("模块事件监听已配置完成");
        }

        protected override void ConfigureModuleCatalog(Prism.Modularity.IModuleCatalog moduleCatalog)
        {
            // UltraThink Phase 9 性能优化：实现智能模块加载策略
            // 只有认证模块在启动时加载，其他模块按需加载+性能监控
            
            // 1. 启动必需模块（立即加载）
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(AuthenticationModule),
                ModuleType = typeof(AuthenticationModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            // 2. 核心业务模块（智能按需加载）
            AddPerformanceOptimizedModule(moduleCatalog, nameof(UsersModule), typeof(UsersModule));
            AddPerformanceOptimizedModule(moduleCatalog, nameof(PatientsModule), typeof(PatientsModule));

            // 3. 功能模块（延迟加载）
            AddPerformanceOptimizedModule(moduleCatalog, nameof(ConsultationModule), typeof(ConsultationModule));
            AddPerformanceOptimizedModule(moduleCatalog, nameof(MedicalCaseModule), typeof(MedicalCaseModule));
            AddPerformanceOptimizedModule(moduleCatalog, nameof(HerbsModule), typeof(HerbsModule));
            AddPerformanceOptimizedModule(moduleCatalog, nameof(PrescriptionsModule), typeof(PrescriptionsModule));
            AddPerformanceOptimizedModule(moduleCatalog, nameof(FormulaModule), typeof(FormulaModule));

            // 4. 工作台模块（延迟加载 + 依赖管理）
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(ConsultationWorkbenchModule),
                ModuleType = typeof(ConsultationWorkbenchModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.OnDemand,
                DependsOn = new System.Collections.ObjectModel.Collection<string> { nameof(PatientsModule), nameof(ConsultationModule), nameof(MedicalCaseModule) }
            });

            base.ConfigureModuleCatalog(moduleCatalog);
        }

        /// <summary>
        /// UltraThink Phase 9: 添加性能优化的模块配置
        /// </summary>
        private void AddPerformanceOptimizedModule(Prism.Modularity.IModuleCatalog moduleCatalog, string moduleName, Type moduleType)
        {
            var moduleInfo = new ModuleInfo
            {
                ModuleName = moduleName,
                ModuleType = moduleType.AssemblyQualifiedName,
                InitializationMode = InitializationMode.OnDemand
            };
            
            moduleCatalog.AddModule(moduleInfo);
        }
    }
}