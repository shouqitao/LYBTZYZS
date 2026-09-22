using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Auth.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Auth.ViewModels;

/// <summary>
/// 连接测试对话框基类（A-31-C5-4 收敛）
/// ServerConfigViewModel / InitializationWizardViewModel 的 TestConnectionAsync 状态机 ~95% 同构，
/// 统一 RemoteUrl/TestStatus/TestStatusMessage/IsNotTesting + TestConnection 命令。
/// 子类差异：FirstRun 额外维护 IsRemoteAvailable（OnTestCompleted 覆写联动）。
/// </summary>
public abstract partial class ConnectionTestViewModelBase : DialogViewModelBase
{
    protected readonly IConnectionModeService _connectionModeService;
    protected readonly IConnectionSettingsService _connectionSettingsService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _remoteUrl = string.Empty;

    [ObservableProperty]
    private ConnectionTestStatus _testStatus = ConnectionTestStatus.Idle;

    [ObservableProperty]
    private string _testStatusMessage = "尚未测试";

    /// <summary>测试中时禁用测试按钮</summary>
    public bool IsNotTesting => TestStatus != ConnectionTestStatus.Testing;

    protected ConnectionTestViewModelBase(
        IViewModelServices services,
        IConnectionModeService connectionModeService,
        IConnectionSettingsService connectionSettingsService)
        : base(services)
    {
        _connectionModeService = connectionModeService ?? throw new ArgumentNullException(nameof(connectionModeService));
        _connectionSettingsService = connectionSettingsService ?? throw new ArgumentNullException(nameof(connectionSettingsService));
    }

    partial void OnRemoteUrlChanged(string value)
    {
        OnRemoteUrlChangedCore(value);
    }

    /// <summary>RemoteUrl 变更后的子类扩展点（如 ServerConfig 刷新 SaveOnlyCommand）</summary>
    protected virtual void OnRemoteUrlChangedCore(string value) { }

    partial void OnTestStatusChanged(ConnectionTestStatus value)
    {
        OnPropertyChanged(nameof(IsNotTesting));
        TestConnectionCommand.NotifyCanExecuteChanged();
        ConfirmCommand.NotifyCanExecuteChanged();
        OnTestStatusChangedCore();
    }

    /// <summary>TestStatus 变更后的子类扩展点（如 FirstRun 刷新 ShouldShowFallbackHint）</summary>
    protected virtual void OnTestStatusChangedCore() { }

    /// <summary>测试完成回调（子类联动额外状态，如 FirstRun 的 IsRemoteAvailable）</summary>
    protected virtual void OnTestCompleted(bool ok) { }

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
            OnTestCompleted(false);
            return;
        }

        if (!_connectionSettingsService.IsValidUrl(RemoteUrl))
        {
            TestStatus = ConnectionTestStatus.Failed;
            TestStatusMessage = "地址格式无效（需以 http:// 或 https:// 开头）";
            OnTestCompleted(false);
            return;
        }

        try
        {
            TestStatus = ConnectionTestStatus.Testing;
            TestStatusMessage = "正在测试连接...";

            var ok = await _connectionModeService.TestRemoteConnectionAsync(RemoteUrl);

            TestStatus = ok ? ConnectionTestStatus.Success : ConnectionTestStatus.Failed;
            TestStatusMessage = ok ? "✓ 可用" : "✗ 不可用";
            OnTestCompleted(ok);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CONNECTION-TEST] 测试连接异常");
            TestStatus = ConnectionTestStatus.Failed;
            TestStatusMessage = "✗ 不可用: 无法连接到服务器";
            OnTestCompleted(false);
        }
    }

    private bool CanTestConnection() => TestStatus != ConnectionTestStatus.Testing;
}
