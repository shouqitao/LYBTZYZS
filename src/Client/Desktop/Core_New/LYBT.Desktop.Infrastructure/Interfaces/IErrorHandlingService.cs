using System;
using System.Threading.Tasks;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 错误处理服务接口 - UltraThink架构
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// 处理异常
        /// </summary>
        Task HandleExceptionAsync(Exception exception, string? context = null);

        /// <summary>
        /// 显示错误消息
        /// </summary>
        Task ShowErrorAsync(string message, string? title = null);

        /// <summary>
        /// 显示成功消息
        /// </summary>
        Task ShowSuccessAsync(string message, string? title = null);

        /// <summary>
        /// 显示警告消息
        /// </summary>
        Task ShowWarningAsync(string message, string? title = null);

        /// <summary>
        /// 显示信息消息
        /// </summary>
        Task ShowInfoAsync(string message, string? title = null);

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        Task<bool> ShowConfirmAsync(string message, string? title = null);

        /// <summary>
        /// 注册全局异常处理器
        /// </summary>
        void RegisterGlobalExceptionHandlers();
    }
}