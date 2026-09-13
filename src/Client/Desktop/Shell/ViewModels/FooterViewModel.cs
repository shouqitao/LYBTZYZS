using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Shell.Services;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Windows.Media;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// FooterViewModel — 底部状态栏 (h32)
/// 按 desktop-layout-framework §底部状态栏：左组（API状态+连接模式 gap16）+ 右时间
/// Tick 订阅从 MainWindowViewModel 迁至此处
/// </summary>
public partial class FooterViewModel : ObservableObject, IDisposable
{
    private readonly IShellServices _shell;

    [ObservableProperty]
    private string _currentTimeDisplay = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public ApiHealthStatus ApiStatus => _shell.StatusBar.ApiStatus;
    public string ConnectionModeDisplay => _shell.StatusBar.ConnectionModeDisplay;
    public PackIconKind ApiStatusIcon => _shell.StatusBar.ApiStatusIcon;
    public Brush ApiStatusColor => _shell.StatusBar.ApiStatusColor;

    /// <summary>API 状态文本（随真实健康状态变化——避免 XAML 硬编码「已连接」）</summary>
    public string ApiStatusText => ApiStatus switch
    {
        ApiHealthStatus.Healthy => "API 已连接",
        ApiHealthStatus.Unhealthy => "API 未连接",
        _ => "API 检测中…",
    };

    public FooterViewModel(IShellServices shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        if (_shell.StatusBar is INotifyPropertyChanged inpc)
            inpc.PropertyChanged += OnStatusBarChanged;
        _shell.Tick.Tick += OnTick;
        UpdateTime();
    }

    private void OnStatusBarChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IStatusBarManager.ApiStatus) || e.PropertyName == nameof(IStatusBarManager.ConnectionModeDisplay))
        {
            OnPropertyChanged(nameof(ApiStatus));
            OnPropertyChanged(nameof(ApiStatusText));
            OnPropertyChanged(nameof(ConnectionModeDisplay));
            OnPropertyChanged(nameof(ApiStatusIcon));
            OnPropertyChanged(nameof(ApiStatusColor));
        }
    }

    private void OnTick(object? sender, ApplicationTickEventArgs e)
    {
        _shell.StatusBar.UpdateTime();
        UpdateTime();
    }

    private void UpdateTime()
    {
        CurrentTimeDisplay = _shell.StatusBar.CurrentTimeDisplay;
    }

    public void Dispose()
    {
        if (_shell.StatusBar is INotifyPropertyChanged inpc)
            inpc.PropertyChanged -= OnStatusBarChanged;
        _shell.Tick.Tick -= OnTick;
    }
}
