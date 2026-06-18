// ---------------------------------------------------------------------------
// ConnectionSettingsService — Connection URL management implementation
// ---------------------------------------------------------------------------

using System.IO;
using System.Text.Json.Nodes;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// Manages the active API connection URL, persists it to appsettings.json,
/// and notifies subscribers on change.
/// </summary>
public sealed class ConnectionSettingsService : IConnectionSettingsService
{
    private readonly ILogger<ConnectionSettingsService> _logger;
    private readonly string _settingsFilePath;
    private string _currentUrl;

    public ConnectionSettingsService(
        IConfiguration configuration,
        ILogger<ConnectionSettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var url = configuration["ApiClient:BaseUrl"];
        if (string.IsNullOrWhiteSpace(url))
        {
            url = "http://localhost:5000";
            _logger.LogInformation("[CONNECTION-CFG] No saved URL, defaulting to {Url}", url);
        }
        _currentUrl = url;

        _settingsFilePath = Path.Combine(
            Directory.GetCurrentDirectory(), "appsettings.json");
    }

    /// <inheritdoc />
    public string CurrentUrl => _currentUrl;

    /// <inheritdoc />
    public bool IsLocal => IsLocalUrl(_currentUrl);

    /// <inheritdoc />
    public event EventHandler<string>? UrlChanged;

    /// <inheritdoc />
    public async Task SetUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        if (!IsValidUrl(url))
            throw new ArgumentException($"Invalid URL format: {url}", nameof(url));

        if (_currentUrl == url)
            return;

        var oldUrl = _currentUrl;
        _currentUrl = url;

        _logger.LogInformation("[CONNECTION-CFG] URL changed: {OldUrl} -> {NewUrl}", oldUrl, url);

        await PersistUrlAsync(url);

        UrlChanged?.Invoke(this, url);
    }

    /// <inheritdoc />
    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private async Task PersistUrlAsync(string url)
    {
        try
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var root = JsonNode.Parse(json)?.AsObject();
            if (root is null) return;

            if (root.TryGetPropertyValue("ApiClient", out var apiClientNode)
                && apiClientNode is JsonObject)
            {
                ((JsonObject)apiClientNode)["BaseUrl"] = url;
            }
            else
            {
                root["ApiClient"] = JsonNode.Parse(
                    $"{{\"BaseUrl\": \"{url}\"}}");
            }

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(_settingsFilePath,
                root.ToJsonString(options));

            _logger.LogDebug("[CONNECTION-CFG] URL persisted");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CONNECTION-CFG] Failed to persist URL");
        }
    }

    private static bool IsLocalUrl(string url)
    {
        return url.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || url.Contains("localhost", StringComparison.OrdinalIgnoreCase);
    }
}
