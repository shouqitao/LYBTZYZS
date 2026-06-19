using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Auth.ViewModels;

/// <summary>
/// 服务器配置对话框 ViewModel - 管理远程 WebAPI 连接 URL 与连通性测试
/// </summary>
public partial class ServerConfigViewModel : DialogViewModelBase
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
    [NotifyCanExecuteChangedFor(nameof(SaveOnlyCommand))]
    private string _remoteUrl = string.Empty;

    [ObservableProperty]
    private ConnectionTestStatus _testStatus = ConnectionTestStatus.Idle;

    [ObservableProperty]
    private string _testStatusMessage = "尚未测试";

    /// <summary>测试中时禁用测试按钮</summary>
    public bool IsNotTesting => TestStatus != ConnectionTestStatus.Testing;

    public ServerConfigViewModel(
        IViewModelServices services,
        IConnectionModeService connectionModeService,
        IConnectionSettingsService connectionSettingsService)
        : base(services)
    {
        _connectionModeService = connectionModeService ?? throw new ArgumentNullException(nameof(connectionModeService));
        _connectionSettingsService = connectionSettingsService ?? throw new ArgumentNullException(nameof(connectionSettingsService));
        Title = "服务器配置";
    }

    protected override void OnDialogOpenedCore(IDialogParameters? parameters)
    {
        try
        {
            RemoteUrl = _connectionSettingsService.RemoteUrl ?? string.Empty;
            TestStatus = ConnectionTestStatus.Idle;
            TestStatusMessage = "尚未测试";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[SERVER-CONFIG] 加载当前 URL 失败");
        }
    }

    partial void OnTestStatusChanged(ConnectionTestStatus value)
    {
        OnPropertyChanged(nameof(IsNotTesting));
        TestConnectionCommand.NotifyCanExecuteChanged();
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// IsLoading 变更时同步刷新 SaveOnlyCommand 的 CanExecute
    /// </summary>
    protected override void OnIsLoadingChangedCore(bool value)
    {
        base.OnIsLoadingChangedCore(value);
        SaveOnlyCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// IsBusy 变更时同步刷新 SaveOnlyCommand 的 CanExecute
    /// </summary>
    protected override void OnIsBusyChangedCore(bool value)
    {
        base.OnIsBusyChangedCore(value);
        SaveOnlyCommand.NotifyCanExecuteChanged();
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
            return;
        }

        if (!_connectionSettingsService.IsValidUrl(RemoteUrl))
        {
            TestStatus = ConnectionTestStatus.Failed;
            TestStatusMessage = "地址格式无效（需以 http:// 或 https:// 开头）";
            return;
        }

        try
        {
            TestStatus = ConnectionTestStatus.Testing;
            TestStatusMessage = "正在测试连接...";

            var ok = await _connectionModeService.TestRemoteConnectionAsync(RemoteUrl);

            TestStatus = ok ? ConnectionTestStatus.Success : ConnectionTestStatus.Failed;
            TestStatusMessage = ok ? "✓ 可用" : "✗ 不可用";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[SERVER-CONFIG] 测试连接异常");
            TestStatus = ConnectionTestStatus.Failed;
            TestStatusMessage = $"✗ 不可用: {ex.Message}";
        }
    }

    private bool CanTestConnection() => TestStatus != ConnectionTestStatus.Testing;

    /// <summary>
    /// "保存并启用" - 校验 URL 合法性 + 测试通过后，持久化 URL 并切换到远程模式
    /// </summary>
    protected override bool CanConfirm() =>
        !string.IsNullOrWhiteSpace(RemoteUrl)
        && TestStatus == ConnectionTestStatus.Success
        && !IsLoading
        && !IsBusy;

    protected override void Confirm()
    {
        if (string.IsNullOrWhiteSpace(RemoteUrl))
        {
            return;
        }

        SaveAndEnableAsync().SafeFireAndForget(ex => Logger.LogError(ex, "[SERVER-CONFIG] 保存并启用失败"));
    }

    private async Task SaveAndEnableAsync()
    {
        try
        {
            SetBusy(true, "正在保存并启用...");
            await _connectionSettingsService.SetUrlAsync(RemoteUrl);
            _connectionModeService.SetMode(ConnectionMode.Remote);
            Logger.LogInformation("[SERVER-CONFIG] 已保存并启用远程模式: {Url}", RemoteUrl);
            CloseDialog(ButtonResult.OK);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[SERVER-CONFIG] 保存并启用失败");
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

    /// <summary>
    /// "仅保存" - 仅持久化远程 URL，不切换当前模式 (用户可能仍处于本地模式)
    /// </summary>
    private bool CanSaveOnly() =>
        !string.IsNullOrWhiteSpace(RemoteUrl)
        && _connectionSettingsService.IsValidUrl(RemoteUrl)
        && !IsLoading
        && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSaveOnly))]
    private async Task SaveOnlyAsync()
    {
        try
        {
            SetBusy(true, "正在保存...");
            await _connectionSettingsService.SaveRemoteUrlAsync(RemoteUrl);
            Logger.LogInformation("[SERVER-CONFIG] 已保存远程地址 (未切换模式): {Url}", RemoteUrl);
            CloseDialog(ButtonResult.OK);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[SERVER-CONFIG] 仅保存失败");
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
