// ---------------------------------------------------------------------------
// UnifiedApiClientExtensions — IApiClient registration based on ApiMode
// ---------------------------------------------------------------------------
// Reads ApiMode from appsettings.json and registers either:
//   - RefitApiClient (Remote mode): reuses existing HttpClient + RefitSettings
//   - HttpClientApiClient (Local mode): creates IHttpClientFactory with LocalBaseUrl
//
// This replaces the scattered IApiClient registration logic and provides a
// single entry point for the unified API client pipeline.
// ---------------------------------------------------------------------------

using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Foundation.Http;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Configuration;
using Prism.Ioc;
using Refit;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// Extension methods for registering <see cref="IApiClient"/> based on ApiMode configuration.
/// </summary>
public static class UnifiedApiClientExtensions
{
    /// <summary>
    /// Registers <see cref="IApiClient"/> as a singleton, selecting the implementation
    /// based on the <c>ApiClient:ApiMode</c> configuration value.
    /// </summary>
    /// <param name="containerRegistry">The Prism DryIoc container registry.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <remarks>
    /// <para><b>Remote mode</b> (default): Reuses the existing <see cref="HttpClient"/> (with handler chain:
    /// HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler)
    /// and creates a <see cref="RefitApiClient"/> with Refit serialization settings.</para>
    /// <para><b>Local mode</b>: Registers a dedicated <see cref="IHttpClientFactory"/> pointing to
    /// <c>ApiClient:LocalBaseUrl</c> (or <c>OfflineMode:LocalApiBaseUrl</c> as fallback), then creates
    /// an <see cref="HttpClientApiClient"/> for LocalWebAPI mode.</para>
    /// <para>Existing per-module Refit registrations (IAuthApi, IPatientApi, etc.) in
    /// <see cref="HttpServiceRegistrationExtensions.RegisterHttpServices"/> are preserved for
    /// backward compatibility — they do NOT conflict with this IApiClient registration.</para>
    /// </remarks>
    public static void AddUnifiedApiClient(
        this IContainerRegistry containerRegistry,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry);
        ArgumentNullException.ThrowIfNull(configuration);

        var apiMode = configuration["ApiClient:ApiMode"] ?? "Remote";

        if (string.Equals(apiMode, "Local", StringComparison.OrdinalIgnoreCase))
        {
            RegisterLocalApiClient(containerRegistry, configuration);
        }
        else
        {
            RegisterRemoteApiClient(containerRegistry);
        }
    }

    /// <summary>
    /// Registers <see cref="IApiClient"/> backed by <see cref="RefitApiClient"/> (Remote mode).
    /// Reuses the existing <see cref="HttpClient"/> singleton with the full handler chain.
    /// </summary>
    private static void RegisterRemoteApiClient(IContainerRegistry containerRegistry)
    {
        // Reuse existing HttpClient singleton (registered in HttpServiceRegistrationExtensions)
        // with the full handler chain: HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            })
        };

        // Register as deferred factory — HttpClient is a singleton already in the container,
        // so resolving it at factory invocation time is safe.
        containerRegistry.RegisterSingleton<IApiClient>(resolver =>
        {
            var httpClient = resolver.Resolve<HttpClient>();
            return new RefitApiClient(httpClient, refitSettings);
        });
    }

    /// <summary>
    /// Registers <see cref="IApiClient"/> backed by <see cref="HttpClientApiClient"/> (Local mode).
    /// Ensures <see cref="IHttpClientFactory"/> is available with the local base address
    /// before registering the API client.
    /// </summary>
    /// <remarks>
    /// Local base URL resolution order:
    /// <list type="number">
    ///   <item><c>ApiClient:LocalBaseUrl</c> (new unified config)</item>
    ///   <item><c>OfflineMode:LocalApiBaseUrl</c> (existing fallback)</item>
    ///   <item>Default: <c>http://localhost:5100</c></item>
    /// </list>
    /// </remarks>
    private static void RegisterLocalApiClient(
        IContainerRegistry containerRegistry,
        IConfiguration configuration)
    {
        // Resolve local base URL from config (priority: ApiClient:LocalBaseUrl > OfflineMode:LocalApiBaseUrl > default)
        var localBaseUrl = configuration["ApiClient:LocalBaseUrl"]
            ?? configuration["OfflineMode:LocalApiBaseUrl"]
            ?? "http://localhost:5100";

        var baseAddress = new Uri(localBaseUrl);

        // Register IHttpClientFactory as singleton with the local base address
        // HttpClientApiClient uses IHttpClientFactory.CreateClient() to get clients
        containerRegistry.RegisterSingleton<IHttpClientFactory>(() => new LocalWebApiHttpClientFactory(baseAddress));

        containerRegistry.AddHttpClientApiClient();
    }

    /// <summary>
    /// Minimal <see cref="IHttpClientFactory"/> for LocalWebAPI mode.
    /// Returns a shared <see cref="HttpClient"/> with the configured base address.
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
