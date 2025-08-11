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
using LYBT.Desktop.Services;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Core.Mapping;
using LYBT.Desktop.Core.Models.Cache;
using LYBT.Desktop.Infrastructure;
using LYBT.Desktop.Services.Handlers;

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
            containerRegistry.RegisterSingleton<CacheOptions>(() =>
            {
#if DEBUG
                return CacheOptions.Development();
#else
                return CacheOptions.Production();
#endif
            });

            // 注册缓存服务
            containerRegistry.RegisterSingleton<ICacheService, MemoryCacheService>();
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
            containerRegistry.RegisterSingleton<IErrorHandlingService, ErrorHandlingService>();
        }

        /// <summary>
        /// 注册领域业务服务
        /// </summary>
        private static void RegisterDomainServices(IContainerRegistry containerRegistry)
        {
            var domainServices = new (Type Interface, Type Implementation)[]
            {
                (typeof(IAuthenticationService), typeof(AuthenticationService)),
                (typeof(IUserService), typeof(UserService)),
                (typeof(IPatientService), typeof(PatientService)),
                (typeof(IHerbService), typeof(HerbService)),
                (typeof(IFormulaService), typeof(FormulaService)),
                (typeof(IConsultationService), typeof(ConsultationService)),
                (typeof(IPrescriptionService), typeof(PrescriptionService)),
                (typeof(IMedicalCaseService), typeof(MedicalCaseService))
            };

            foreach (var (interfaceType, implementationType) in domainServices)
            {
                containerRegistry.RegisterSingleton(interfaceType, implementationType);
            }
        }

        /// <summary>
        /// 注册支持性服务
        /// </summary>
        private static void RegisterSupportingServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IPrescriptionPrintService, SimplePrescriptionPrintService>();
            containerRegistry.RegisterSingleton<ICredentialService, CredentialService>();
            containerRegistry.RegisterSingleton<IPrescriptionValidationService, PrescriptionValidationService>();
            containerRegistry.RegisterSingleton<IIDCardReaderService, MockIDCardReaderService>();
        }

        /// <summary>
        /// 注册对话框
        /// </summary>
        private static void RegisterDialogs(IContainerRegistry containerRegistry)
        {
            // 暂时不注册 Prism 对话框，因为 IDialogAware 接口有兼容性问题

            // 注册简单的对话框服务，使用 MessageBox 实现
            containerRegistry.RegisterSingleton<ICommonDialogService, SimpleDialogService>();
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