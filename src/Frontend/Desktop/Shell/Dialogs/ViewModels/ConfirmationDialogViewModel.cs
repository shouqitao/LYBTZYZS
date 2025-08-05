using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 确认对话框视图模型
    /// </summary>
    public class ConfirmationDialogViewModel : BindableBase // Temporarily remove IDialogAware due to Prism 9 compatibility issues
    {
        private string _title = "确认";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _message = "";
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public DelegateCommand YesCommand { get; }
        public DelegateCommand NoCommand { get; }

        public event Action<IDialogResult>? RequestClose;

        public ConfirmationDialogViewModel()
        {
            YesCommand = new DelegateCommand(OnYes);
            NoCommand = new DelegateCommand(OnNo);
        }

        private void OnYes()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Yes));
        }

        private void OnNo()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.No));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("title"))
            {
                Title = parameters.GetValue<string>("title");
            }

            if (parameters.ContainsKey("message"))
            {
                Message = parameters.GetValue<string>("message");
            }
        }
    }
}