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
            RemoteUrl = _connectionSettingsService.CurrentUrl ?? string.Empty;
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
    /// 保存并关闭 - 校验 URL 合法性后持久化到 IConnectionSettingsService
    /// </summary>
    protected override bool CanConfirm() =>
        !string.IsNullOrWhiteSpace(RemoteUrl) && !IsLoading && !IsBusy;

    protected override void Confirm()
    {
        if (string.IsNullOrWhiteSpace(RemoteUrl))
        {
            return;
        }

        SaveAsync().SafeFireAndForget(ex => Logger.LogError(ex, "[SERVER-CONFIG] 保存配置失败"));
    }

    private async Task SaveAsync()
    {
        try
        {
            SetBusy(true, "正在保存...");
            await _connectionSettingsService.SetUrlAsync(RemoteUrl);
            Logger.LogInformation("[SERVER-CONFIG] 已保存服务器地址: {Url}", RemoteUrl);
            CloseDialog(ButtonResult.OK);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[SERVER-CONFIG] 保存配置失败");
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
