namespace LYBT.Desktop.Shell.Services;

public interface IDialogHostService
{
    Task<bool> ShowConfirmationAsync(string message, string title = "确认");

    Task<T?> ShowCustomDialogAsync<T>(object dialogContent) where T : class;
}
