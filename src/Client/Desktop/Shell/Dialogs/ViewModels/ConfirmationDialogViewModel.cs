using LYBT.Desktop.Core.ViewModels;
using Prism.Commands;
using Prism.Events;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{

    /// <summary>
    /// 确认对话框视图模型
    /// </summary>
    /// <summary>
    /// 确认对话框ViewModel - UltraThink架构统一
    /// </summary>
    public class ConfirmationDialogViewModel : DialogViewModelBase
    {
        private string _message = string.Empty;

        /// <summary>
        /// 对话框消息
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// 是命令（兼容性）
        /// </summary>
        public DelegateCommand YesCommand => ConfirmCommand;

        /// <summary>
        /// 否命令（兼容性）
        /// </summary>
        public DelegateCommand NoCommand => CancelCommand;

        public ConfirmationDialogViewModel(
            IEventAggregator eventAggregator,
            IErrorHandlingService? errorHandlingService = null) 
            : base(eventAggregator, errorHandlingService)
        {
            Title = "确认";
        }

        /// <summary>
        /// 设置对话框内容
        /// </summary>
        public void SetContent(string message, string title = "确认")
        {
            Message = message;
            Title = title;
        }

        /// <summary>
        /// 执行确认逻辑
        /// </summary>
        protected override Task<bool> ExecuteConfirmAsync()
        {
            return Task.FromResult(true);
        }
    }
}
