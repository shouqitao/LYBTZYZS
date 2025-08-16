using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Prism.Ioc;
using Refit;
using AutoMapper;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Services;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Core.Mapping;
using LYBT.Desktop.Core.Models.Cache;
using LYBT.Desktop.Infrastructure;
using LYBT.Desktop.Services.Handlers;
using LYBT.Desktop.Core.Caching;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Consultation.Services;
// using LYBT.Desktop.Admin.Prescriptions.Services; // 已整合到AdminWorkbench

namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>
    /// 服务注册扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册所有服务
        /// </summary>
        public static void RegisterAllServices(this IContainerRegistry containerRegistry)
        {
            RegisterLogging(containerRegistry);
            RegisterAutoMapper(containerRegistry);
            RegisterCacheServices(containerRegistry);
            RegisterHttpServices(containerRegistry);
            RegisterApiServices(containerRegistry);
            RegisterBusinessServices(containerRegistry);
            RegisterDialogs(containerRegistry);
            RegisterViewModels(containerRegistry);
            RegisterViews(containerRegistry);
        }

        /// <summary>
        /// 注册日志服务
        /// </summary>
        private static void RegisterLogging(IContainerRegistry containerRegistry)
        {
            // 创建日志工厂
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // 注册日志工厂
            containerRegistry.RegisterSingleton<ILoggerFactory>(() => loggerFactory);

            // 注册泛型日志器 - 简单注册，让DI容器自动解析依赖
            containerRegistry.RegisterSingleton(typeof(ILogger<>), typeof(Logger<>));
        }

        /// <summary>
        /// 注册AutoMapper
        /// </summary>
        private static void RegisterAutoMapper(IContainerRegistry containerRegistry)
        {
            // 创建AutoMapper配置 - AutoMapper 15需要ILoggerFactory参数
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
                // 可以在这里添加更多的Profile
            }, NullLoggerFactory.Instance);

            IMapper mapper = mapperConfig.CreateMapper();

            // 注册IMapper为单例
            containerRegistry.RegisterSingleton<IMapper>(() => mapper);
        }

        /// <summary>
        /// 注册缓存服务
        /// </summary>
        private static void RegisterCacheServices(IContainerRegistry containerRegistry)
        {
            // 注册Microsoft.Extensions.Caching.Memory
            containerRegistry.RegisterSingleton<IMemoryCache>(() => new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1000 // 可选：设置大小限制
            }));

            // 注册缓存配置（根据环境选择不同的配置）
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Models.Cache.CacheOptions>(() =>
            {
#if DEBUG
                return LYBT.Desktop.Core.Models.Cache.CacheOptions.Development();
#else
                return LYBT.Desktop.Core.Models.Cache.CacheOptions.Production();
#endif
            });

            // 注册缓存服务
            containerRegistry.RegisterSingleton<ICacheService, LYBT.Desktop.Services.MemoryCacheService>();
            
            // 注册内存缓存服务（阶段3新增）
            containerRegistry.RegisterSingleton<IMemoryCacheService, LYBT.Desktop.Core.Caching.MemoryCacheService>();
        }

        /// <summary>
        /// 注册HTTP相关服务
        /// </summary>
        private static void RegisterHttpServices(IContainerRegistry containerRegistry)
        {
            // 注册基础HttpClient（使用统一工厂）
            containerRegistry.RegisterSingleton<HttpClient>(() =>
            {
                return HttpClientFactory.CreateBasicClient(ApiConfiguration.BaseUrl);
            });
        }

        /// <summary>
        /// 注册API服务
        /// </summary>
        private static void RegisterApiServices(IContainerRegistry containerRegistry)
        {
            // 注册基础API服务（无认证）
            RegisterBasicApiService<IAuthApiService>(containerRegistry);
            
            // 注册需要认证的API服务
            RegisterAuthenticatedApiService<IUserApiService>(containerRegistry);
            RegisterAuthenticatedApiService<IHerbApiService>(containerRegistry);
            RegisterAuthenticatedApiService<IFormulaApiService>(containerRegistry);
            RegisterAuthenticatedApiService<IPatientApiService>(containerRegistry);
            RegisterAuthenticatedApiService<ISystemSettingsApiService>(containerRegistry);
            RegisterAuthenticatedApiService<IBackupApiService>(containerRegistry);
            RegisterAuthenticatedApiService<IConsultationApiService>(containerRegistry);
            RegisterAuthenticatedApiService<IPrescriptionApiService>(containerRegistry);
            RegisterAuthenticatedApiService<IMedicalCaseApiService>(containerRegistry);

            // 注册通用API服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.IApiService, LYBT.Desktop.Services.ApiService>();

            // UltraThink Phase 5.4: 注册性能优化服务基础设施
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Configuration.IAppConfiguration>(() =>
                LYBT.Desktop.Core.Configuration.AppConfiguration.CreateDefault());
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IUIPerformanceOptimizer, 
                LYBT.Desktop.Core.Services.Performance.UIPerformanceOptimizer>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IPerformanceMonitorService, 
                LYBT.Desktop.Core.Services.Performance.PerformanceMonitorService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IMemoryManagerService, 
                LYBT.Desktop.Core.Services.Performance.MemoryManagerService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.ISmartVirtualizationManager, 
                LYBT.Desktop.Core.Services.Performance.SmartVirtualizationManager>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.ISmartLoadingStrategy, 
                LYBT.Desktop.Core.Services.Performance.SmartLoadingStrategy>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IDataBindingOptimizer, 
                LYBT.Desktop.Core.Services.Performance.DataBindingOptimizer>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IPerformanceAnalysisService, 
                LYBT.Desktop.Core.Services.Performance.PerformanceAnalysisService>();
        }

        /// <summary>
        /// 注册基础API服务（无认证）
        /// </summary>
        private static void RegisterBasicApiService<TService>(IContainerRegistry containerRegistry)
            where TService : class
        {
            containerRegistry.RegisterSingleton<TService>(() =>
            {
                var httpClient = HttpClientFactory.CreateBasicClient(ApiConfiguration.BaseUrl);
                return RestService.For<TService>(httpClient, RefitConfiguration.GetRefitSettings());
            });
        }

        /// <summary>
        /// 注册需要认证的API服务
        /// </summary>
        private static void RegisterAuthenticatedApiService<TService>(IContainerRegistry containerRegistry)
            where TService : class
        {
            containerRegistry.Register<TService>(container =>
            {
                var tokenManager = container.Resolve<ITokenManager>();
                var authHandler = new AuthHeaderHandler(tokenManager);
                var httpClient = HttpClientFactory.CreateAuthenticatedClient(authHandler, ApiConfiguration.BaseUrl);
                return RestService.For<TService>(httpClient, RefitConfiguration.GetRefitSettings());
            });
        }

        /// <summary>
        /// 注册业务服务
        /// </summary>
        private static void RegisterBusinessServices(IContainerRegistry containerRegistry)
        {
            // 按功能组注册服务，提高代码可读性和维护性
            RegisterCoreServices(containerRegistry);
            RegisterDomainServices(containerRegistry);
            RegisterSupportingServices(containerRegistry);
        }

        /// <summary>
        /// 注册核心基础服务
        /// </summary>
        private static void RegisterCoreServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ITokenManager, TokenManager>();
            containerRegistry.RegisterSingleton<IUserSessionManager, UserSessionManager>();
            containerRegistry.RegisterSingleton<IPermissionService, PermissionService>();
            
            // 错误处理相关服务 - 注册依赖链
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.IErrorClassifier, LYBT.Desktop.Core.Services.ErrorClassifier>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.IUserNotificationService, LYBT.Desktop.Core.Services.UserNotificationService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.IGlobalExceptionHandler, LYBT.Desktop.Core.Services.GlobalExceptionHandler>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IErrorHandlingService, LYBT.Desktop.Services.ErrorHandlingService>();
            
            // UltraThink重构：注册新的统一事件系统组件
            RegisterEventSystemServices(containerRegistry);
        }

        /// <summary>
        /// 注册事件系统服务 - UltraThink架构重构
        /// </summary>
        private static void RegisterEventSystemServices(IContainerRegistry containerRegistry)
        {
            // UltraThink策略：完全切换到新统一事件架构
            containerRegistry.RegisterSingleton<UnifiedEventHandler>();
            containerRegistry.RegisterSingleton<EventMigrationAdapter>();
            
            // 直接使用新架构的ConsultationEventHandler
            containerRegistry.RegisterSingleton<LYBT.Desktop.Consultation.Services.Interfaces.IConsultationEventHandler, 
                LYBT.Desktop.Consultation.Services.ConsultationEventHandler>();
        }

        /// <summary>
        /// 注册领域业务服务
        /// </summary>
        private static void RegisterDomainServices(IContainerRegistry containerRegistry)
        {
            // UltraThink Phase 2.2.6: 注册新的业务接口和旧的UI接口
            // 每个Service实现都同时支持两套接口
            
            // 注册AuthenticationService - UI接口实现
            containerRegistry.RegisterSingleton<AuthenticationService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService>(
                container => container.Resolve<AuthenticationService>());

            // 注册UserService - UI接口实现
            containerRegistry.RegisterSingleton<UserService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IUserService>(
                container => container.Resolve<UserService>());

            // 注册其他服务 - 暂时保持原有注册方式，使用完全限定类型名解决歧义
            var legacyDomainServices = new (Type Interface, Type Implementation)[]
            {
                (typeof(LYBT.Desktop.Core.Interfaces.Services.IPatientService), typeof(PatientService)),
                (typeof(LYBT.Desktop.Core.Interfaces.Services.IConsultationService), typeof(ConsultationService)),
                (typeof(LYBT.Shared.Interfaces.Services.IPrescriptionService), typeof(PrescriptionService)),
                (typeof(LYBT.Desktop.Core.Interfaces.Services.IMedicalCaseService), typeof(MedicalCaseService))
            };

            // 注册FormulaService为具体类型（当前未实现接口，在Phase 3中将重构为接口实现）
            containerRegistry.RegisterSingleton<FormulaService>();

            foreach (var (interfaceType, implementationType) in legacyDomainServices)
            {
                containerRegistry.RegisterSingleton(interfaceType, implementationType);
            }

            // 特殊处理：药材服务使用缓存装饰器（阶段3优化）
            containerRegistry.RegisterSingleton<HerbService>();  // 原始服务
            containerRegistry.RegisterSingleton<LYBT.Shared.Interfaces.Services.IHerbService>(container => 
                new CachedHerbService(
                    container.Resolve<HerbService>(),
                    container.Resolve<IMemoryCacheService>(),
                    container.Resolve<ILogger<CachedHerbService>>()));
        }

        /// <summary>
        /// 注册支持性服务
        /// </summary>
        private static void RegisterSupportingServices(IContainerRegistry containerRegistry)
        {
            // 原有服务
            containerRegistry.RegisterSingleton<ICredentialService, CredentialService>();
            containerRegistry.RegisterSingleton<IPrescriptionValidationService, PrescriptionValidationService>();
            containerRegistry.RegisterSingleton<IIDCardReaderService, MockIDCardReaderService>();

            // 阶段3新增服务
            containerRegistry.RegisterSingleton<ApiOptimizationService>();
            
            // 注册处方打印服务 - 支持两个不同的接口
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.PrescriptionPrintService>(); // Services项目中的实现
            containerRegistry.RegisterSingleton<IPrescriptionPrintService>(container => container.Resolve<LYBT.Desktop.Services.PrescriptionPrintService>());
            
            // SystemManagement模块的高级打印服务 - 已整合到AdminWorkbench
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Admin.Prescriptions.Services.PrescriptionPrintService>();
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Admin.Prescriptions.Services.IAdvancedPrescriptionPrintService>(
            //     container => container.Resolve<LYBT.Desktop.Admin.Prescriptions.Services.PrescriptionPrintService>());
            
            // containerRegistry.RegisterSingleton<PrescriptionTemplateService>(); // 已删除
            // containerRegistry.RegisterSingleton<OptimizedPrescriptionSearchService>(); // 已删除

            // 缓存和性能优化服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.ICacheWarmupService, LYBT.Desktop.Core.Services.CacheWarmupService>();

            // 错误处理服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.IUserFriendlyErrorService, LYBT.Desktop.Core.Services.UserFriendlyErrorService>();

            // 智能加载管理器 - UltraThink创新组件
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.ISmartLoadingManager, LYBT.Desktop.Core.Services.SmartLoadingManager>();

            // UltraThink简化：移除AI预测和行为分析服务，保持系统简单
            // 注释掉复杂的性能优化服务
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IUserBehaviorAnalyzer, LYBT.Desktop.Core.Services.Performance.UserBehaviorAnalyzer>();
            // containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IPredictivePreloadService, LYBT.Desktop.Core.Services.Performance.PredictivePreloadService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.ISmartConcurrencyManager, LYBT.Desktop.Core.Services.Performance.SmartConcurrencyManager>();
            
            // UltraThink重构：已删除复杂监控服务，使用标准Microsoft.Extensions.Logging
            
            // 配置管理服务 - UltraThink Stage 5.3.2
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Configuration.IConfigurationManagerService, LYBT.Desktop.Core.Services.Configuration.ConfigurationManagerService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Configuration.IFeatureToggleService, LYBT.Desktop.Core.Services.Configuration.FeatureToggleService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Configuration.IHotReloadService, LYBT.Desktop.Core.Services.Configuration.HotReloadService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Configuration.ISecureConfigurationService, LYBT.Desktop.Core.Services.Configuration.SecureConfigurationService>();
            
            // API测试服务 - 修复SystemWorkbench启动问题
            containerRegistry.RegisterSingleton<ApiTestService>();
        }

        /// <summary>
        /// 注册对话框
        /// </summary>
        private static void RegisterDialogs(IContainerRegistry containerRegistry)
        {
            // 注册新的自定义对话框服务（兼容 Prism 8.1.97）
            containerRegistry.RegisterSingleton<ICustomDialogService, WpfDialogService>();

            // 注册对话框ViewModels
            containerRegistry.Register<LYBT.Desktop.Core.ViewModels.Dialogs.InputDialogViewModel>();
            containerRegistry.Register<LYBT.Desktop.Core.ViewModels.Dialogs.HerbSelectionDialogViewModel>();
            containerRegistry.Register<LYBT.Desktop.Core.ViewModels.Dialogs.FormulaSelectionDialogViewModel>();

            // 注册对话框Windows
            containerRegistry.Register<LYBT.Desktop.Core.Views.Dialogs.InputDialog>();
            containerRegistry.Register<LYBT.Desktop.Core.Views.Dialogs.HerbSelectionDialog>();
            containerRegistry.Register<LYBT.Desktop.Core.Views.Dialogs.FormulaSelectionDialog>();

            // 保留原有的简单对话框服务，用于向后兼容
            containerRegistry.RegisterSingleton<ICommonDialogService, SimpleDialogService>();

            // 注册 PrismDialogService 作为适配器（如果需要）
            // containerRegistry.RegisterSingleton<PrismDialogService>();
        }
        
        /// <summary>
        /// 注册ViewModels - 关键：让ViewModelLocator能自动装配
        /// </summary>
        private static void RegisterViewModels(IContainerRegistry containerRegistry)
        {
            // 注册Shell ViewModels
            containerRegistry.Register<LYBT.Desktop.Shell.ViewModels.HomeViewModel>();
            
            // 注册其他ViewModels（如果需要）
            // 注意：MainWindowViewModel通过构造函数注入，已经在App.xaml.cs中处理
        }
        
        /// <summary>
        /// 注册视图
        /// </summary>
        private static void RegisterViews(IContainerRegistry containerRegistry)
        {
            // 注册主页视图
            containerRegistry.RegisterForNavigation<LYBT.Desktop.Shell.Views.HomeView>("HomeView");
            
            // 注册测试视图
            containerRegistry.RegisterForNavigation<LYBT.Desktop.Shell.Views.TestView>("TestView");
        }
    }
}