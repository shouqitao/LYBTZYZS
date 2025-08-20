using System;
using System.Net.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AutoMapper;
using Prism.Ioc;
using Refit;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Mapping;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Infrastructure;
using LYBT.Desktop.Services;
using LYBT.Desktop.Services.Handlers;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;

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
            // 注册简单的控制台日志提供程序
            containerRegistry.RegisterSingleton<ILoggerFactory>(() =>
            {
                return LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));
            });

            // 注册泛型日志接口
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
        }

        /// <summary>
        /// 注册AutoMapper
        /// </summary>
        private static void RegisterAutoMapper(IContainerRegistry containerRegistry)
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            }, NullLoggerFactory.Instance);

            var mapper = mapperConfig.CreateMapper();
            containerRegistry.RegisterInstance<IMapper>(mapper);
        }

        /// <summary>
        /// 注册缓存服务
        /// </summary>
        private static void RegisterCacheServices(IContainerRegistry containerRegistry)
        {
            // 注册内存缓存
            containerRegistry.RegisterSingleton<IMemoryCache, MemoryCache>();
            
            // 注册简化的缓存服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.MemoryCacheService>();
        }

        /// <summary>
        /// 注册HTTP相关服务
        /// </summary>
        private static void RegisterHttpServices(IContainerRegistry containerRegistry)
        {
            // 注册基础HttpClient
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
            // TODO: 添加实际存在的API接口
            
            // 注册通用API服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.IApiService, LYBT.Desktop.Services.ApiService>();
        }

        /// <summary>
        /// 注册业务服务
        /// </summary>
        private static void RegisterBusinessServices(IContainerRegistry containerRegistry)
        {
            RegisterCoreServices(containerRegistry);
            RegisterDomainServices(containerRegistry);
        }

        /// <summary>
        /// 注册核心基础服务
        /// </summary>
        private static void RegisterCoreServices(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<TokenManager>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IUserSessionManager, UserSessionManager>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IPermissionService, PermissionService>();
            
            // SessionManager和NotificationService
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.ISessionManager, 
                LYBT.Desktop.Core.Services.SessionManager>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.INotificationService, 
                LYBT.Desktop.Core.Services.NotificationService>();

            // 认证服务
            containerRegistry.RegisterSingleton<AuthenticationService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService>(
                container => container.Resolve<AuthenticationService>());
        }

        /// <summary>
        /// 注册领域业务服务
        /// </summary>
        private static void RegisterDomainServices(IContainerRegistry containerRegistry)
        {
            // API测试服务
            containerRegistry.RegisterSingleton<ApiTestService>();
        }

        /// <summary>
        /// 注册对话框服务
        /// </summary>
        private static void RegisterDialogs(IContainerRegistry containerRegistry)
        {
            // 使用简化的对话框服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService, 
                LYBT.Desktop.Core.Services.WpfDialogService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Services.SimpleDialogService>();
        }

        /// <summary>
        /// 注册ViewModels
        /// </summary>
        private static void RegisterViewModels(IContainerRegistry containerRegistry)
        {
            // ViewModels通过Prism自动解析
        }

        /// <summary>
        /// 注册Views
        /// </summary>
        private static void RegisterViews(IContainerRegistry containerRegistry)
        {
            // Views通过Prism的ViewModelLocator自动注册
        }

        #region 辅助方法

        /// <summary>
        /// 注册基础API服务（无认证）
        /// </summary>
        private static void RegisterBasicApiService<T>(IContainerRegistry containerRegistry) where T : class
        {
            containerRegistry.RegisterSingleton<T>(() =>
            {
                var httpClient = HttpClientFactory.CreateBasicClient(ApiConfiguration.BaseUrl);
                return RestService.For<T>(httpClient);
            });
        }

        /// <summary>
        /// 注册需要认证的API服务
        /// </summary>
        private static void RegisterAuthenticatedApiService<T>(IContainerRegistry containerRegistry) where T : class
        {
            containerRegistry.RegisterSingleton<T>(() =>
            {
                var httpClient = HttpClientFactory.CreateBasicClient(ApiConfiguration.BaseUrl);
                return RestService.For<T>(httpClient);
            });
        }

        #endregion
    }
}