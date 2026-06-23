using MaterialDesignThemes.Wpf;
using Prism.Ioc;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Services;

public partial class DialogHostService : IDialogHostService
{
    private const string RootDialog = "RootDialog";

    public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        // C2 fix: Resolve the existing ConfirmationDialogViewModel from DI instead of
        // a hand-rolled DataContext. C4: the VM exposes every property the XAML binds.
        var vm = ContainerLocator.Container.Resolve<Dialogs.ViewModels.ConfirmationDialogViewModel>();
        vm.Message = message;
        vm.Title = title;

        var view = new Dialogs.Views.ConfirmationDialog
        {
            DataContext = vm
        };

        // Bridge Prism IDialogAware.RequestClose -> MaterialDesign DialogHost.CloseDialogCommand.
        // The VM's Confirm/Cancel commands raise RequestClose (Prism dialog semantics), but we are
        // hosting inside a MaterialDesign DialogHost which closes via CloseDialogCommand routed event.
        vm.RequestClose += dialogResult =>
        {
            var confirmed = dialogResult.Result == ButtonResult.OK;
            DialogHost.CloseDialogCommand.Execute(confirmed, view);
        };

        // C3 fix: use the identifier string directly, no FindName/FindDialogHost helper.
        var result = await DialogHost.Show(view, RootDialog);
        return result is true;
    }

    public async Task<T?> ShowCustomDialogAsync<T>(object dialogContent) where T : class
    {
        var result = await DialogHost.Show(dialogContent, RootDialog);
        return result as T;
    }
}
