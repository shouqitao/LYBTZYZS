// ---------------------------------------------------------------------------
// ConnectionSettingsService — Connection URL management implementation
// ---------------------------------------------------------------------------

using System.IO;
using System.Text.Json.Nodes;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// Manages the active API connection URL, persists it to appsettings.json,
/// and notifies subscribers on change. Supports PreferredMode + RemoteUrl
/// persistence for explicit mode switching.
/// </summary>
public sealed class ConnectionSettingsService : IConnectionSettingsService
{
    /// <summary>LocalWebAPI fixed address (always http://localhost:5300).</summary>
    public const string LocalUrlConstant = "http://localhost:5300";

    private readonly ILogger<ConnectionSettingsService> _logger;
    private readonly string _settingsFilePath;

    private string _currentUrl;
    private string _remoteUrl;
    private string _preferredMode;

    public ConnectionSettingsService(
        IOptions<ApiClientOptions> apiOptions,
        ILogger<ConnectionSettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var options = apiOptions.Value;
        var baseUrl = options.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = LocalUrlConstant;
            _logger.LogInformation("[CONNECTION-CFG] No saved BaseUrl, defaulting to {Url}", baseUrl);
        }
        _currentUrl = baseUrl;

        _remoteUrl = options.RemoteUrl ?? string.Empty;

        var preferred = options.PreferredMode;
        _preferredMode = string.IsNullOrWhiteSpace(preferred) ? "Local" : preferred;

        _settingsFilePath = Path.Combine(
            AppContext.BaseDirectory, "appsettings.json");
    }

    /// <inheritdoc />
    public string LocalUrl => LocalUrlConstant;

    /// <inheritdoc />
    public string RemoteUrl => _remoteUrl;

    /// <inheritdoc />
    public string PreferredMode => _preferredMode;

    /// <inheritdoc />
    public string CurrentUrl =>
        _preferredMode == "Remote" && !string.IsNullOrEmpty(_remoteUrl)
            ? _remoteUrl
            : LocalUrlConstant;

    /// <inheritdoc />
    public bool IsLocal => _preferredMode != "Remote" || string.IsNullOrEmpty(_remoteUrl);

    /// <inheritdoc />
    public event EventHandler<string>? UrlChanged;

    /// <inheritdoc />
    public async Task SetUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        if (!IsValidUrl(url))
            throw new ArgumentException($"Invalid URL format: {url}", nameof(url));

        var normalized = url.TrimEnd('/');

        if (IsLocalUrl(normalized))
        {
            _preferredMode = "Local";
            await PersistPreferredModeAsync("Local").ConfigureAwait(false);
        }
        else
        {
            _remoteUrl = normalized;
            _preferredMode = "Remote";
            await PersistRemoteUrlAsync(normalized).ConfigureAwait(false);
            await PersistPreferredModeAsync("Remote").ConfigureAwait(false);
        }

        var oldUrl = _currentUrl;
        _currentUrl = CurrentUrl;

        _logger.LogInformation("[CONNECTION-CFG] URL changed: {OldUrl} -> {NewUrl}", oldUrl, _currentUrl);

        UrlChanged?.Invoke(this, _currentUrl);
    }

    /// <inheritdoc />
    public async Task SaveRemoteUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _remoteUrl = string.Empty;
        }
        else
        {
            if (!IsValidUrl(url))
                throw new ArgumentException($"Invalid URL format: {url}", nameof(url));

            _remoteUrl = url.TrimEnd('/');
        }

        await PersistRemoteUrlAsync(_remoteUrl).ConfigureAwait(false);

        var oldUrl = _currentUrl;
        _currentUrl = CurrentUrl;
        if (oldUrl != _currentUrl)
        {
            UrlChanged?.Invoke(this, _currentUrl);
        }
    }

    /// <inheritdoc />
    public async Task SavePreferredModeAsync(string mode)
    {
        if (mode != "Local" && mode != "Remote")
            throw new ArgumentException($"Invalid mode: {mode}. Expected 'Local' or 'Remote'.", nameof(mode));

        _preferredMode = mode;
        await PersistPreferredModeAsync(mode).ConfigureAwait(false);

        var oldUrl = _currentUrl;
        _currentUrl = CurrentUrl;
        if (oldUrl != _currentUrl)
        {
            UrlChanged?.Invoke(this, _currentUrl);
        }
    }

    /// <inheritdoc />
    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private async Task PersistRemoteUrlAsync(string url)
    {
        await PersistSettingAsync("ApiClient", "RemoteUrl", url).ConfigureAwait(false);
    }

    private async Task PersistPreferredModeAsync(string mode)
    {
        await PersistSettingAsync("ApiClient", "PreferredMode", mode).ConfigureAwait(false);
    }

    /// <summary>
    /// Persist a single key under a top-level section, creating the section
    /// if missing. Other keys in the section are preserved.
    /// </summary>
    private async Task PersistSettingAsync(string section, string key, string value)
    {
        try
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var root = JsonNode.Parse(json)?.AsObject();
            if (root is null) return;

            if (root.TryGetPropertyValue(section, out var sectionNode) && sectionNode is JsonObject sectionObj)
            {
                sectionObj[key] = value;
            }
            else
            {
                root[section] = JsonNode.Parse($"{{\"{key}\": \"{value}\"}}");
            }

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(_settingsFilePath,
                root.ToJsonString(options));

            _logger.LogDebug("[CONNECTION-CFG] Persisted {Section}:{Key} = {Value}", section, key, value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CONNECTION-CFG] Failed to persist {Section}:{Key}", section, key);
        }
    }

    private static bool IsLocalUrl(string url)
    {
        // LocalWebAPI runs on port 5300. Any localhost/127.0.0.1 URL on a
        // different port (e.g., 5000) is a remote WebAPI running locally.
        return url.Contains("localhost:5300", StringComparison.OrdinalIgnoreCase)
            || url.Contains("127.0.0.1:5300", StringComparison.OrdinalIgnoreCase);
    }
}
