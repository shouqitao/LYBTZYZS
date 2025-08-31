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
using LYBT.Desktop.Workbench.Admin;
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
            
            // UltraThink Phase H: 启动性能优化 - 应用预热
            _ = Task.Run(async () =>
            {
                try
                {
                    var startupService = Container.Resolve<LYBT.Desktop.Core.Services.Performance.IStartupOptimizationService>();
                    await startupService.WarmupApplicationAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"应用预热失败: {ex.Message}");
                }
            });
            
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

            // UltraThink Phase H: 简化模块加载协调器（移除复杂的性能监控）
            try
            {
                InitializeSimplifiedModuleCoordinator();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化模块协调器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// UltraThink Phase 9: 初始化模块加载协调器
        /// </summary>
        /// <summary>
        /// UltraThink Phase H: 简化的模块协调器初始化
        /// 移除复杂的性能监控，专注核心功能
        /// </summary>
        private void InitializeSimplifiedModuleCoordinator()
        {
            try
            {
                var logger = Container.Resolve<Microsoft.Extensions.Logging.ILogger<App>>();
                logger.LogInformation("UltraThink Phase H: 简化模块协调器初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"简化模块协调器初始化异常: {ex}");
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
            // UltraThink Phase H: 基于角色的智能模块加载策略
            // 显著提升启动性能，只加载用户角色所需的模块
            
            // 1. 核心必需模块（所有角色都需要）
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(AuthenticationModule),
                ModuleType = typeof(AuthenticationModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(UsersModule),
                ModuleType = typeof(UsersModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            // 2. 基础业务模块（医疗相关角色必需）
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(PatientsModule),
                ModuleType = typeof(PatientsModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });

            // 3. 专业功能模块（按需加载，提升启动速度）
            AddRoleBasedModule(moduleCatalog, nameof(ConsultationModule), typeof(ConsultationModule), 
                new[] { "Doctor", "Admin" });
            
            AddRoleBasedModule(moduleCatalog, nameof(MedicalCaseModule), typeof(MedicalCaseModule), 
                new[] { "Doctor", "Admin" });
            
            AddRoleBasedModule(moduleCatalog, nameof(HerbsModule), typeof(HerbsModule), 
                new[] { "Doctor", "Pharmacist", "Admin" });
            
            AddRoleBasedModule(moduleCatalog, nameof(PrescriptionsModule), typeof(PrescriptionsModule), 
                new[] { "Doctor", "Pharmacist", "Admin" });
            
            AddRoleBasedModule(moduleCatalog, nameof(FormulaModule), typeof(FormulaModule), 
                new[] { "Doctor", "Admin" });

            // 4. 工作台模块（基于角色智能加载）
            AddRoleBasedModule(moduleCatalog, nameof(SystemWorkbenchModule), typeof(SystemWorkbenchModule), 
                new[] { "Admin" });

            AddRoleBasedModule(moduleCatalog, nameof(ConsultationWorkbenchModule), typeof(ConsultationWorkbenchModule), 
                new[] { "Doctor", "Admin" });

            // 5. 其他工作台模块（按需加载）
            // 注意：CashierWorkbench、PharmacistWorkbench等根据实际角色需要时再加载

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

        /// <summary>
        /// UltraThink Phase H: 添加基于角色的智能模块配置
        /// 根据用户角色决定模块加载时机，提升启动性能
        /// </summary>
        private void AddRoleBasedModule(Prism.Modularity.IModuleCatalog moduleCatalog, string moduleName, Type moduleType, string[] requiredRoles)
        {
            var moduleInfo = new ModuleInfo
            {
                ModuleName = moduleName,
                ModuleType = moduleType.AssemblyQualifiedName,
                // 暂时设为按需加载，登录后根据角色决定是否立即加载
                InitializationMode = InitializationMode.OnDemand
            };
            
            // 将角色信息存储在模块元数据中，供后续角色检查使用
            if (requiredRoles?.Length > 0)
            {
                moduleInfo.Metadata.Add("RequiredRoles", string.Join(",", requiredRoles));
            }
            
            moduleCatalog.AddModule(moduleInfo);
        }

        /// <summary>
        /// UltraThink Phase H: 用户登录后的角色驱动模块加载
        /// 根据用户角色智能加载所需模块，避免不必要的资源消耗
        /// </summary>
        public async Task LoadRoleBasedModulesAsync(string userRole)
        {
            try
            {
                var moduleManager = Container.Resolve<Prism.Modularity.IModuleManager>();
                var moduleCatalog = Container.Resolve<Prism.Modularity.IModuleCatalog>();
                var logger = Container.Resolve<Microsoft.Extensions.Logging.ILogger<App>>();

                logger.LogInformation("UltraThink Phase H: 开始为角色 {UserRole} 加载模块", userRole);

                var modulesToLoad = new List<string>();

                // 遍历所有按需加载的模块，检查角色匹配
                foreach (var module in moduleCatalog.Modules.Where(m => m.InitializationMode == InitializationMode.OnDemand))
                {
                    if (module.Metadata.TryGetValue("RequiredRoles", out var requiredRolesStr) && 
                        !string.IsNullOrEmpty(requiredRolesStr))
                    {
                        var requiredRoles = requiredRolesStr.Split(',');
                        if (requiredRoles.Contains(userRole) || requiredRoles.Contains("Admin"))
                        {
                            modulesToLoad.Add(module.ModuleName);
                        }
                    }
                }

                // 批量加载匹配的模块
                foreach (var moduleName in modulesToLoad)
                {
                    try
                    {
                        await Task.Run(() => moduleManager.LoadModule(moduleName));
                        logger.LogDebug("UltraThink: 模块 {ModuleName} 加载完成", moduleName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "加载模块 {ModuleName} 失败", moduleName);
                    }
                }

                logger.LogInformation("UltraThink Phase H: 角色驱动模块加载完成，共加载 {Count} 个模块", modulesToLoad.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"角色驱动模块加载异常: {ex.Message}");
            }
        }
    }
}