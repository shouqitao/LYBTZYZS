using System;
using System.Net.Http;
using Prism.Ioc;
using Refit;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Core.Configuration;
using LYBT.WPF.Client.Infrastructure;

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
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(60);
                return client;
            });
        }

        /// <summary>
        /// 注册API服务
        /// </summary>
        private static void RegisterApiServices(IContainerRegistry containerRegistry)
        {
            // 注册Refit API服务
            containerRegistry.RegisterSingleton<IAuthApiService>(() =>
            {
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(ApiConfiguration.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(60)
                };
                return RestService.For<IAuthApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册用户API服务
            containerRegistry.RegisterSingleton<IUserApiService>(() =>
            {
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(ApiConfiguration.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(60)
                };
                return RestService.For<IUserApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册药材API服务
            containerRegistry.RegisterSingleton<IHerbApiService>(() =>
            {
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(ApiConfiguration.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(60)
                };
                return RestService.For<IHerbApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册病例API服务
            containerRegistry.RegisterSingleton<IRecordApiService>(() =>
            {
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(ApiConfiguration.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(60)
                };
                return RestService.For<IRecordApiService>(httpClient, RefitConfiguration.GetRefitSettings());
            });

            // 注册验方模板API服务
            containerRegistry.RegisterSingleton<IFormulaTemplateApiService>(() =>
            {
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(ApiConfiguration.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(60)
                };
                return RestService.For<IFormulaTemplateApiService>(httpClient, RefitConfiguration.GetRefitSettings());
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
            containerRegistry.RegisterSingleton<IRecordService, RecordService>();
            containerRegistry.RegisterSingleton<IFormulaTemplateService, FormulaTemplateService>();
            containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();
        }
    }
}