using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Auth.ViewModels;

/// <summary>
/// 首次运行配置向导 ViewModel - 单屏欢迎对话框，引导用户配置远程服务器或回退到本地模式
/// </summary>
public partial class FirstRunSetupViewModel : DialogViewModelBase
{
    private readonly IConnectionModeService _connectionModeService;
    private readonly IConnectionSettingsService _connectionSettingsService;

    /// <summary>
    /// 连接测试状态 (UI 显示用枚举)
    /// </summary>
    public enum ConnectionTestStatus
    {
        Idle,
        Testing,
        Success,
        Failed
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _remoteUrl = string.Empty;

    [ObservableProperty]
    private ConnectionTestStatus _testStatus = ConnectionTestStatus.Idle;

    [ObservableProperty]
    private string _testStatusMessage = "尚未测试";

    /// <summary>远程服务器最近一次测试是否成功 - 用于驱动"将使用本地模式"提示</summary>
    [ObservableProperty]
    private bool _isRemoteAvailable;

    /// <summary>测试中时禁用测试按钮</summary>
    public bool IsNotTesting => TestStatus != ConnectionTestStatus.Testing;

    /// <summary>已测试且失败 - 显示回退提示</summary>
    public bool ShouldShowFallbackHint => TestStatus == ConnectionTestStatus.Failed && !IsRemoteAvailable;

    public FirstRunSetupViewModel(
        IViewModelServices services,
        IConnectionModeService connectionModeService,
        IConnectionSettingsService connectionSettingsService)
        : base(services)
    {
        _connectionModeService = connectionModeService ?? throw new ArgumentNullException(nameof(connectionModeService));
        _connectionSettingsService = connectionSettingsService ?? throw new ArgumentNullException(nameof(connectionSettingsService));
        Title = "首次运行配置";
    }

    protected override void OnDialogOpenedCore(IDialogParameters? parameters)
    {
        // 预填一个常见的占位地址，引导用户输入
        RemoteUrl = string.Empty;
        TestStatus = ConnectionTestStatus.Idle;
        TestStatusMessage = "尚未测试";
        IsRemoteAvailable = false;
    }

    partial void OnTestStatusChanged(ConnectionTestStatus value)
    {
        OnPropertyChanged(nameof(IsNotTesting));
        OnPropertyChanged(nameof(ShouldShowFallbackHint));
        TestConnectionCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsRemoteAvailableChanged(bool value)
    {
        OnPropertyChanged(nameof(ShouldShowFallbackHint));
    }

    /// <summary>
    /// 测试远程连接 - 调用 IConnectionModeService.TestRemoteConnectionAsync
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanTestConnection))]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(RemoteUrl))
        {
            TestStatus = ConnectionTestStatus.Failed;
            TestStatusMessage = "请先输入服务器地址";
            IsRemoteAvailable = false;
            return;
        }

        if (!_connectionSettingsService.IsValidUrl(RemoteUrl))
        {
            TestStatus = ConnectionTestStatus.Failed;
            TestStatusMessage = "地址格式无效（需以 http:// 或 https:// 开头）";
            IsRemoteAvailable = false;
            return;
        }

        try
        {
            TestStatus = ConnectionTestStatus.Testing;
            TestStatusMessage = "正在测试连接...";

            var ok = await _connectionModeService.TestRemoteConnectionAsync(RemoteUrl);

            IsRemoteAvailable = ok;
            TestStatus = ok ? ConnectionTestStatus.Success : ConnectionTestStatus.Failed;
            TestStatusMessage = ok ? "✓ 可用" : "✗ 不可用";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[FIRST-RUN] 测试连接异常");
            IsRemoteAvailable = false;
            TestStatus = ConnectionTestStatus.Failed;
            TestStatusMessage = $"✗ 不可用: {ex.Message}";
        }
    }

    private bool CanTestConnection() => TestStatus != ConnectionTestStatus.Testing;

    /// <summary>
    /// "完成" - 保存远程 URL 并切换到远程模式。仅在校验通过时执行
    /// </summary>
    protected override bool CanConfirm() =>
        !string.IsNullOrWhiteSpace(RemoteUrl) && !IsLoading && !IsBusy;

    protected override void Confirm()
    {
        if (string.IsNullOrWhiteSpace(RemoteUrl))
        {
            return;
        }

        if (!_connectionSettingsService.IsValidUrl(RemoteUrl))
        {
            TestStatusMessage = "地址格式无效（需以 http:// 或 https:// 开头）";
            return;
        }

        SaveRemoteAsync().SafeFireAndForget(ex => Logger.LogError(ex, "[FIRST-RUN] 保存远程配置失败"));
    }

    /// <summary>
    /// "跳过，使用本地模式" - 直接切换到本地模式并关闭对话框
    /// </summary>
    [RelayCommand]
    private void UseLocalMode()
    {
        try
        {
            _connectionModeService.SetMode(ConnectionMode.Local);
            Logger.LogInformation("[FIRST-RUN] 用户选择跳过，使用本地模式");
            CloseDialog(ButtonResult.OK);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[FIRST-RUN] 切换本地模式失败");
            TestStatusMessage = $"切换本地模式失败: {ex.Message}";
        }
    }

    private async Task SaveRemoteAsync()
    {
        try
        {
            SetBusy(true, "正在保存...");
            await _connectionSettingsService.SetUrlAsync(RemoteUrl);
            // URL 变更会通过 UrlChanged 事件回流到 ConnectionModeService 自动切换模式；
            // 显式 SetMode(Remote) 保证即便 URL 仍被判为本地时也明确选择远程。
            _connectionModeService.SetMode(ConnectionMode.Remote);
            Logger.LogInformation("[FIRST-RUN] 已保存远程服务器地址: {Url}", RemoteUrl);
            CloseDialog(ButtonResult.OK);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[FIRST-RUN] 保存远程配置失败");
            await Services.UiThreadDispatcher.InvokeAsync(() =>
            {
                TestStatusMessage = $"保存失败: {ex.Message}";
            });
        }
        finally
        {
            SetBusy(false);
        }
    }
}
