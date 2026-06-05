using System.Net.Http;
using LYBT.Desktop.Contracts.ApiClient;
using Prism.Ioc;
using Refit;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// Extension methods for registering <see cref="RefitApiClient"/> in the Prism DryIoc container.
/// </summary>
public static class RefitApiClientExtensions
{
    /// <summary>
    /// Registers <see cref="IApiClient"/> as a singleton backed by <see cref="RefitApiClient"/>.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="RefitApiClient"/> lazily creates Refit-generated HTTP clients for each
    /// domain API interface (IAuthApi, IUserApi, etc.) and wraps them in adapter classes that
    /// implement the <see cref="IApiClient"/> sub-interfaces.</para>
    /// <para>The shared <see cref="HttpClient"/> should have the full handler chain configured:
    /// HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler.</para>
    /// </remarks>
    /// <param name="containerRegistry">The Prism DryIoc container registry.</param>
    /// <param name="httpClient">
    /// Shared <see cref="HttpClient"/> with the configured handler chain and base address.
    /// </param>
    /// <param name="refitSettings">
    /// Refit serialization settings (camelCase, StringEnumConverter, etc.).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="containerRegistry"/>, <paramref name="httpClient"/>,
    /// or <paramref name="refitSettings"/> is null.
    /// </exception>
    public static void AddRefitApiClient(
        this IContainerRegistry containerRegistry,
        HttpClient httpClient,
        RefitSettings refitSettings)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(refitSettings);

        containerRegistry.RegisterSingleton<IApiClient>(_ => new RefitApiClient(httpClient, refitSettings));
    }
}
