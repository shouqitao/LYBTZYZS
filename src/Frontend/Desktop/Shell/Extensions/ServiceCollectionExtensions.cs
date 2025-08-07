using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prism.Ioc;
using Refit;
using AutoMapper;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Core.Configuration;
using LYBT.WPF.Client.Core.Mapping;
using LYBT.WPF.Client.Infrastructure;
using LYBT.WPF.Client.Services.Handlers;

namespace LYBT.WPF.Client.Shell.Extensions
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
            RegisterHttpServices(containerRegistry);
            RegisterApiServices(containerRegistry);
            RegisterBusinessServices(containerRegistry);
            RegisterDialogs(containerRegistry);
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
            
            // 注册泛型日志器
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
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
        /// 注册HTTP相关服务
        /// </summary>
        private static void RegisterHttpServices(IContainerRegistry containerRegistry)
        {
            // 注册HttpClient，配置超时时间
            containerRegistry.RegisterSingleton<HttpClient>(() =>
            {
                var client = CreateHttpClient();
                client.Timeout = TimeSpan.FromSeconds(60);
                return client;
            });
        }

        /// <summary>
        /// 创建配置好的HttpClient（开发环境忽略SSL证书验证）
        /// </summary>
        private static HttpClient CreateHttpClient()
        {
#if DEBUG
            // 开发环境忽略SSL证书验证
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            return HttpClientFactory.CreateWithRetryPolicy(handler);
#else
            // 生产环境使用默认设置
            return HttpClientFactory.CreateWithRetryPolicy(new HttpClientHandler());
#endif
        }

        /// <summary>
        /// 创建带认证的HttpClient
        /// </summary>
        private static HttpClient CreateAuthenticatedHttpClient(IContainerProvider container)
        {
            var tokenManager = container.Resolve<ITokenManager>();
            var authHandler = new AuthHeaderHandler(tokenManager);
            
#if DEBUG
            // 开发环境忽略SSL证书验证
            var innerHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            authHandler.InnerHandler = innerHandler;
#else
            authHandler.InnerHandler = new HttpClientHandler();
#endif

            return HttpClientFactory.CreateWithRetryPolicy(authHandler);
        }

        /// <summary>
        /// 注册API服务
        /// </summary>
        private static void RegisterApiServices(IContainerRegistry containerRegistry)
        {
            // 注册Refit API服务
            containerRegistry.RegisterSingleton<IAuthApiService>(() =>
            {
                var httpClient = CreateHttpClient();
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IAuthApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册用户API服务（使用Factory以获取容器）
            containerRegistry.Register<IUserApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IUserApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册药材API服务
            containerRegistry.Register<IHerbApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IHerbApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });


            // 注册验方模板API服务
            containerRegistry.Register<IFormulaApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IFormulaApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });


            // 注册患者API服务
            containerRegistry.Register<IPatientsApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IPatientsApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });


            // 注册日志API服务
//             {
//                 var httpClient = CreateAuthenticatedHttpClient(container);
//                 httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
//                 httpClient.Timeout = TimeSpan.FromSeconds(60);
//                 return RestService.For<ILogsApiService>(httpClient, RefitConfiguration.GetRefitSettings());
//             });

            // 注册系统设置API服务
            containerRegistry.Register<ISystemSettingsApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<ISystemSettingsApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册备份API服务
            containerRegistry.Register<IBackupApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IBackupApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册看诊API服务
            containerRegistry.Register<IConsultationApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IConsultationApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册处方API服务
            containerRegistry.Register<IPrescriptionApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IPrescriptionApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册通用API服务
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Services.IApiService, LYBT.WPF.Client.Services.ApiService>();
        }

        /// <summary>
        /// 注册业务服务
        /// </summary>
        private static void RegisterBusinessServices(IContainerRegistry containerRegistry)
        {
            // 核心服务
            containerRegistry.RegisterSingleton<ITokenManager, TokenManager>();
            containerRegistry.RegisterSingleton<IUserSessionManager, UserSessionManager>();
            containerRegistry.RegisterSingleton<IPermissionService, PermissionService>();

            // 业务服务
            containerRegistry.RegisterSingleton<IAuthenticationService, AuthenticationService>();
            containerRegistry.RegisterSingleton<IUserService, UserService>();
            containerRegistry.RegisterSingleton<IPatientService, PatientService>();
            containerRegistry.RegisterSingleton<IHerbService, HerbService>();
            containerRegistry.RegisterSingleton<IFormulaService, FormulaService>();
            containerRegistry.RegisterSingleton<IConsultationService, ConsultationService>();
            containerRegistry.RegisterSingleton<IPrescriptionPrintService, SimplePrescriptionPrintService>();
            containerRegistry.RegisterSingleton<ICredentialService, CredentialService>();
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
    }
}