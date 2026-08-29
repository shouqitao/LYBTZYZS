using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.CardReader.Abstractions;
using LYBT.Desktop.Infrastructure.CardReader.Models;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 读卡器诊断面板 ViewModel（SHELL-019: 测试模式——厂家选择/探测/读卡/参数覆盖/持久化）
/// 使用模式（医生端 AutoDetect）不受影响——诊断仅创建独立读卡器实例。
/// </summary>
public partial class CardReaderDiagnosticsViewModel : NavigableViewModelBase
{
    private readonly ICardReaderDiagnostics _diagnostics;
    private readonly IClientConfigurationStore _store;
    private readonly IOptions<CardReaderOptions> _cardReaderOptions;

    [ObservableProperty]
    private ObservableCollection<CardReaderType> _readerTypes = [];

    [ObservableProperty]
    private CardReaderType _selectedReaderType = CardReaderType.HuaDaHD100;

    [ObservableProperty]
    private string _usbPort = "1001";

    [ObservableProperty]
    private string _connectTimeout = "5000";

    [ObservableProperty]
    private string _readTimeout = "10000";

    [ObservableProperty]
    private ObservableCollection<string> _reportLines = [];

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _lastTestPassed;

    public CardReaderDiagnosticsViewModel(
        IViewModelServices services,
        ICardReaderDiagnostics diagnostics,
        IClientConfigurationStore store,
        IOptions<CardReaderOptions> cardReaderOptions)
        : base(services)
    {
        _diagnostics = diagnostics;
        _store = store;
        _cardReaderOptions = cardReaderOptions;
        ReaderTypes = new ObservableCollection<CardReaderType>
        {
            CardReaderType.HuaDaHD100,
            CardReaderType.Auto
        };
        LoadFromOptions();
    }

    private void LoadFromOptions()
    {
        var options = _cardReaderOptions.Value;
        if (options is null) return;
        UsbPort = options.UsbPort.ToString();
        ConnectTimeout = options.ConnectTimeout.ToString();
        ReadTimeout = options.ReadTimeout.ToString();
    }

    /// <summary>
    /// 运行完整诊断（探测 → 握手 → 固件 → 读卡测试）
    /// </summary>
    [RelayCommand]
    private async Task RunDiagnosticsAsync()
    {
        if (IsRunning) return;

        if (!int.TryParse(UsbPort, out var port) || port <= 0)
        {
            StatusMessage = "USB 端口需为正整数";
            return;
        }
        if (!int.TryParse(ConnectTimeout, out var connectMs) || connectMs is < 1000 or > 30000)
        {
            StatusMessage = "连接超时需在 1000-30000 毫秒";
            return;
        }
        if (!int.TryParse(ReadTimeout, out var readMs) || readMs is < 1000 or > 60000)
        {
            StatusMessage = "读取超时需在 1000-60000 毫秒";
            return;
        }

        IsRunning = true;
        LastTestPassed = false;
        ReportLines = [];
        StatusMessage = "正在运行诊断（请将样卡放至感应区）...";
        try
        {
            var options = new CardReaderOptions
            {
                UsbPort = port,
                ConnectTimeout = connectMs,
                ReadTimeout = readMs
            };

            var report = await _diagnostics.RunDiagnosticsAsync(SelectedReaderType, options);
            ReportLines = new ObservableCollection<string>(report.Messages);
            LastTestPassed = report.Passed;
            StatusMessage = report.Passed
                ? "诊断通过——设备链路正常（可持久化厂家选择）"
                : "诊断未通过——请检查设备连接与参数后重试";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CARD-DIAG] 诊断运行失败");
            ReportLines.Add($"诊断异常: {ex.Message}");
            StatusMessage = "诊断失败，请检查读卡器驱动与连接";
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    /// 持久化厂家选择 + 手动参数到 appsettings（US-SHELL-019 AC: 测试通过后自动加载）
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveSettings))]
    private async Task SaveSettingsAsync()
    {
        // P2-A：非法数字输入不再崩溃（TryParse 失败提示）
        if (!int.TryParse(UsbPort, out var usbPort)
            || !int.TryParse(ConnectTimeout, out var connectTimeout)
            || !int.TryParse(ReadTimeout, out var readTimeout))
        {
            StatusMessage = "端口/超时需为有效数字";
            return;
        }

        var ok = await _store.SaveSectionAsync("CardReader", new Dictionary<string, object>
        {
            ["UsbPort"] = usbPort,
            ["ConnectTimeout"] = connectTimeout,
            ["ReadTimeout"] = readTimeout,
            ["ReaderType"] = SelectedReaderType.ToString()
        });
        StatusMessage = ok ? "读卡器配置已保存（医生端自动检测优先，此配置为默认参考）" : "保存失败，请检查文件权限";
    }

    private bool CanSaveSettings() => !IsRunning;

    partial void OnSelectedReaderTypeChanged(CardReaderType value)
    {
        if (value == CardReaderType.Auto)
            StatusMessage = "自动检测模式（当前适配厂家: 华大HD100）——推荐先选择具体厂家验证";
        else
            StatusMessage = string.Empty;
    }
}
