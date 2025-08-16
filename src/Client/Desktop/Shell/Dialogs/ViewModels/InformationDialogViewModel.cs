using LYBT.Shared.Models.Contracts.Common;
using System;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Desktop.Core.Enums;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 信息对话框视图模型
    /// </summary>
    public class InformationDialogViewModel : BindableBase // Temporarily remove IDialogAware due to Prism 9 compatibility issues
    {
        private string _title = "信息";
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

        private DialogType _dialogType = DialogType.Information;
        public DialogType DialogType
        {
            get => _dialogType;
            set => SetProperty(ref _dialogType, value);
        }

        public DelegateCommand OkCommand { get; }

        // Simplified dialog implementation without Prism.Dialogs dependency
        public event Action? RequestClose;
        public bool DialogResult { get; private set; } = true;

        public InformationDialogViewModel()
        {
            OkCommand = new DelegateCommand(OnOk);
        }

        private void OnOk()
        {
            DialogResult = true;
            RequestClose?.Invoke();
        }
    }
}