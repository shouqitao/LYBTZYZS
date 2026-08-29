using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Services.FeatureToggle;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 配置中心面板 ViewModel（SHELL-018 Phase 2: 5 组可编辑 + 读卡器只读占位 + 系统信息只读）
/// 保存：ClientConfigurationStore 节级原子写 + Reload；功能开关/诊所信息即时生效（决策 A），其余重启生效提示。
/// </summary>
public partial class ConfigurationCenterViewModel : NavigableViewModelBase
{
    private readonly IClientConfigurationStore _store;
    private readonly IFeatureToggleService _featureToggles;
    private readonly IConnectionModeService _connectionMode;
    private readonly IOptions<ClinicSettingsOptions> _clinicOptions;
    private readonly IOptions<ClientSessionOptions> _sessionOptions;
    private readonly IOptions<ApiClientOptions> _apiOptions;
    private readonly IOptions<CardReaderOptions> _cardReaderOptions;
    private readonly IOptions<OfflineModeOptions> _offlineOptions;
    private readonly IOptions<DefaultPasswordOptions>? _defaultPasswordOptions;

    // ── 组 1：诊所信息（热更新） ──
    [ObservableProperty] private string _clinicName = string.Empty;
    [ObservableProperty] private string _clinicAddress = string.Empty;
    [ObservableProperty] private string _clinicPhone = string.Empty;
    [ObservableProperty] private string _clinicDepartment = string.Empty;
    [ObservableProperty] private string _clinicLicenseNumber = string.Empty;
    [ObservableProperty] private string _clinicEmail = string.Empty;

    // ── 组 2：会话设置（重启生效） ──
    [ObservableProperty] private int _inactivityTimeoutMinutes = 30;
    [ObservableProperty] private int _warningBeforeTimeoutMinutes;
    [ObservableProperty] private int _activityCheckIntervalSeconds = 30;

    // ── 组 3：连接设置（重启生效） ──
    [ObservableProperty] private string _apiBaseUrl = string.Empty;
    [ObservableProperty] private int _apiTimeoutSeconds = 60;

    // ── 组 4：安全策略（重启生效） ──
    [ObservableProperty] private bool _forceChangeOnFirstLogin = true;
    /// <summary>新用户默认密码（PasswordBox 经 BoundPassword 双向绑定，P1-10 修复非 INPC 参数）。</summary>
    [ObservableProperty] private string _newUserPassword = string.Empty;

    // ── 组 5：功能开关（热更新即时生效） ──
    [ObservableProperty] private bool _overwriteConflicts = true;
    [ObservableProperty] private string _duplicateHerbMergeStrategy = "Max";

    // ── 读卡器（只读占位，SHELL-019 诊断） ──
    [ObservableProperty] private string _cardReaderStatus = string.Empty;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSaving;

    /// <summary>本地模式（决策 B: 本地配置生效 = 重启内嵌 LocalWebAPI）</summary>
    [ObservableProperty] private bool _isLocalMode;

    /// <summary>重复药材合并策略取值（功能开关组）</summary>
    public static string[] MergeStrategies { get; } = { "Skip", "Update", "Error", "Max" };

    public ConfigurationCenterViewModel(
        IViewModelServices services,
        IClientConfigurationStore store,
        IFeatureToggleService featureToggles,
        IConnectionModeService connectionMode,
        IOptions<ClinicSettingsOptions> clinicOptions,
        IOptions<ClientSessionOptions> sessionOptions,
        IOptions<ApiClientOptions> apiOptions,
        IOptions<CardReaderOptions> cardReaderOptions,
        IOptions<OfflineModeOptions> offlineOptions,
        IOptions<DefaultPasswordOptions>? defaultPasswordOptions = null)
        : base(services)
    {
        _store = store;
        _featureToggles = featureToggles;
        _connectionMode = connectionMode;
        _clinicOptions = clinicOptions;
        _sessionOptions = sessionOptions;
        _apiOptions = apiOptions;
        _cardReaderOptions = cardReaderOptions;
        _offlineOptions = offlineOptions;
        _defaultPasswordOptions = defaultPasswordOptions;
        LoadFromOptions();
    }

    private void LoadFromOptions()
    {
        var clinic = _clinicOptions.Value ?? new ClinicSettingsOptions();
        ClinicName = clinic.Name;
        ClinicAddress = clinic.Address;
        ClinicPhone = clinic.Phone;
        ClinicDepartment = clinic.Department;
        ClinicLicenseNumber = clinic.LicenseNumber;
        ClinicEmail = clinic.Email;

        var session = _sessionOptions.Value ?? new ClientSessionOptions();
        InactivityTimeoutMinutes = session.InactivityTimeoutMinutes;
        WarningBeforeTimeoutMinutes = session.WarningBeforeTimeoutMinutes;
        ActivityCheckIntervalSeconds = session.ActivityCheckIntervalSeconds;

        var api = _apiOptions.Value ?? new ApiClientOptions();
        ApiBaseUrl = string.IsNullOrEmpty(api.RemoteUrl) ? api.BaseUrl : api.RemoteUrl;
        ApiTimeoutSeconds = api.TimeoutSeconds;

        // P1-A：从已注册配置读取，而非硬编码（用户保存 false 后重开页面不再回弹 true）
        ForceChangeOnFirstLogin = _defaultPasswordOptions?.Value.ForceChangeOnFirstLogin ?? true;
        OverwriteConflicts = _featureToggles.IsEnabled("OverwriteConflicts");
        DuplicateHerbMergeStrategy = _featureToggles.GetValue("DuplicateHerbMergeStrategy") ?? "Max";

        var card = _cardReaderOptions.Value ?? new CardReaderOptions();
        CardReaderStatus = $"UsbPort={card.UsbPort} · ConnectTimeout={card.ConnectTimeout}ms · ReadTimeout={card.ReadTimeout}ms（完整诊断归 US-SHELL-019）";

        IsLocalMode = _connectionMode.IsLocal;
    }

    /// <summary>
    /// 重启本地内嵌服务（SHELL-018 Phase 3 决策 B: 本地配置生效 = 重启 LocalWebAPI——Desktop 会话不丢）
    /// </summary>
    [RelayCommand]
    private async Task RestartLocalServiceAsync()
    {
        var confirm = System.Windows.MessageBox.Show(
            "将重启本地内嵌服务（30 秒后生效，本地模式短暂不可用）。确认继续？",
            "确认重启本地服务",
            System.Windows.MessageBoxButton.OKCancel,
            System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.OK)
            return;

        try
        {
            // 本地 API 地址（OfflineMode:LocalApiBaseUrl——本地模式服务端面板语义）
            var config = _offlineOptions.Value?.LocalApiBaseUrl ?? "http://localhost:5300";
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await client.PostAsync($"{config.TrimEnd('/')}/api/v1/configuration/restart", null);
            StatusMessage = response.IsSuccessStatusCode
                ? "本地服务重启已调度（30 秒后生效，内嵌服务自动拉起）"
                : $"重启请求失败（HTTP {(int)response.StatusCode}）——需系统管理员权限";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CFG-CENTER] 本地服务重启失败");
            StatusMessage = "重启请求失败，请确认本地服务运行中";
        }
    }

    // ── 保存命令 ──

    [RelayCommand]
    private async Task SaveClinicAsync()
    {
        await SaveAsync("ClinicSettings", new Dictionary<string, object>
        {
            ["Name"] = ClinicName,
            ["Address"] = ClinicAddress,
            ["Phone"] = ClinicPhone,
            ["Department"] = ClinicDepartment,
            ["LicenseNumber"] = ClinicLicenseNumber,
            ["Email"] = ClinicEmail
        }, hotReload: true);
    }

    [RelayCommand]
    private async Task SaveSessionAsync()
    {
        if (InactivityTimeoutMinutes is < 1 or > 120)
        {
            StatusMessage = "会话超时需在 1-120 分钟之间";
            return;
        }
        if (WarningBeforeTimeoutMinutes is < 0 or > 10 || WarningBeforeTimeoutMinutes > InactivityTimeoutMinutes)
        {
            StatusMessage = "提前提醒需在 0-10 分钟且不超过会话超时";
            return;
        }
        if (ActivityCheckIntervalSeconds is < 10 or > 120)
        {
            StatusMessage = "活动检测间隔需在 10-120 秒之间";
            return;
        }

        await SaveAsync("ClientSession", new Dictionary<string, object>
        {
            ["InactivityTimeoutMinutes"] = InactivityTimeoutMinutes,
            ["WarningBeforeTimeoutMinutes"] = WarningBeforeTimeoutMinutes,
            ["ActivityCheckIntervalSeconds"] = ActivityCheckIntervalSeconds
        }, hotReload: false);
    }

    [RelayCommand]
    private async Task SaveConnectionAsync()
    {
        if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            StatusMessage = "API 地址需为合法的 http/https URL";
            return;
        }
        if (ApiTimeoutSeconds is < 5 or > 300)
        {
            StatusMessage = "超时时间需在 5-300 秒之间";
            return;
        }

        await SaveAsync("ApiClient", new Dictionary<string, object>
        {
            ["BaseUrl"] = ApiBaseUrl,
            ["RemoteUrl"] = ApiBaseUrl,
            ["TimeoutSeconds"] = ApiTimeoutSeconds
        }, hotReload: false, note: "连接设置需重启生效（决策 C：仅提示，不做模式联动）");
    }

    [RelayCommand]
    private async Task SaveSecurityAsync(object? password)
    {
        var values = new Dictionary<string, object>
        {
            ["ForceChangeOnFirstLogin"] = ForceChangeOnFirstLogin
        };

        var pwd = password as string;
        if (!string.IsNullOrWhiteSpace(pwd))
        {
            if (pwd.Length < 8)
            {
                StatusMessage = "新用户默认密码至少 8 位";
                return;
            }
            values["NewUserPassword"] = pwd;
        }

        await SaveAsync("DefaultPasswords", values, hotReload: false);
    }

    [RelayCommand]
    private async Task SaveFeatureTogglesAsync()
    {
        var validStrategies = new[] { "Skip", "Update", "Error", "Max" };
        if (!validStrategies.Contains(DuplicateHerbMergeStrategy, StringComparer.OrdinalIgnoreCase))
        {
            StatusMessage = $"重复药材合并策略需为：{string.Join(" / ", validStrategies)}";
            return;
        }

        var ok = await _store.SaveSectionAsync("FeatureToggles", new Dictionary<string, object>
        {
            ["OverwriteConflicts"] = OverwriteConflicts,
            ["DuplicateHerbMergeStrategy"] = DuplicateHerbMergeStrategy
        });
        if (ok)
        {
            StatusMessage = "功能开关已保存并即时生效（热更新）";
            // reloadOnChange 已配置——文件变更自动触发 IFeatureToggleService.TogglesChanged
        }
        else
        {
            StatusMessage = "功能开关保存失败，请检查文件权限";
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        StatusMessage = "正在测试远程连接...";
        var ok = await _connectionMode.TestRemoteConnectionAsync(ApiBaseUrl);
        StatusMessage = ok ? "远程连接测试成功" : "远程连接测试失败（无法访问健康端点）";
    }

    private async Task SaveAsync(string section, Dictionary<string, object> values, bool hotReload, string? note = null)
    {
        if (IsSaving) return;
        IsSaving = true;
        try
        {
            var ok = await _store.SaveSectionAsync(section, values);
            StatusMessage = ok
                ? (hotReload ? "已保存并即时生效（热更新）" : "已保存，重启后生效")
                : "保存失败，请检查配置文件权限";
            if (!string.IsNullOrEmpty(note) && ok)
                StatusMessage = $"{StatusMessage}（{note}）";
        }
        catch (Exception ex)
        {
            // P1-A：保存异常不能逃逸（否则配置面板保存即崩溃）
            Logger.LogError(ex, "[CFG-CENTER] 保存配置节失败: {Section}", section);
            StatusMessage = "保存失败，请检查配置文件权限或磁盘状态";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
