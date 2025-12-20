using System.Windows;
using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 用户通知服务实现 - UltraThink架构
    /// 提供基于 WPF MessageBox 的简单用户通知功能
    /// Issue #840: 替代 ErrorHandlingServiceStub,提供真实实现
    /// </summary>
    public class UserNotificationService : IUserNotificationService
    {
        /// <summary>
        /// 处理异常并显示给用户
        /// </summary>
        public Task HandleExceptionAsync(Exception exception, string? context = null)
        {
            var message = context != null
                ? $"{context}\n\n错误详情: {exception.Message}"
                : exception.Message;

            return ShowErrorAsync(message, "错误");
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        public Task ShowErrorAsync(string message, string? title = null)
        {
            MessageBox.Show(
                message,
                title ?? "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示成功消息
        /// </summary>
        public Task ShowSuccessAsync(string message, string? title = null)
        {
            MessageBox.Show(
                message,
                title ?? "成功",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        public Task ShowWarningAsync(string message, string? title = null)
        {
            MessageBox.Show(
                message,
                title ?? "警告",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示信息消息
        /// </summary>
        public Task ShowInfoAsync(string message, string? title = null)
        {
            MessageBox.Show(
                message,
                title ?? "信息",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public Task<bool> ShowConfirmAsync(string message, string? title = null)
        {
            var result = MessageBox.Show(
                message,
                title ?? "确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return Task.FromResult(result == MessageBoxResult.Yes);
        }
    }
}
