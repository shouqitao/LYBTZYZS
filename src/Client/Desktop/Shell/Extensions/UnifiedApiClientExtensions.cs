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
using LYBT.Desktop.Shared.Models;
using LYBT.Desktop.Contracts.Services;
using Prism.DryIoc;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Refit;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// Extension methods for registering <see cref="IApiClient"/>.
/// </summary>
public static class UnifiedApiClientExtensions
{
    /// <summary>
    /// Registers <see cref="SwitchingApiClient"/> as the singleton <see cref="IApiClient"/>.
    /// </summary>
    public static void AddUnifiedApiClient(
        this IContainerRegistry containerRegistry,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry);
        ArgumentNullException.ThrowIfNull(configuration);

        var apiOptions = new LYBT.Shared.Configuration.Options.Client.ApiClientOptions();
        configuration.GetSection(
            LYBT.Shared.Configuration.Options.Client.ApiClientOptions.SectionName)
            .Bind(apiOptions);
        var ignoreSslErrors = apiOptions.IgnoreSslErrors;
        var timeoutSeconds = apiOptions.TimeoutSeconds;

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            })
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

            var tokenRefreshHandler = new TokenRefreshHandler(
                tokenStorage, credentialVault, configuration,
                container.Resolve<ILogger<TokenRefreshHandler>>(),
                userActivityState: null);
            tokenRefreshHandler.InnerHandler = httpHandler;

            var authHandler = new AuthorizationMessageHandler(
                tokenStorage,
                container.Resolve<ILogger<AuthorizationMessageHandler>>());
            authHandler.InnerHandler = tokenRefreshHandler;

            var loggingHandler = new LoggingHttpHandler(
                container.Resolve<ILogger<LoggingHttpHandler>>());
            loggingHandler.InnerHandler = authHandler;

            return new HttpClient(loggingHandler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
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
                refitSettings);
        });
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
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public HttpClient CreateClient(string name) => _httpClient;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
