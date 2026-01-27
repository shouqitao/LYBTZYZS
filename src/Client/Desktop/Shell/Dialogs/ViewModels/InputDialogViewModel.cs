using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 输入对话框视图模型
    /// OpenSpec: fix-missing-dialogs - 系统性设计
    ///
    /// 用于获取用户输入的对话框。
    /// </summary>
    public partial class InputDialogViewModel : DialogViewModelBase
    {
        #region 可观察属性

        /// <summary>
        /// 提示消息
        /// </summary>
        [ObservableProperty]
        private string _message = string.Empty;

        /// <summary>
        /// 用户输入值
        /// </summary>
        [ObservableProperty]
        private string _inputValue = string.Empty;

        /// <summary>
        /// 占位符文本
        /// </summary>
        [ObservableProperty]
        private string _placeholder = string.Empty;

        /// <summary>
        /// 确认按钮文本
        /// </summary>
        [ObservableProperty]
        private string _okButtonText = "确定";

        /// <summary>
        /// 取消按钮文本
        /// </summary>
        [ObservableProperty]
        private string _cancelButtonText = "取消";

        /// <summary>
        /// 是否必填
        /// </summary>
        [ObservableProperty]
        private bool _isRequired = true;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public InputDialogViewModel(IViewModelServices services)
            : base(services)
        {
            Title = "输入";
        }

        #endregion

        #region 对话框生命周期

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        protected override void OnDialogOpenedCore(IDialogParameters? parameters)
        {
            if (parameters == null) return;

            // 读取参数
            Message = GetDialogParameter(parameters, "message", string.Empty);
            Title = GetDialogParameter(parameters, "title", "输入");
            InputValue = GetDialogParameter(parameters, "defaultValue", string.Empty);
            Placeholder = GetDialogParameter(parameters, "placeholder", string.Empty);
            IsRequired = GetDialogParameter(parameters, "isRequired", true);

            Logger.LogInformation("InputDialog - 打开对话框，标题：{Title}", Title);
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        protected override void OnDialogClosedCore()
        {
            Logger.LogDebug("InputDialog - 对话框已关闭");
        }

        #endregion

        #region 命令

        /// <summary>
        /// 是否可以确认
        /// </summary>
        protected override bool CanConfirm()
        {
            if (!base.CanConfirm()) return false;

            // 如果必填，检查输入是否为空
            if (IsRequired && string.IsNullOrWhiteSpace(InputValue))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 确认命令 - 返回输入值
        /// </summary>
        protected override void Confirm()
        {
            Logger.LogDebug("InputDialog - 确认，输入值：{InputValue}", InputValue);

            var parameters = new DialogParameters
            {
                { "input", InputValue }
            };
            CloseDialog(parameters, ButtonResult.OK);
        }

        #endregion

        #region 属性变更

        /// <summary>
        /// InputValue 变更时通知 ConfirmCommand
        /// </summary>
        partial void OnInputValueChanged(string value)
        {
            ConfirmCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }
}
