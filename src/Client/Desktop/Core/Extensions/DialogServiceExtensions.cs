using System;
using System.Threading.Tasks;
using Prism.Dialogs;

namespace LYBT.Desktop.Core.Extensions
{
    /// <summary>
    /// IDialogService 扩展方法
    /// </summary>
    public static class DialogServiceExtensions
    {
        /// <summary>
        /// 显示信息对话框
        /// </summary>
        public static Task ShowInformationAsync(this IDialogService dialogService, string message, string title = "信息")
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message }
            };

            return Task.Run(() =>
            {
                dialogService.ShowDialog("MessageDialog", parameters, result => { });
            });
        }

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        public static Task ShowWarningAsync(this IDialogService dialogService, string message, string title = "警告")
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "MessageType", "Warning" }
            };

            return Task.Run(() =>
            {
                dialogService.ShowDialog("MessageDialog", parameters, result => { });
            });
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        public static Task ShowErrorAsync(this IDialogService dialogService, string message, string title = "错误")
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "MessageType", "Error" }
            };

            return Task.Run(() =>
            {
                dialogService.ShowDialog("MessageDialog", parameters, result => { });
            });
        }

        /// <summary>
        /// 显示成功对话框
        /// </summary>
        public static Task ShowSuccessAsync(this IDialogService dialogService, string message, string title = "成功")
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "MessageType", "Success" }
            };

            return Task.Run(() =>
            {
                dialogService.ShowDialog("MessageDialog", parameters, result => { });
            });
        }

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        public static Task<string?> ShowInputAsync(this IDialogService dialogService, string message, string title = "输入", string defaultValue = "")
        {
            var tcs = new TaskCompletionSource<string?>();

            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "DefaultValue", defaultValue },
                { "MessageType", "Input" }
            };

            dialogService.ShowDialog("InputDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK || result.Result == ButtonResult.Yes)
                {
                    tcs.SetResult(result.Parameters.GetValue<string>("InputValue"));
                }
                else
                {
                    tcs.SetResult(null);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public static Task<bool> ShowConfirmationAsync(this IDialogService dialogService, string message, string title = "确认")
        {
            var tcs = new TaskCompletionSource<bool>();

            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "MessageType", "Confirmation" }
            };

            dialogService.ShowDialog("MessageDialog", parameters, result =>
            {
                tcs.SetResult(result.Result == ButtonResult.OK || result.Result == ButtonResult.Yes);
            });

            return tcs.Task;
        }

        /// <summary>
        /// 显示确认对话框（别名）
        /// </summary>
        public static Task<bool> ShowConfirmAsync(this IDialogService dialogService, string message, string title = "确认")
        {
            return ShowConfirmationAsync(dialogService, message, title);
        }
    }
}