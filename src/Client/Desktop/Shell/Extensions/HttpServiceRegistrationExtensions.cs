using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Http;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Refit;

namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>HTTP相关服务注册扩展方法</summary>
    public static class HttpServiceRegistrationExtensions
    {
        /// <summary>注册HTTP相关服务</summary>
        /// <remarks>adopt-activity-api-tracing: HttpClient自动传播W3C TraceContext，无需自定义Handler</remarks>
        public static void RegisterHttpServices(this IContainerRegistry containerRegistry, IConfiguration config)
        {
            // unify-configuration-system: 使用强类型配置
            var apiOptions = new ApiClientOptions();
            config.GetSection(ApiClientOptions.SectionName).Bind(apiOptions);
            var apiBaseUrl = apiOptions.BaseUrl;
            var ignoreSslErrors = apiOptions.IgnoreSslErrors;

            containerRegistry.RegisterSingleton<AuthorizationMessageHandler>();
            containerRegistry.RegisterSingleton<TokenRefreshHandler>(resolver =>
            {
                var tokenStorage = resolver.Resolve<ITokenStorageService>();
                var credentialVault = resolver.Resolve<ICredentialVault>();
                var configuration = resolver.Resolve<IConfiguration>();
                var logger = resolver.Resolve<ILogger<TokenRefreshHandler>>();
                IUserActivityState? userActivityState = null;
                try { userActivityState = resolver.Resolve<IUserActivityState>(); }
                catch { /* 启动阶段可能尚未注册 */ }
                return new TokenRefreshHandler(tokenStorage, credentialVault, configuration, logger, userActivityState);
            });
            // OpenSpec: refactor-login-authentication (Phase 1.4) - 注册接口
            containerRegistry.Register<ITokenRefreshHandler>(resolver => resolver.Resolve<TokenRefreshHandler>());

            // LOG-012: 注册LoggingHttpHandler
            containerRegistry.RegisterSingleton<LoggingHttpHandler>();

            // Handler链: HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler → HttpClient
            // LOG-012 & LOG-013: LoggingHttpHandler记录请求/响应并添加traceparent header
            containerRegistry.RegisterSingleton<HttpClient>(resolver =>
            {
                var httpHandler = new HttpClientHandler();
                if (ignoreSslErrors)
                    httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                var tokenRefreshHandler = resolver.Resolve<TokenRefreshHandler>();
                tokenRefreshHandler.InnerHandler = httpHandler;
                var authHandler = resolver.Resolve<AuthorizationMessageHandler>();
                authHandler.InnerHandler = tokenRefreshHandler;
                // LOG-012: 添加日志Handler到链中
                var loggingHandler = resolver.Resolve<LoggingHttpHandler>();
                loggingHandler.InnerHandler = authHandler;

                return new HttpClient(loggingHandler) { BaseAddress = new Uri(apiBaseUrl), Timeout = TimeSpan.FromSeconds(30) };
            });

            // Refit客户端共享HttpClient实例 - 配置JSON序列化以支持枚举字符串转换
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
                })
            };
            containerRegistry.RegisterSingleton<IAuthApi>(r => RestService.For<IAuthApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IPatientApi>(r => RestService.For<IPatientApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IUserApi>(r => RestService.For<IUserApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IHerbApi>(r => RestService.For<IHerbApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IFormulaApi>(r => RestService.For<IFormulaApi>(r.Resolve<HttpClient>(), refitSettings));
            containerRegistry.RegisterSingleton<IMedicalCaseApi>(r => RestService.For<IMedicalCaseApi>(r.Resolve<HttpClient>(), refitSettings));
            // OpenSpec: implement-data-sync - 同步API客户端
            containerRegistry.RegisterSingleton<ISyncApi>(r => RestService.For<ISyncApi>(r.Resolve<HttpClient>(), refitSettings));
        }
    }
}
