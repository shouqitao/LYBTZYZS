using Prism.Services.Dialogs;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 对话框管理服务实现
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 集成Prism IDialogService
    /// </summary>
    public class DialogManager : IDialogManager
    {
        private readonly IDialogService _dialogService;

        public DialogManager(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        /// <inheritdoc/>
        public Task ShowSuccessAsync(string message, string? title = null)
        {
            return ShowMessageAsync("SuccessDialog", message, title ?? "成功");
        }

        /// <inheritdoc/>
        public Task ShowErrorAsync(string message, string? title = null)
        {
            return ShowMessageAsync("ErrorDialog", message, title ?? "错误");
        }

        /// <inheritdoc/>
        public Task ShowWarningAsync(string message, string? title = null)
        {
            return ShowMessageAsync("WarningDialog", message, title ?? "警告");
        }

        /// <inheritdoc/>
        public Task ShowInfoAsync(string message, string? title = null)
        {
            return ShowMessageAsync("InfoDialog", message, title ?? "提示");
        }

        /// <inheritdoc/>
        public Task<bool> ShowConfirmAsync(string message, string? title = null)
        {
            var tcs = new TaskCompletionSource<bool>();

            var parameters = new DialogParameters
            {
                { "message", message },
                { "title", title ?? "确认" }
            };

            _dialogService.ShowDialog("ConfirmDialog", parameters, result =>
            {
                tcs.SetResult(result.Result == ButtonResult.OK);
            });

            return tcs.Task;
        }

        /// <inheritdoc/>
        public Task<string?> ShowInputAsync(string message, string? title = null, string? defaultValue = null)
        {
            var tcs = new TaskCompletionSource<string?>();

            var parameters = new DialogParameters
            {
                { "message", message },
                { "title", title ?? "输入" },
                { "defaultValue", defaultValue ?? string.Empty }
            };

            _dialogService.ShowDialog("InputDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    tcs.SetResult(result.Parameters.GetValue<string>("input"));
                }
                else
                {
                    tcs.SetResult(null);
                }
            });

            return tcs.Task;
        }

        /// <inheritdoc/>
        public Task<TResult?> ShowDialogAsync<TResult>(string dialogName, IDictionary<string, object>? parameters = null)
        {
            var tcs = new TaskCompletionSource<TResult?>();

            var dialogParams = new DialogParameters();
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    dialogParams.Add(kvp.Key, kvp.Value);
                }
            }

            _dialogService.ShowDialog(dialogName, dialogParams, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    tcs.SetResult(result.Parameters.GetValue<TResult>("result"));
                }
                else
                {
                    tcs.SetResult(default);
                }
            });

            return tcs.Task;
        }

        private Task ShowMessageAsync(string dialogName, string message, string title)
        {
            var tcs = new TaskCompletionSource<bool>();

            var parameters = new DialogParameters
            {
                { "message", message },
                { "title", title }
            };

            _dialogService.ShowDialog(dialogName, parameters, _ =>
            {
                tcs.SetResult(true);
            });

            return tcs.Task;
        }
    }
}
