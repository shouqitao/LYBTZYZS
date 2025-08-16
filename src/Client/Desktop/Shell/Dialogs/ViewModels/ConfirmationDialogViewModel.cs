using LYBT.Shared.Models.Contracts.Common;
using System;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
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

        // Simplified dialog implementation without Prism.Dialogs dependency
        public event Action? RequestClose;
        public bool? DialogResult { get; private set; }

        public ConfirmationDialogViewModel()
        {
            YesCommand = new DelegateCommand(OnYes);
            NoCommand = new DelegateCommand(OnNo);
        }

        private void OnYes()
        {
            DialogResult = true;
            RequestClose?.Invoke();
        }

        private void OnNo()
        {
            DialogResult = false;
            RequestClose?.Invoke();
        }
    }
}