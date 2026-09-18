using Prism.Services.Dialogs;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 对话框管理服务实现
    ///
    /// 集成Prism IDialogService，使用统一的 MessageDialog 处理所有消息类型
    /// </summary>
    public class DialogManager : IDialogManager
    {
        private readonly IDialogService _dialogService;

        /// <summary>
        /// 统一消息对话框名称
        /// </summary>
        private const string MessageDialogName = "MessageDialog";

        public DialogManager(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        /// <inheritdoc/>
        public Task ShowSuccessAsync(string message, string? title = null)
        {
            return ShowMessageDialogAsync("success", message, title ?? "成功");
        }

        /// <inheritdoc/>
        public Task ShowErrorAsync(string message, string? title = null)
        {
            return ShowMessageDialogAsync("error", message, title ?? "错误");
        }

        /// <inheritdoc/>
        public Task ShowWarningAsync(string message, string? title = null)
        {
            return ShowMessageDialogAsync("warning", message, title ?? "警告");
        }

        /// <summary>
        /// 显示统一消息对话框
        /// </summary>
        /// <param name="type">消息类型 (success/error/warning/info)</param>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        private Task ShowMessageDialogAsync(string type, string message, string title)
        {
            var tcs = new TaskCompletionSource<bool>();

            var parameters = new DialogParameters
            {
                { DialogParams.Message, message },
                { DialogParams.Title, title },
                { DialogParams.Type, type }
            };

            _dialogService.ShowDialog(MessageDialogName, parameters, _ =>
            {
                tcs.SetResult(true);
            });

            return tcs.Task;
        }

        /// <inheritdoc/>
        public Task<bool> ShowConfirmAsync(string message, string? title = null)
        {
            var tcs = new TaskCompletionSource<bool>();

            // 键名 PascalCase，与 ConfirmationDialogViewModel.OnDialogOpenedCore 一致
            var parameters = new DialogParameters
            {
                { DialogParams.Message, message },
                { DialogParams.Title, title ?? "确认" }
            };

            _dialogService.ShowDialog("ConfirmationDialog", parameters, result =>
            {
                tcs.SetResult(result.Result == ButtonResult.OK);
            });

            return tcs.Task;
        }

    }
}
