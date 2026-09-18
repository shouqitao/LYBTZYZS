using LYBT.Desktop.Contracts.Services;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// Prism 对话框参数键（PascalCase 统一，禁止 lowercase 双轨）
    /// MessageDialog / ConfirmationDialog / InputDialog 共用
    /// </summary>
    public static class DialogParams
    {
        /// <summary>消息内容</summary>
        public const string Message = "Message";
        /// <summary>标题</summary>
        public const string Title = "Title";
        /// <summary>消息类型 success/error/warning/info</summary>
        public const string Type = "Type";
        /// <summary>图标路径</summary>
        public const string IconSource = "IconSource";
        /// <summary>确认按钮文案</summary>
        public const string ConfirmButtonText = "ConfirmButtonText";
        /// <summary>取消按钮文案</summary>
        public const string CancelButtonText = "CancelButtonText";
        /// <summary>第三按钮（否）文案</summary>
        public const string NoButtonText = "NoButtonText";
        /// <summary>是否显示第三按钮（否）</summary>
        public const string ShowNoButton = "ShowNoButton";
        /// <summary>是否显示删除选项</summary>
        public const string ShowDeleteOptions = "ShowDeleteOptions";
        /// <summary>输入默认值</summary>
        public const string DefaultValue = "DefaultValue";
        /// <summary>输入占位符</summary>
        public const string Placeholder = "Placeholder";
        /// <summary>输入是否必填</summary>
        public const string IsRequired = "IsRequired";
        /// <summary>输入返回值</summary>
        public const string Input = "Input";
        /// <summary>是否软删除</summary>
        public const string IsSoftDelete = "IsSoftDelete";
    }

    /// <summary>
    /// 通用对话框服务实现
    /// 委托 Prism Dialog（MessageDialog / ConfirmationDialog），禁止 System.Windows.MessageBox
    /// </summary>
    public class CommonDialogService : ICommonDialogService
    {
        private const string MessageDialogName = "MessageDialog";
        private const string ConfirmationDialogName = "ConfirmationDialog";

        private readonly IDialogService _dialogService;

        public CommonDialogService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        public Task ShowWarningAsync(string message, string? title = null)
            => ShowMessageAsync("warning", message, title ?? "警告");

        /// <summary>
        /// 显示错误消息
        /// </summary>
        public Task ShowErrorAsync(string message, string? title = null)
            => ShowMessageAsync("error", message, title ?? "错误");

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public Task<bool> ShowConfirmAsync(string message, string? title = null)
        {
            var tcs = new TaskCompletionSource<bool>();
            var parameters = new DialogParameters
            {
                { DialogParams.Message, message },
                { DialogParams.Title, title ?? "确认" }
            };

            _dialogService.ShowDialog(ConfirmationDialogName, parameters, result =>
            {
                tcs.SetResult(result.Result == ButtonResult.OK);
            });

            return tcs.Task;
        }

        /// <summary>
        /// 显示三选项对话框（是/否/取消）— ConfirmationDialog 扩展 ShowNoButton
        /// Issue #2247: 支持离开确认等三选项场景
        /// </summary>
        public Task<TripleChoiceResult> ShowTripleChoiceAsync(string message, string? title = null)
        {
            var tcs = new TaskCompletionSource<TripleChoiceResult>();
            var parameters = new DialogParameters
            {
                { DialogParams.Message, message },
                { DialogParams.Title, title ?? "确认" },
                { DialogParams.ShowNoButton, true },
                { DialogParams.NoButtonText, "否" }
            };

            _dialogService.ShowDialog(ConfirmationDialogName, parameters, result =>
            {
                var choice = result.Result switch
                {
                    ButtonResult.OK => TripleChoiceResult.Yes,
                    ButtonResult.No => TripleChoiceResult.No,
                    _ => TripleChoiceResult.Cancel
                };
                tcs.SetResult(choice);
            });

            return tcs.Task;
        }

        private Task ShowMessageAsync(string type, string message, string title)
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
    }
}
