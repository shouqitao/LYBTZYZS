using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 对话框辅助方法 — 从 MainWindowViewModel 提取
/// </summary>
public class ShellDialogHelper
{
    private readonly ICommonDialogService? _dialogService;
    private readonly IToastService? _toastService;
    private readonly ILogger _logger;

    public ShellDialogHelper(
        ICommonDialogService? dialogService,
        IToastService? toastService,
        ILogger<ShellDialogHelper> logger)
    {
        _dialogService = dialogService;
        _toastService = toastService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ShowSuccessMessageAsync(string message)
    {
        if (_toastService != null)
            await Task.Run(() => _toastService.ShowSuccess(message));
        else
            _logger.LogWarning("ToastService 不可用: {Message}", message);
    }

    public async Task ShowErrorMessageAsync(string message)
    {
        if (_toastService != null)
            await Task.Run(() => _toastService.ShowError(message));
        else
            _logger.LogError("ToastService 不可用: {Message}", message);
    }

    public async Task ShowWarningMessageAsync(string message)
    {
        if (_dialogService != null)
            await _dialogService.ShowWarningAsync(message, "警告");
        else
            _logger.LogWarning("CommonDialogService 不可用: {Message}", message);
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        if (_dialogService != null)
            return await _dialogService.ShowConfirmAsync(message, title);
        _logger.LogWarning("CommonDialogService 不可用: {Message}", message);
        return false;
    }
}
