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
/// 管理活动 API 连接 URL，持久化到用户设置文件
/// （%LOCALAPPDATA%/LYBTZYZS/user-settings.json），并与 appsettings.json 分离——
/// 构建 --no-incremental 会以源码 appsettings.json 覆盖 bin 目录副本导致用户配置丢失。
/// 支持 PreferredMode + RemoteUrl 持久化以显式切换模式。
/// </summary>
public sealed class ConnectionSettingsService : IConnectionSettingsService
{
    /// <summary>LocalWebAPI 固定地址（始终为 http://localhost:5300）。</summary>
    public const string LocalUrlConstant = "http://localhost:5300";

    /// <summary>用户设置文件名（与 appsettings.json 分离，构建不覆盖）。</summary>
    public const string SettingsFileName = "user-settings.json";

    /// <summary>默认用户设置目录（%LOCALAPPDATA%/LYBTZYZS）。</summary>
    public static string DefaultUserSettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LYBTZYZS");

    /// <summary>默认用户设置文件路径。</summary>
    public static string DefaultUserSettingsPath => Path.Combine(
        DefaultUserSettingsDirectory, SettingsFileName);

    private readonly ILogger<ConnectionSettingsService> _logger;
    private readonly string _settingsFilePath;

    private string _currentUrl = LocalUrlConstant;
    private string _remoteUrl = string.Empty;
    private string _preferredMode = "Local";

    public ConnectionSettingsService(
        IOptions<ApiClientOptions> apiOptions,
        ILogger<ConnectionSettingsService> logger,
        string? settingsFilePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsFilePath = settingsFilePath ?? DefaultUserSettingsPath;

        LoadInitialValues(apiOptions?.Value ?? new ApiClientOptions());
    }

    /// <summary>
    /// 加载初始连接配置：用户设置文件优先，缺失键回退 appsettings 配置值。
    /// </summary>
    private void LoadInitialValues(ApiClientOptions options)
    {
        var userSettings = TryLoadUserSettings();

        var baseUrl = FirstNonEmpty(userSettings.BaseUrl, options.BaseUrl);
        _currentUrl = string.IsNullOrWhiteSpace(baseUrl) ? LocalUrlConstant : baseUrl;
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _logger.LogInformation("[CONNECTION-CFG] No BaseUrl configured, defaulting to {Url}", _currentUrl);
        }

        // RemoteUrl 空值（用户显式清空）优先于配置默认值：null 表示键缺失才回退。
        _remoteUrl = userSettings.RemoteUrl is not null
            ? userSettings.RemoteUrl.Trim()
            : options.RemoteUrl?.Trim() ?? string.Empty;

        var preferred = FirstNonEmpty(userSettings.PreferredMode, options.PreferredMode);
        _preferredMode = string.IsNullOrWhiteSpace(preferred) ? "Local" : preferred;
    }

    private static string? FirstNonEmpty(string? a, string? b)
        => !string.IsNullOrWhiteSpace(a) ? a : b;

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
            : _currentUrl;

    /// <inheritdoc />
    public bool IsLocal => CurrentUrl.Contains("127.0.0.1") || CurrentUrl.Contains("localhost");

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
        var oldUrl = _currentUrl;

        if (IsLocalUrl(normalized))
        {
            _preferredMode = "Local";
            _currentUrl = normalized;
            await PersistPreferredModeAsync("Local").ConfigureAwait(false);
        }
        else
        {
            _remoteUrl = normalized;
            _preferredMode = "Remote";
            _currentUrl = normalized;
            await PersistRemoteUrlAsync(normalized).ConfigureAwait(false);
            await PersistPreferredModeAsync("Remote").ConfigureAwait(false);
        }

        _logger.LogInformation("[CONNECTION-CFG] URL changed: {OldUrl} -> {NewUrl}", oldUrl, _currentUrl);

        if (oldUrl != _currentUrl)
        {
            UrlChanged?.Invoke(this, _currentUrl);
        }
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
        if (_preferredMode == "Remote" && !string.IsNullOrEmpty(_remoteUrl))
        {
            _currentUrl = _remoteUrl;
        }
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
        if (mode == "Remote" && !string.IsNullOrEmpty(_remoteUrl))
        {
            _currentUrl = _remoteUrl;
        }
        else
        {
            _currentUrl = LocalUrlConstant;
        }
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
    /// 在用户设置文件顶层节下持久化单个键。文件/目录缺失时创建，
    /// 节中其他键保持不变。
    /// </summary>
    private async Task PersistSettingAsync(string section, string key, string value)
    {
        try
        {
            var root = await LoadOrCreateRootAsync();

            if (root[section] is JsonObject sectionObj)
            {
                sectionObj[key] = value;
            }
            else
            {
                root[section] = new JsonObject { [key] = value };
            }

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(_settingsFilePath, root.ToJsonString(options));

            _logger.LogDebug(
                "[CONNECTION-CFG] Persisted {Section}:{Key} = {Value} to {Path}",
                section, key, value, _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[CONNECTION-CFG] Failed to persist {Section}:{Key} to {Path}",
                section, key, _settingsFilePath);
        }
    }

    /// <summary>
    /// 读取用户设置文件（JSON 对象根），文件缺失/损坏时返回空根并创建目录。
    /// </summary>
    private async Task<JsonObject> LoadOrCreateRootAsync()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var text = await File.ReadAllTextAsync(_settingsFilePath);
                var root = JsonNode.Parse(text)?.AsObject();
                if (root is not null) return root;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CONNECTION-CFG] Failed to parse {Path}, starting fresh", _settingsFilePath);
            }
        }

        var dir = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return new JsonObject();
    }

    /// <summary>
    /// 读取用户设置文件中的 ApiClient 节。键缺失返回 null，显式空字符串原样返回。
    /// 任何读取失败仅记录日志并返回全 null（不阻塞启动）。
    /// </summary>
    private (string? BaseUrl, string? RemoteUrl, string? PreferredMode) TryLoadUserSettings()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                return (null, null, null);

            var json = File.ReadAllText(_settingsFilePath);
            var root = JsonNode.Parse(json)?.AsObject();
            if (root is null
                || !root.TryGetPropertyValue("ApiClient", out var sectionNode)
                || sectionNode is not JsonObject sectionObj)
            {
                return (null, null, null);
            }

            return (
                JsonValueToString(sectionObj["BaseUrl"]),
                JsonValueToString(sectionObj["RemoteUrl"]),
                JsonValueToString(sectionObj["PreferredMode"]));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CONNECTION-CFG] Failed to load user settings from {Path}", _settingsFilePath);
            return (null, null, null);
        }
    }

    private static string? JsonValueToString(JsonNode? node)
        => node?.GetValue<string>();

    private static bool IsLocalUrl(string url)
    {
        // LocalWebAPI runs on port 5300. Any localhost/127.0.0.1 URL on a
        // different port (e.g., 5000) is a remote WebAPI running locally.
        return url.Contains("localhost:5300", StringComparison.OrdinalIgnoreCase)
            || url.Contains("127.0.0.1:5300", StringComparison.OrdinalIgnoreCase);
    }
}
