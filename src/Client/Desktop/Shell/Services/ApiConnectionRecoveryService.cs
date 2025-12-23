using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Shell.Dialogs.ViewModels;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// API连接恢复服务实现
/// enhance-shell-connection-dialog: 负责处理API连接失败后的用户交互和恢复流程
/// </summary>
public class ApiConnectionRecoveryService : IApiConnectionRecoveryService
{
    private readonly IDialogService _dialogService;
    private readonly ILogger<ApiConnectionRecoveryService> _logger;

    public ApiConnectionRecoveryService(
        IDialogService dialogService,
        ILogger<ApiConnectionRecoveryService> logger)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RecoveryAction> ShowConnectionFailedDialogAsync(
        string errorMessage,
        Exception? exception = null,
        string? apiEndpoint = null)
    {
        _logger.LogInformation("显示API连接失败对话框，错误: {ErrorMessage}", errorMessage);

        var parameters = new DialogParameters
        {
            { "ErrorMessage", errorMessage }
        };

        if (exception != null)
        {
            parameters.Add("Exception", exception);
        }

        if (!string.IsNullOrEmpty(apiEndpoint))
        {
            parameters.Add("ApiEndpoint", apiEndpoint);
        }

        var result = RecoveryAction.Exit;
        var tcs = new TaskCompletionSource<RecoveryAction>();

        // 在UI线程上显示对话框
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _dialogService.ShowDialog(
                nameof(ApiConnectionFailedDialogViewModel).Replace("ViewModel", string.Empty),
                parameters,
                dialogResult =>
                {
                    if (dialogResult.Parameters.ContainsKey("RecoveryAction"))
                    {
                        result = dialogResult.Parameters.GetValue<RecoveryAction>("RecoveryAction");
                    }
                    else
                    {
                        // 根据ButtonResult判断
                        result = dialogResult.Result switch
                        {
                            ButtonResult.Retry => RecoveryAction.Retry,
                            ButtonResult.Yes => RecoveryAction.OfflineMode,
                            _ => RecoveryAction.Exit
                        };
                    }

                    _logger.LogInformation("用户选择的恢复操作: {RecoveryAction}", result);
                    tcs.SetResult(result);
                });
        });

        return await tcs.Task;
    }
}
