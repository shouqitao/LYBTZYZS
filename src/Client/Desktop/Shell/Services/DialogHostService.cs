using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace LYBT.Desktop.Shell.Services;

public partial class DialogHostService : IDialogHostService
{
    private const string RootDialog = "RootDialog";

    public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        var vm = new ConfirmationDialogDataContext(message, title);
        var view = new Dialogs.Views.ConfirmationDialog
        {
            DataContext = vm
        };
        var result = await DialogHost.Show(view, RootDialog);
        return result is true;
    }

    public async Task<T?> ShowCustomDialogAsync<T>(object dialogContent) where T : class
    {
        var result = await DialogHost.Show(dialogContent, RootDialog);
        return result as T;
    }

    private sealed partial class ConfirmationDialogDataContext : ObservableObject
    {
        [ObservableProperty]
        private string _message;

        [ObservableProperty]
        private string _title;

        public string ConfirmButtonText => "确认";
        public string CancelButtonText => "取消";
        public string IconSource => "/Assets/Icons/warning.png";

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public ConfirmationDialogDataContext(string message, string title)
        {
            _message = message;
            _title = title;
            ConfirmCommand = new RelayCommand(OnConfirm);
            CancelCommand = new RelayCommand(OnCancel);
        }

        private static void OnConfirm()
        {
            DialogHost.CloseDialogCommand.Execute(true, FindDialogHost());
        }

        private static void OnCancel()
        {
            DialogHost.CloseDialogCommand.Execute(false, FindDialogHost());
        }

        private static DialogHost? FindDialogHost()
        {
            var mainWindow = Application.Current.MainWindow;
            return mainWindow?.FindName(RootDialog) as DialogHost;
        }
    }
}
