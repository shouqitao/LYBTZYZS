using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 对话框辅助方法 — 从 MainWindowViewModel 提取
/// 统一走 IDialogManager（Prism MDIX Dialog）+ IToastService，禁止 MessageBox 双轨
/// </summary>
public class ShellDialogHelper
{
    private readonly IDialogManager? _dialogManager;
    private readonly IToastService? _toastService;
    private readonly ILogger _logger;

    public ShellDialogHelper(
        IDialogManager? dialogManager,
        IToastService? toastService,
        ILogger<ShellDialogHelper> logger)
    {
        _dialogManager = dialogManager;
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
        if (_dialogManager != null)
            await _dialogManager.ShowWarningAsync(message, "警告");
        else
            _logger.LogWarning("IDialogManager 不可用: {Message}", message);
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        if (_dialogManager != null)
            return await _dialogManager.ShowConfirmAsync(message, title);
        _logger.LogWarning("IDialogManager 不可用: {Message}", message);
        return false;
    }
}
