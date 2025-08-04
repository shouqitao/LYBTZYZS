using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Prism.Ioc;
using Refit;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Core.Configuration;
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
            RegisterHttpServices(containerRegistry);
            RegisterApiServices(containerRegistry);
            RegisterBusinessServices(containerRegistry);
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
            return new HttpClient(handler);
#else
            // 生产环境使用默认设置
            return new HttpClient();
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

            return new HttpClient(authHandler);
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

            // 注册病例API服务
            containerRegistry.Register<IRecordApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IRecordApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册验方模板API服务
            containerRegistry.Register<IFormulaTemplateApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IFormulaTemplateApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册挂号API服务
            containerRegistry.Register<IRegistrationApiService>(container =>
            {
                var httpClient = CreateAuthenticatedHttpClient(container);
                httpClient.BaseAddress = new Uri(ApiConfiguration.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                return RestService.For<IRegistrationApiService>(httpClient, RefitConfiguration.GetRefitSettings());
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
            containerRegistry.RegisterSingleton<IDoctorService, DoctorService>();
            containerRegistry.RegisterSingleton<IHerbService, HerbService>();
            containerRegistry.RegisterSingleton<IRecordService, RecordService>();
            containerRegistry.RegisterSingleton<IFormulaTemplateService, FormulaTemplateService>();
            containerRegistry.RegisterSingleton<IRegistrationService, RegistrationService>();
            containerRegistry.RegisterSingleton<IBillingService, BillingService>();
            containerRegistry.RegisterSingleton<IPharmacyService, PharmacyService>();
            containerRegistry.RegisterSingleton<IPhysiotherapyService, PhysiotherapyService>();
            containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();
            containerRegistry.RegisterSingleton<ICredentialService, CredentialService>();
        }
    }
}