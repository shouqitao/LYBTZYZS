using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Desktop.Core.ViewModels;

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
        private string _message = "";

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

        public ConfirmationDialogViewModel() : base()
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