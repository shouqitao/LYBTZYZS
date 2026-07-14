using MaterialDesignThemes.Wpf;
using Prism.Ioc;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Services;

public class DialogHostService : IDialogHostService
{
    private readonly IDialogService _dialogService;

    public DialogHostService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        var tcs = new TaskCompletionSource<bool>();
        
        var parameters = new DialogParameters
        {
            { "Message", message },
            { "Title", title }
        };

        _dialogService.ShowDialog("ConfirmationDialog", parameters, result =>
        {
            tcs.SetResult(result.Result == ButtonResult.OK);
        });
        
        return tcs.Task;
    }

    public async Task<T?> ShowCustomDialogAsync<T>(object dialogContent) where T : class
    {
        var result = await DialogHost.Show(dialogContent, "RootDialog");
        return result as T;
    }
}
