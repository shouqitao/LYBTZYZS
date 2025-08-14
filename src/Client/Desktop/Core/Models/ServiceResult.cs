using System;

namespace LYBT.Desktop.Core.Models
{
    /// <summary>
    /// 服务层响应结果
    /// </summary>
    public class ServiceResult<T>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 异常信息（可选）
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 创建成功的结果
        /// </summary>
        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        /// <summary>
        /// 创建失败的结果
        /// </summary>
        public static ServiceResult<T> Failure(string errorMessage, Exception? exception = null)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// 无数据的服务响应结果
    /// </summary>
    public class ServiceResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 异常信息（可选）
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 创建成功的结果
        /// </summary>
        public static ServiceResult Success()
        {
            return new ServiceResult
            {
                IsSuccess = true
            };
        }

        /// <summary>
        /// 创建失败的结果
        /// </summary>
        public static ServiceResult Failure(string errorMessage, Exception? exception = null)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }
}