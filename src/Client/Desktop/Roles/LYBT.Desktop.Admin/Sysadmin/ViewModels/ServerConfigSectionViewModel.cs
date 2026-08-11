using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 服务端配置面板 ViewModel（SHELL-018 Phase 3: 远程模式——节列表/编辑/保存/重启）
/// 数据源：Configuration API（GET/PUT sections/{section} + POST restart，Phase 1 已实现）。
/// </summary>
public partial class ServerConfigSectionViewModel : NavigableViewModelBase
{
    /// <summary>服务端可编辑业务参数节（ADR-0014 PUT 白名单镜像；SystemAdmin:SessionTimeoutMinutes 单键）</summary>
    private static readonly string[] EditableSections =
    {
        "ClinicSettings", "FeatureToggles", "Session", "SystemAdmin", "MemoryCache", "Security"
    };

    private readonly IServerConfigurationService _serverConfig;
    private readonly ILogger<ServerConfigSectionViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ServerSectionItem> _sections = [];

    [ObservableProperty]
    private ServerSectionItem? _selectedSection;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    public ServerConfigSectionViewModel(
        IViewModelServices services,
        IServerConfigurationService serverConfig,
        ILogger<ServerConfigSectionViewModel> logger)
        : base(services)
    {
        _serverConfig = serverConfig;
        _logger = logger;
    }

    public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        _ = LoadSectionsAsync();
    }

    [RelayCommand]
    private async Task LoadSectionsAsync()
    {
        IsLoading = true;
        StatusMessage = "正在加载服务端配置...";
        try
        {
            Sections = new ObservableCollection<ServerSectionItem>();
            foreach (var section in EditableSections)
                Sections.Add(await CreateSectionItemAsync(section));
            SelectedSection = Sections.FirstOrDefault();
            StatusMessage = Sections.Count > 0 ? $"已加载 {Sections.Count} 个配置节" : "无可编辑配置节";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVR-CFG] 加载服务端配置失败");
            StatusMessage = "加载服务端配置失败（仅远程模式可用）";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<ServerSectionItem> CreateSectionItemAsync(string section)
    {
        var item = new ServerSectionItem { Section = section };
        try
        {
            var response = await _serverConfig.GetSectionAsync(section);
            if (response.Success && response.Data != null)
            {
                foreach (var kv in response.Data)
                    item.Values[kv.Key] = kv.Value;
                item.IsLoaded = true;
                item.IsReadOnly = (key, value) => value == "***";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SVR-CFG] 节 {Section} 加载失败（可能节不存在）", section);
            item.IsLoaded = false;
        }
        return item;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveSectionAsync()
    {
        if (SelectedSection is null) return;

        IsSaving = true;
        try
        {
            var editable = SelectedSection.Values
                .Where(kv => !SelectedSection.IsReadOnly(kv.Key, kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var response = await _serverConfig.UpdateSectionAsync(SelectedSection.Section, editable);
            if (response.Success && response.Data != null)
            {
                var mode = response.Data.EffectiveMode == "hot" ? "已即时生效（热更新）" : "已保存，重启后生效";
                StatusMessage = $"节「{SelectedSection.Section}」保存成功——{mode}";
                await LoadSectionsAsync(); // 刷新（脱敏值回显）
            }
            else
            {
                StatusMessage = $"保存失败：{response.Message ?? "服务器拒绝（白名单/敏感配置）"}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVR-CFG] 保存节失败: {Section}", SelectedSection.Section);
            StatusMessage = "保存失败，请检查网络连接";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSave() => SelectedSection is not null && !IsSaving && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task RestartServerAsync()
    {
        var confirm = System.Windows.MessageBox.Show(
            "将重启远程服务器（30 秒后生效，所有在线用户会短暂断连）。确认继续？",
            "确认重启服务",
            System.Windows.MessageBoxButton.OKCancel,
            System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.OK)
            return;

        try
        {
            var response = await _serverConfig.RestartAsync();
            StatusMessage = response.Success
                ? "重启已调度，30 秒后生效"
                : $"重启失败：{response.Message ?? "服务器拒绝（限频/权限）"}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVR-CFG] 重启失败");
            StatusMessage = "重启请求失败，请检查网络连接";
        }
    }

    partial void OnSelectedSectionChanged(ServerSectionItem? value)
    {
        SaveSectionCommand.NotifyCanExecuteChanged();
        RestartServerCommand.NotifyCanExecuteChanged();
    }
}

/// <summary>服务端配置节项（节名 + 键值字典 + 脱敏只读标记）</summary>
public partial class ServerSectionItem : ObservableObject
{
    public string Section { get; set; } = string.Empty;

    public bool IsLoaded { get; set; }

    public Func<string, string, bool> IsReadOnly { get; set; } = (_, _) => false;

    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string DisplayName => Section;
}
