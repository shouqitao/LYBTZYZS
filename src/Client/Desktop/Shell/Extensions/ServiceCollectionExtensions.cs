using System.Net.Http;
using AutoMapper;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Core.Mapping;
using LYBT.Desktop.Infrastructure;
using LYBT.Desktop.Services;
using LYBT.Desktop.Services.Handlers;
using LYBT.Desktop.Services.Registration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Extensions {

    /// <summary>
    /// 服务注册扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions {

        /// <summary>
        /// 注册所有服务
        /// </summary>
        public static void RegisterAllServices(this IContainerRegistry containerRegistry) {
            RegisterLogging(containerRegistry);
            RegisterAutoMapper(containerRegistry);
            RegisterCacheServices(containerRegistry);
            RegisterHttpServices(containerRegistry);
            RegisterApiServices(containerRegistry);
            RegisterModuleRegistrationServices(containerRegistry); // 新增：注册服务发现系统
            RegisterBusinessServices(containerRegistry);
            RegisterErrorHandlingServices(containerRegistry);
            RegisterDialogs(containerRegistry);
            RegisterPerformanceServices(containerRegistry);
            RegisterUltraThinkServices(containerRegistry);
            RegisterModuleServicesAutomatically(containerRegistry); // 新增：自动注册模块服务
            // ViewModels和Views通过Prism的ViewModelLocator自动解析，无需手动注册
        }

        /// <summary>
        /// 注册UltraThink高级服务
        /// </summary>
        private static void RegisterUltraThinkServices(IContainerRegistry containerRegistry) {
            // Phase I: 简化主题服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Theming.IThemeService,
                LYBT.Desktop.Core.Services.Theming.ThemeService>();

            // UltraThink Phase H: 高级功能优化服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IStartupOptimizationService,
                LYBT.Desktop.Core.Services.Performance.StartupOptimizationService>();

            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Settings.IUserPreferencesService,
                LYBT.Desktop.Core.Services.Settings.UserPreferencesService>();
        }

        /// <summary>
        /// 注册统一错误处理服务 - UltraThink简化版
        /// </summary>
        private static void RegisterErrorHandlingServices(IContainerRegistry containerRegistry) {
            // 注册统一错误处理器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Services.IStandardErrorHandler,
                LYBT.Desktop.Infrastructure.Services.StandardErrorHandler>();
        }

        /// <summary>
        /// 注册日志服务
        /// </summary>
        private static void RegisterLogging(IContainerRegistry containerRegistry) {
            // 注册简单的控制台日志提供程序
            containerRegistry.RegisterSingleton<ILoggerFactory>(() => {
                return LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));
            });

            // 注册泛型日志接口
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
        }

        /// <summary>
        /// 注册AutoMapper
        /// </summary>
        private static void RegisterAutoMapper(IContainerRegistry containerRegistry) {
            var mapperConfig = new MapperConfiguration(cfg => {
                cfg.AddProfile(new MappingProfile());
            }, NullLoggerFactory.Instance);

            var mapper = mapperConfig.CreateMapper();
            containerRegistry.RegisterInstance<IMapper>(mapper);
        }

        /// <summary>
        /// 注册缓存服务
        /// </summary>
        private static void RegisterCacheServices(IContainerRegistry containerRegistry) {
            // 注册内存缓存服务
            containerRegistry.RegisterSingleton<IMemoryCache, MemoryCache>();
        }

        /// <summary>
        /// 注册HTTP相关服务
        /// </summary>
        private static void RegisterHttpServices(IContainerRegistry containerRegistry) {
            // 注册基础HttpClient
            containerRegistry.RegisterSingleton<HttpClient>(() => {
                return HttpClientFactory.CreateBasicClient(ApiConfiguration.BaseUrl);
            });
        }

        /// <summary>
        /// 注册API服务 - UltraThink统一API客户端管理器
        /// </summary>
        private static void RegisterApiServices(IContainerRegistry containerRegistry) {
            // 注册认证处理器
            containerRegistry.Register<AuthHeaderHandler>();

            // 注册统一API客户端管理器 - 替代原有8个独立API客户端
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager,
                LYBT.Desktop.Infrastructure.Api.UnifiedApiClientManager>();

            // 注册各个API接口的便捷访问器（委托给统一管理器）
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IAuthApi>(container =>
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().AuthApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IUserApi>(container =>
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().UserApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IPatientApi>(container =>
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().PatientApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IHerbApi>(container =>
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().HerbApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IFormulaApi>(container =>
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().FormulaApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IConsultationApi>(container =>
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().ConsultationApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IPrescriptionApi>(container =>
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().PrescriptionApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IMedicalCaseApi>(container =>
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().MedicalCaseApi);

            // 注册通用API服务 - UltraThink统一架构：使用完整版Http.ApiService
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Http.IApiService, LYBT.Desktop.Core.Http.ApiService>();
        }

        /// <summary>
        /// 注册模块注册系统服务
        /// </summary>
        private static void RegisterModuleRegistrationServices(IContainerRegistry containerRegistry) {
            // 注册服务发现和注册组件
            containerRegistry.RegisterSingleton<IModuleServiceRegistrar, ModuleRegistrationValidator>();
            
            // 注册模块配置管理器
            containerRegistry.RegisterSingleton<ModuleConfigurationManager>(() => {
                var manager = new ModuleConfigurationManager();
                ConfigureDefaultModules(manager);
                return manager;
            });
        }

        /// <summary>
        /// 自动注册所有模块服务（增强版，支持条件注册）
        /// </summary>
        private static void RegisterModuleServicesAutomatically(IContainerRegistry containerRegistry) {
            try {
                var logger = LoggerFactory.Create(builder => builder.AddDebug())
                    .CreateLogger("ServiceRegistration");
                
                logger.LogInformation("开始自动发现和注册模块服务（增强模式）...");
                
                // 获取配置管理器
                var configManager = new ModuleConfigurationManager();
                ConfigureDefaultModules(configManager);
                
                // 获取模块注册器
                var registrar = new ModuleRegistrationValidator(
                    LoggerFactory.Create(builder => builder.AddDebug())
                    .CreateLogger<ModuleRegistrationValidator>());
                
                // 执行条件注册
                RegisterServicesWithConfiguration(containerRegistry, registrar, configManager, logger);
                
                // 输出诊断报告
                var report = registrar.CreateDiagnosticReport();
                logger.LogInformation("增强自动服务注册完成:\n{Report}", report);
            }
            catch (Exception ex) {
                // 如果自动发现失败，记录错误并回退到手动注册
                var logger = LoggerFactory.Create(builder => builder.AddDebug())
                    .CreateLogger("ServiceRegistration");
                logger.LogError(ex, "增强自动服务发现失败，回退到手动注册模式");
                RegisterModuleServicesManually(containerRegistry);
            }
        }

        /// <summary>
        /// 手动注册模块服务（回退方案）
        /// </summary>
        private static void RegisterModuleServicesManually(IContainerRegistry containerRegistry) {
            // 保留关键模块的手动注册作为回退方案
            
            // Auth服务注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Auth.Services.AuthModule>();
            containerRegistry.Register<LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService>(container =>
                container.Resolve<LYBT.Desktop.Auth.Services.AuthModule>());
            containerRegistry.Register<LYBT.Shared.Interfaces.Services.IAuthService>(container =>
                container.Resolve<LYBT.Desktop.Auth.Services.AuthModule>());

            // User服务注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Users.Services.UserModule>();
            containerRegistry.Register<LYBT.Shared.Interfaces.Services.IUserService>(container =>
                container.Resolve<LYBT.Desktop.Users.Services.UserModule>());

            // Patient服务注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Patients.Services.PatientModule>();
            containerRegistry.Register<LYBT.Shared.Interfaces.Services.IPatientService>(container =>
                container.Resolve<LYBT.Desktop.Patients.Services.PatientModule>());
        }

        /// <summary>
        /// 注册业务服务
        /// </summary>
        private static void RegisterBusinessServices(IContainerRegistry containerRegistry) {
            RegisterCoreServices(containerRegistry);
            RegisterDomainServices(containerRegistry);
        }

        /// <summary>
        /// 注册核心基础服务（非业务模块服务）
        /// </summary>
        private static void RegisterCoreServices(IContainerRegistry containerRegistry) {
            // 权限服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IPermissionService, PermissionService>();

            // 会话管理服务 - UserSessionManager实现多个接口
            containerRegistry.RegisterSingleton<UserSessionManager>();
            containerRegistry.Register<LYBT.Desktop.Core.Interfaces.Services.IUserSessionManager>(container => container.Resolve<UserSessionManager>());
            containerRegistry.Register<LYBT.Desktop.Core.Interfaces.Services.ITokenManager>(container => container.Resolve<UserSessionManager>());

            // 其他核心服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.ISessionManager,
                LYBT.Desktop.Core.Services.SessionManager>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.INotificationService,
                LYBT.Desktop.Core.Services.NotificationService>();
                
            // 注意：双层架构服务（QueryService/BusinessService）现在由自动发现系统处理
            // 只保留不符合命名约定的特殊服务的手动注册
            
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IErrorHandlingService>(container =>
                new LYBT.Desktop.Services.ErrorHandlingService(container.Resolve<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService>()));
            containerRegistry.RegisterSingleton<LYBT.Desktop.Workbench.Core.IWorkbenchRouter, LYBT.Desktop.Workbench.Core.WorkbenchRouter>();

            // 主窗口服务门面 - 简化MainWindowViewModel的依赖注入
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IMainWindowServicesFacade,
                LYBT.Desktop.Core.Services.MainWindowServicesFacade>();

            // P7-03: 处方打印服务 - UltraThink标准打印系统
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IPrescriptionPrintService,
                LYBT.Desktop.Core.Services.PrescriptionPrintService>();

            // P7-04: 用户体验优化服务 - UltraThink用户体验增强
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IUserExperienceService,
                LYBT.Desktop.Core.Services.UserExperienceService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IKeyboardShortcutService,
                LYBT.Desktop.Core.Services.KeyboardShortcutService>();
        }

        /// <summary>
        /// 注册领域业务服务
        /// </summary>
        private static void RegisterDomainServices(IContainerRegistry containerRegistry) {
            // API测试服务
            containerRegistry.RegisterSingleton<ApiTestService>();

            // 注意：8个业务模块(Auth/Users/Patients/Herbs/Formula/Consultation/Prescriptions/MedicalCase)
            // 现在通过自动发现系统统一注册，无需在各自的XxxModule.RegisterTypes中重复注册
            // 这消除了双重注册风险，简化了模块开发
        }

        /// <summary>
        /// 注册对话框服务
        /// </summary>
        private static void RegisterDialogs(IContainerRegistry containerRegistry) {
            // 统一对话框服务 - WpfDialogService提供完整功能
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService,
                LYBT.Desktop.Core.Services.WpfDialogService>();

            // 注册业务对话框（在服务启动后动态注册）
            containerRegistry.RegisterInstance<Action<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService>>(RegisterBusinessDialogs);
        }

        /// <summary>
        /// 注册业务对话框Views
        /// </summary>
        private static void RegisterBusinessDialogs(LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService dialogService) {
            // 使用WpfDialogService的RegisterDialog方法注册业务对话框
            if (dialogService is LYBT.Desktop.Core.Services.WpfDialogService wpfDialogService) {
                // 患者管理对话框
                wpfDialogService.RegisterDialog("PatientAddEditDialog", typeof(LYBT.Desktop.Patients.Views.PatientAddEditDialog));

                // 用户管理对话框
                wpfDialogService.RegisterDialog("UserAddEditDialog", typeof(LYBT.Desktop.Users.Views.UserAddEditDialog));

                // 药材管理对话框
                wpfDialogService.RegisterDialog("HerbAddEditDialog", typeof(LYBT.Desktop.Herbs.Views.HerbAddEditDialog));

                // 医案管理对话框
                wpfDialogService.RegisterDialog("CreateMedicalCaseDialog", typeof(LYBT.Desktop.MedicalCase.Views.CreateMedicalCaseDialog));

                // 验方管理对话框
                wpfDialogService.RegisterDialog("AddFormulaDialog", typeof(LYBT.Desktop.Formula.Views.AddFormulaDialog));
                wpfDialogService.RegisterDialog("EditFormulaDialog", typeof(LYBT.Desktop.Formula.Views.EditFormulaDialog));
                wpfDialogService.RegisterDialog("ViewFormulaDialog", typeof(LYBT.Desktop.Formula.Views.ViewFormulaDialog));

                // 处方管理对话框
                wpfDialogService.RegisterDialog("PrescriptionEditorDialog", typeof(LYBT.Desktop.Prescriptions.Views.PrescriptionEditorDialog));
                wpfDialogService.RegisterDialog("HerbSelectionDialog", typeof(LYBT.Desktop.Prescriptions.Views.HerbSelectionDialog));
                wpfDialogService.RegisterDialog("FormulaTemplateDialog", typeof(LYBT.Desktop.Prescriptions.Views.FormulaTemplateDialog));
            }
        }

        /// <summary>
        /// 注册性能优化服务
        /// </summary>
        /// <summary>
        /// 注册性能优化服务
        /// </summary>
        private static void RegisterPerformanceServices(IContainerRegistry containerRegistry) {
            // UltraThink深度清理: 只保留实际使用的模块加载协调器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IModuleLoadingCoordinator,
                LYBT.Desktop.Core.Services.Performance.ModuleLoadingCoordinator>();
        }

        #region 辅助方法

        // UltraThink统一API客户端管理器已替代原有的独立API服务注册方式
        // 所有API客户端现由UnifiedApiClientManager统一管理，提供更好的一致性和可维护性

        /// <summary>
        /// 配置默认模块设置
        /// </summary>
        private static void ConfigureDefaultModules(ModuleConfigurationManager manager)
        {
            // Auth模块配置
            manager.AddOrUpdateConfiguration(new ModuleConfiguration
            {
                ModuleName = "Auth",
                IsEnabled = true,
                LifetimeType = ServiceLifetimeType.Singleton,
                SessionIntegration = new SessionIntegrationSettings
                {
                    RequiresSessionManager = true,
                    SessionTimeoutMinutes = 30,
                    AutoRenewSession = true
                }
            });

            // 核心业务模块配置
            var coreModules = new[] { "User", "Patient", "Herb", "Formula", "Consultation", "Prescription", "MedicalCase" };
            foreach (var module in coreModules)
            {
                manager.AddOrUpdateConfiguration(new ModuleConfiguration
                {
                    ModuleName = module,
                    IsEnabled = true,
                    LifetimeType = ServiceLifetimeType.Singleton,
                    Dependencies = module == "Auth" ? new List<string>() : new List<string> { "Auth" },
                    SessionIntegration = new SessionIntegrationSettings
                    {
                        RequiresSessionManager = module != "Herb" && module != "Formula",
                        SessionTimeoutMinutes = 30
                    }
                });
            }
        }

        /// <summary>
        /// 使用配置进行条件服务注册
        /// </summary>
        private static void RegisterServicesWithConfiguration(
            IContainerRegistry containerRegistry,
            ModuleRegistrationValidator registrar,
            ModuleConfigurationManager configManager,
            ILogger logger)
        {
            // 获取按依赖顺序排列的配置
            var orderedConfigurations = configManager.GetDependencyOrderedConfigurations();
            var registrationContext = CreateRegistrationContext();

            logger.LogInformation("按依赖顺序注册 {Count} 个模块", orderedConfigurations.Count);

            foreach (var config in orderedConfigurations)
            {
                try
                {
                    // 检查是否应该注册此模块
                    if (!configManager.ShouldRegisterModule(config, registrationContext))
                    {
                        logger.LogInformation("跳过模块 {ModuleName}，条件不满足", config.ModuleName);
                        continue;
                    }

                    // 验证依赖关系
                    var dependencyValidation = configManager.ValidateDependencies(config.ModuleName);
                    if (!dependencyValidation.IsValid)
                    {
                        logger.LogWarning("跳过模块 {ModuleName}，依赖验证失败: {Error}", 
                            config.ModuleName, dependencyValidation.ErrorMessage);
                        continue;
                    }

                    // 注册模块服务
                    RegisterModuleWithConfiguration(containerRegistry, registrar, config, logger);

                    logger.LogInformation("成功注册模块: {ModuleName}", config.ModuleName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "注册模块 {ModuleName} 时出错", config.ModuleName);
                }
            }
        }

        /// <summary>
        /// 使用特定配置注册单个模块
        /// </summary>
        private static void RegisterModuleWithConfiguration(
            IContainerRegistry containerRegistry,
            ModuleRegistrationValidator registrar,
            ModuleConfiguration config,
            ILogger logger)
        {
            // 基于配置查找和注册服务
            var discoveredServices = ServiceDiscovery.GetModuleServices(config.ModuleName);
            
            foreach (var serviceInfo in discoveredServices)
            {
                // 应用生命周期配置
                var lifetime = MapServiceLifetime(config.LifetimeType);
                
                // 注册服务
                RegisterServiceWithLifetime(containerRegistry, serviceInfo.ServiceType, 
                    serviceInfo.ImplementationType, lifetime);

                // 如果需要会话管理集成，注入额外依赖
                if (config.SessionIntegration.RequiresSessionManager)
                {
                    InjectSessionManagerDependency(containerRegistry, serviceInfo, logger);
                }

                logger.LogDebug("注册服务: {ServiceType} → {ImplementationType} ({Lifetime})",
                    serviceInfo.ServiceType.Name, serviceInfo.ImplementationType.Name, lifetime);
            }
        }

        /// <summary>
        /// 创建注册上下文
        /// </summary>
        private static Dictionary<string, object> CreateRegistrationContext()
        {
            return new Dictionary<string, object>
            {
                { "Environment", "Development" },
                { "EnableDebugLogging", true },
                { "EnablePerformanceMonitoring", false }
            };
        }

        /// <summary>
        /// 映射服务生命周期
        /// </summary>
        private static object MapServiceLifetime(ServiceLifetimeType lifetimeType)
        {
            // 对于Prism.Ioc，我们使用相应的注册方法而不是枚举
            return lifetimeType; // 保持枚举类型，在注册时使用
        }

        /// <summary>
        /// 根据生命周期类型注册服务
        /// </summary>
        private static void RegisterServiceWithLifetime(
            IContainerRegistry containerRegistry,
            Type serviceType,
            Type implementationType,
            object lifetime)
        {
            if (lifetime is ServiceLifetimeType lifetimeType)
            {
                switch (lifetimeType)
                {
                    case ServiceLifetimeType.Singleton:
                        containerRegistry.RegisterSingleton(serviceType, implementationType);
                        break;
                    case ServiceLifetimeType.Transient:
                        containerRegistry.Register(serviceType, implementationType);
                        break;
                    case ServiceLifetimeType.Scoped:
                        // Prism.Ioc 不直接支持 Scoped，使用 Transient
                        containerRegistry.Register(serviceType, implementationType);
                        break;
                }
            }
        }

        /// <summary>
        /// 为服务注入会话管理器依赖
        /// </summary>
        private static void InjectSessionManagerDependency(
            IContainerRegistry containerRegistry,
            ServiceRegistrationInfo serviceInfo,
            ILogger logger)
        {
            try
            {
                // 检查实现类型的构造函数是否需要会话管理器
                var constructors = serviceInfo.ImplementationType.GetConstructors();
                var hasSessionManagerParameter = constructors.Any(c => 
                    c.GetParameters().Any(p => 
                        p.ParameterType == typeof(LYBT.Desktop.Core.Interfaces.Services.ISessionManager) ||
                        p.ParameterType == typeof(LYBT.Desktop.Core.Interfaces.Services.IUserSessionManager)));

                if (hasSessionManagerParameter)
                {
                    logger.LogDebug("服务 {ServiceType} 需要会话管理器依赖", serviceInfo.ServiceType.Name);
                    // 会话管理器已在RegisterCoreServices中注册，无需额外处理
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "分析服务 {ServiceType} 的会话管理器依赖时出错", serviceInfo.ServiceType.Name);
            }
        }

        #endregion 辅助方法
    }
}
