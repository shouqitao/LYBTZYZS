using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 确认对话框视图模型 - Phase 4B 骨架实现
    /// </summary>
    public class ConfirmationDialogViewModel : BindableBase
    {
        private string _message = string.Empty;
        private string _title = "确认";

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 确认消息
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// 是命令
        /// </summary>
        public DelegateCommand YesCommand { get; }

        /// <summary>
        /// 否命令
        /// </summary>
        public DelegateCommand NoCommand { get; }

        /// <summary>
        /// 是按钮点击事件
        /// </summary>
        public event EventHandler? YesRequested;

        /// <summary>
        /// 否按钮点击事件
        /// </summary>
        public event EventHandler? NoRequested;

        public ConfirmationDialogViewModel()
        {
            YesCommand = new DelegateCommand(ExecuteYes);
            NoCommand = new DelegateCommand(ExecuteNo);
        }

        private void ExecuteYes()
        {
            YesRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteNo()
        {
            NoRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
