using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Services.Exceptions
{
    /// <summary>
    /// 统一异常处理器接口 - 简化架构版本
    /// 提供统一的异常处理逻辑，遵循"适度设计、拒绝过度工程"原则
    /// </summary>
    public interface IExceptionHandler
    {
        /// <summary>
        /// 处理异常并返回用户友好的结果
        /// </summary>
        ServiceResult<T> HandleException<T>(Exception exception, string methodName, string? context = null);

        /// <summary>
        /// 处理异常并返回无数据的结果
        /// </summary>
        ServiceResult HandleException(Exception exception, string methodName, string? context = null);

        /// <summary>
        /// 安全执行操作，自动处理异常
        /// </summary>
        Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<ServiceResult<T>>> operation, string methodName, string? context = null);

        /// <summary>
        /// 安全执行无返回值的操作，自动处理异常
        /// </summary>
        Task<ServiceResult> SafeExecuteAsync(Func<Task<ServiceResult>> operation, string methodName, string? context = null);
    }
}
