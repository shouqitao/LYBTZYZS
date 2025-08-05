using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;
using LYBT.WPF.Client.Core.Enums;

namespace LYBT.WPF.Client.Shell.Dialogs.ViewModels
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

#pragma warning disable CS8618
        public event Action<IDialogResult> RequestClose;
#pragma warning restore CS8618

        public InformationDialogViewModel()
        {
            OkCommand = new DelegateCommand(OnOk);
        }

        private void OnOk()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
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

            if (parameters.ContainsKey("type"))
            {
                DialogType = parameters.GetValue<DialogType>("type");
            }
        }
    }
}