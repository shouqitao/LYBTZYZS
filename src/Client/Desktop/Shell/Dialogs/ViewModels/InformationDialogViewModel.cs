using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 信息对话框视图模型 - Phase 4B 骨架实现
    /// </summary>
    public class InformationDialogViewModel : BindableBase
    {
        private string _message = string.Empty;
        private string _title = "信息";

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 信息消息
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// 确定命令
        /// </summary>
        public DelegateCommand OkCommand { get; }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        public event EventHandler? OkRequested;

        public InformationDialogViewModel()
        {
            OkCommand = new DelegateCommand(ExecuteOk);
        }

        private void ExecuteOk()
        {
            OkRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
