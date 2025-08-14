using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;

namespace LYBT.Desktop.Core.ViewModels
{
    /// <summary>
    /// 对话框视图模型基类
    /// 提供通用的对话框功能
    /// </summary>
    public abstract class DialogViewModelBase : BindableBase
    {
        private string _title = "对话框";

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 关闭命令
        /// </summary>
        public DelegateCommand<string?> CloseDialogCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        protected DialogViewModelBase()
        {
            CloseDialogCommand = new DelegateCommand<string?>(CloseDialog);
        }

        /// <summary>
        /// 关闭对话框
        /// </summary>
        /// <param name="parameter">对话框返回参数</param>
        protected virtual void CloseDialog(string? parameter)
        {
            ButtonResult result = ButtonResult.None;

            if (parameter?.ToLower() == "true" || parameter?.ToLower() == "ok" || parameter?.ToLower() == "yes")
                result = ButtonResult.OK;
            else if (parameter?.ToLower() == "false" || parameter?.ToLower() == "cancel" || parameter?.ToLower() == "no")
                result = ButtonResult.Cancel;

            RaiseRequestClose(new DialogResult(result));
        }

        /// <summary>
        /// 触发请求关闭事件（子类需要实现）
        /// </summary>
        /// <param name="dialogResult">对话框结果</param>
        protected abstract void RaiseRequestClose(IDialogResult dialogResult);
    }
}
