namespace LYBT.Desktop.Shell.Services;

public interface ISnackbarService
{
    void ShowSuccess(string message, int durationMs = 3000);

    void ShowError(string message, int durationMs = 5000);

    void ShowInfo(string message, int durationMs = 3000);
}
