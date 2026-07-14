using MaterialDesignThemes.Wpf;
using Prism.Ioc;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Services;

public class DialogHostService : IDialogHostService
{
    private const string RootDialog = "RootDialog";
    private readonly IDialogService _dialogService;

    public DialogHostService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        // 使用Prism IDialogService而不是ContainerLocator
        var parameters = new DialogParameters
        {
            { "Message", message },
            { "Title", title }
        };

        IDialogResult? dialogResult = null;
        _dialogService.ShowDialog("ConfirmationDialog", parameters, result => dialogResult = result);
        return Task.FromResult(dialogResult?.Result == ButtonResult.OK);
    }

    public async Task<T?> ShowCustomDialogAsync<T>(object dialogContent) where T : class
    {
        var result = await DialogHost.Show(dialogContent, RootDialog);
        return result as T;
    }
}
