// ---------------------------------------------------------------------------
// UnifiedApiClientExtensions — IApiClient registration with SwitchingApiClient
// ---------------------------------------------------------------------------
// Registers SwitchingApiClient as the singleton IApiClient, which routes to
// RefitApiClient or HttpClientApiClient based on the current connection URL.
// Also registers factory delegates for dynamic HttpClient creation.
// ---------------------------------------------------------------------------

using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using DryIoc;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Logging.Correlation;
using LYBT.Shared.Logging.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prism.DryIoc;
using Prism.Ioc;
using Refit;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// 注册 <see cref="IApiClient"/> 的扩展方法。
/// </summary>
public static class UnifiedApiClientExtensions
{
    /// <summary>
    /// 将 <see cref="SwitchingApiClient"/> 注册为单例 <see cref="IApiClient"/>。
    /// </summary>
    public static void AddUnifiedApiClient(
        this IContainerRegistry containerRegistry,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(containerRegistry);
        ArgumentNullException.ThrowIfNull(configuration);

        var apiOptions = new LYBT.Shared.Configuration.Options.Client.ApiClientOptions();
        configuration
            .GetSection(LYBT.Shared.Configuration.Options.Client.ApiClientOptions.SectionName)
            .Bind(apiOptions);
        var ignoreSslErrors = apiOptions.IgnoreSslErrors;
        var timeoutSeconds = apiOptions.TimeoutSeconds;

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() },
                }
            ),
        };

        // Get the underlying DryIoc container via Prism extension method
        var container = containerRegistry.GetContainer();

        // Factory for Remote-mode HttpClient with full handler chain
        Func<string, HttpClient> remoteHttpClientFactory = baseUrl =>
        {
            var httpHandler = new HttpClientHandler();
            if (ignoreSslErrors)
                httpHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            var tokenStorage = container.Resolve<ITokenStorageService>();
            var credentialVault = container.Resolve<ICredentialVault>();

            var apiClientOptions = Options.Create(apiOptions);
            var tokenRefreshHandler = new TokenRefreshHandler(
                tokenStorage,
                credentialVault,
                apiClientOptions,
                container.Resolve<ILogger<TokenRefreshHandler>>(),
                userActivityState: null
            );
            tokenRefreshHandler.InnerHandler = httpHandler;

            var authHandler = new AuthorizationMessageHandler(
                tokenStorage,
                container.Resolve<ILogger<AuthorizationMessageHandler>>()
            );
            authHandler.InnerHandler = tokenRefreshHandler;

            var loggingHandler = new LoggingHttpHandler(
                container.Resolve<ILogger<LoggingHttpHandler>>(),
                container.Resolve<ICorrelationIdProvider>()
            );
            loggingHandler.InnerHandler = authHandler;

            return new HttpClient(loggingHandler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
            };
        };

        // Factory for Local-mode IHttpClientFactory
        Func<string, IHttpClientFactory> localHttpClientFactory = baseUrl =>
        {
            var baseAddress = new Uri(baseUrl);
            return new LocalWebApiHttpClientFactory(baseAddress);
        };

        containerRegistry.RegisterSingleton<IApiClient>(resolver =>
        {
            var connectionSettings = resolver.Resolve<IConnectionSettingsService>();
            return new SwitchingApiClient(
                connectionSettings,
                remoteHttpClientFactory,
                localHttpClientFactory,
                refitSettings
            );
        });

        // IApiClientIdentity 单例 = SwitchingApiClient.Identity（运行时按连接模式路由到
        // Refit 远程 / HttpClient 本地——消费方（AuthenticationService/LogoutService/
        // TokenLifecycleService/AuthHealthService）直接注入该子接口。
        // desktop-di-fix 2026-08-14：原缺失导致 Desktop 启动 DI 解析崩溃
        // （Unable to resolve IApiClientIdentity as parameter "authApi"）。
        containerRegistry.RegisterSingleton<IApiClientIdentity>(resolver =>
            resolver.Resolve<IApiClient>().Identity
        );

        // 其余直接注入子接口的服务（启动验证逐层暴露——ConnectionModeService→MedicalCases、
        // DeploymentService→Deploy、DiagnosticsService→Diagnostics、
        // ServerConfigurationService→Configuration）统一注册为 SwitchingApiClient 子接口转发。
        containerRegistry.RegisterSingleton<IApiClientMedicalCases>(resolver =>
            resolver.Resolve<IApiClient>().MedicalCases
        );
        containerRegistry.RegisterSingleton<IApiClientDeploy>(resolver =>
            resolver.Resolve<IApiClient>().Deploy
        );
        containerRegistry.RegisterSingleton<IApiClientDiagnostics>(resolver =>
            resolver.Resolve<IApiClient>().Diagnostics
        );
        containerRegistry.RegisterSingleton<IApiClientConfiguration>(resolver =>
            resolver.Resolve<IApiClient>().Configuration
        );
    }

    /// <summary>
    /// Minimal <see cref="IHttpClientFactory"/> for LocalWebAPI mode.
    /// </summary>
    private sealed class LocalWebApiHttpClientFactory : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public LocalWebApiHttpClientFactory(Uri baseAddress)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromSeconds(30),
            };
        }

        public HttpClient CreateClient(string name) => _httpClient;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
