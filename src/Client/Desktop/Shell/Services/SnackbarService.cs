using MaterialDesignThemes.Wpf;

namespace LYBT.Desktop.Shell.Services;

public class SnackbarService : ISnackbarService
{
    private readonly ISnackbarMessageQueue _messageQueue;

    public SnackbarService(ISnackbarMessageQueue messageQueue)
    {
        _messageQueue = messageQueue;
    }

    public void ShowSuccess(string message, int durationMs = 3000) =>
        _messageQueue.Enqueue(message, "关闭", () => { }, true);

    public void ShowError(string message, int durationMs = 5000) =>
        _messageQueue.Enqueue(message, "关闭", () => { }, true);

    public void ShowInfo(string message, int durationMs = 3000) =>
        _messageQueue.Enqueue(message, "关闭", () => { }, true);
}
