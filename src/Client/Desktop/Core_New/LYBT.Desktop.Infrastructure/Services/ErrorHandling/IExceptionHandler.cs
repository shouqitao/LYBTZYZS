using System;
using System.Threading.Tasks;

namespace LYBT.Desktop.Infrastructure.Services.ErrorHandling
{
    /// <summary>
    /// 异常处理器接口 - UltraThink架构
    /// </summary>
    public interface IExceptionHandler
    {
        /// <summary>
        /// 处理异常
        /// </summary>
        Task<bool> HandleAsync(Exception exception, string? context = null);

        /// <summary>
        /// 是否可以处理指定类型的异常
        /// </summary>
        bool CanHandle(Exception exception);

        /// <summary>
        /// 获取错误消息
        /// </summary>
        string GetErrorMessage(Exception exception);

        /// <summary>
        /// 记录异常
        /// </summary>
        Task LogExceptionAsync(Exception exception, string? context = null);
    }
}