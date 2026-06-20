using System.Net.Http;
using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Infrastructure.Http;

/// <summary>
/// Thread-safe DelegatingHandler that rewrites request URIs based on
/// the current connection mode (Local vs Remote). Replaces the unsafe
/// HttpClient.BaseAddress mutation approach.
/// </summary>
public sealed class BaseUrlDelegatingHandler : DelegatingHandler
{
    private readonly IConnectionSettingsService _connectionSettings;

    public BaseUrlDelegatingHandler(IConnectionSettingsService connectionSettings)
    {
        _connectionSettings = connectionSettings;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null)
        {
            var currentUrl = _connectionSettings.CurrentUrl;
            if (Uri.TryCreate(currentUrl, UriKind.Absolute, out var baseUri))
            {
                request.RequestUri = new Uri(baseUri, request.RequestUri.PathAndQuery);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
