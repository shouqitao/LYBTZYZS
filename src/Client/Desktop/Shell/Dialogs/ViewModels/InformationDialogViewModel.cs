using LYBT.Desktop.Core.ViewModels;
using Prism.Commands;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels {

    /// <summary>
    /// 信息对话框视图模型
    /// </summary>
    /// <summary>
    /// 信息对话框ViewModel - UltraThink架构统一
    /// </summary>
    public class InformationDialogViewModel : DialogViewModelBase {
        private string _message = "";

        /// <summary>
        /// 对话框消息
        /// </summary>
        public string Message {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// 确定命令（兼容性）
        /// </summary>
        public DelegateCommand OkCommand => ConfirmCommand;

        public InformationDialogViewModel() : base() {
            Title = "提示";
        }

        /// <summary>
        /// 设置对话框内容
        /// </summary>
        public void SetContent(string message, string title = "提示") {
            Message = message;
            Title = title;
        }

        /// <summary>
        /// 执行确认逻辑（信息对话框直接关闭）
        /// </summary>
        protected override Task<bool> ExecuteConfirmAsync() {
            return Task.FromResult(true);
        }
    }
}
