using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Core.Extensions
{

    /// <summary>
    /// 自定义对话框服务扩展方法
    /// 提供向后兼容性，替代原 IDialogService 扩展
    /// 兼容 Prism 8.1.97
    /// </summary>
    public static class CustomDialogServiceExtensions
    {

        /// <summary>
        /// 显示确认对话框 (别名方法，提供向后兼容性)
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        /// <param name="message">消息内容</param>
        /// <param name="title">对话框标题</param>
        /// <returns>用户选择结果</returns>
        public static Task<bool> ShowConfirmAsync(this ICustomDialogService dialogService, string message, string title = "确认")
        {
            return dialogService.ShowConfirmationAsync(message, title);
        }

        /// <summary>
        /// 显示简单的消息对话框
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        /// <param name="message">消息内容</param>
        /// <param name="title">对话框标题</param>
        /// <param name="isError">是否为错误消息</param>
        /// <returns>任务</returns>
        public static Task ShowMessageAsync(this ICustomDialogService dialogService, string message, string title = "消息", bool isError = false)
        {
            return isError
                ? dialogService.ShowErrorAsync(message, title)
                : dialogService.ShowInformationAsync(message, title);
        }

        /// <summary>
        /// 显示操作结果对话框
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        /// <param name="isSuccess">操作是否成功</param>
        /// <param name="message">消息内容</param>
        /// <param name="successTitle">成功时的标题</param>
        /// <param name="errorTitle">失败时的标题</param>
        /// <returns>任务</returns>
        public static Task ShowResultAsync(this ICustomDialogService dialogService, bool isSuccess, string message,
            string successTitle = "成功", string errorTitle = "错误")
        {
            return isSuccess
                ? dialogService.ShowSuccessAsync(message, successTitle)
                : dialogService.ShowErrorAsync(message, errorTitle);
        }

        /// <summary>
        /// 显示删除确认对话框
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        /// <param name="itemName">要删除的项目名称</param>
        /// <param name="itemType">项目类型</param>
        /// <returns>用户确认结果</returns>
        public static Task<bool> ShowDeleteConfirmationAsync(
            this ICustomDialogService dialogService,
            string itemName, string itemType = "项目")
        {
            var message = $"确定要删除{itemType} \"{itemName}\" 吗？\n\n此操作无法撤销。";
            return dialogService.ShowConfirmationAsync(message, "确认删除");
        }

        /// <summary>
        /// 显示保存确认对话框
        /// </summary>
        /// <param name="dialogService">对话框服务</param>
        /// <param name="hasUnsavedChanges">是否有未保存的更改</param>
        /// <returns>用户选择结果 (true: 保存, false: 不保存, null: 取消)</returns>
        public static async Task<bool?> ShowSaveConfirmationAsync(this ICustomDialogService dialogService, bool hasUnsavedChanges = true)
        {
            if (!hasUnsavedChanges)
            {
                return true;
            }

            var message = "检测到未保存的更改。\n\n是否要保存这些更改？";

            // 使用标准二按钮确认对话框 (简化UX设计)
            var result = await dialogService.ShowConfirmationAsync(message, "保存确认");
            return result; // 简化实现：true=保存，false=不保存
        }
    }
}
