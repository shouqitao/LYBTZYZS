using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.ExceptionHandling;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 用户通知服务实现 - UltraThink架构
    /// 统一委托 <see cref="IDialogManager"/>（Prism MDIX Dialog），禁止 System.Windows.MessageBox。
    /// Issue #840: 替代 ErrorHandlingServiceStub,提供真实实现
    /// </summary>
    public class UserNotificationService : IUserNotificationService
    {
        private readonly IDialogManager _dialogManager;

        public UserNotificationService(IDialogManager dialogManager)
        {
            _dialogManager = dialogManager ?? throw new ArgumentNullException(nameof(dialogManager));
        }

        /// <summary>
        /// 处理异常并显示给用户
        /// </summary>
        public Task HandleExceptionAsync(Exception exception, string? context = null)
        {
            var message = context != null
                ? ClientErrorMessageMapper.GetSafeOperationFailureMessage(context, exception)
                : ClientErrorMessageMapper.GetUserFriendlyMessage(exception);

            return ShowErrorAsync(message, "错误");
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        public Task ShowErrorAsync(string message, string? title = null)
            => _dialogManager.ShowErrorAsync(message, title ?? "错误");

        /// <summary>
        /// 显示成功消息
        /// </summary>
        public Task ShowSuccessAsync(string message, string? title = null)
            => _dialogManager.ShowSuccessAsync(message, title ?? "成功");

        /// <summary>
        /// 显示警告消息
        /// </summary>
        public Task ShowWarningAsync(string message, string? title = null)
            => _dialogManager.ShowWarningAsync(message, title ?? "警告");

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public Task<bool> ShowConfirmAsync(string message, string? title = null)
            => _dialogManager.ShowConfirmAsync(message, title ?? "确认");
    }
}
