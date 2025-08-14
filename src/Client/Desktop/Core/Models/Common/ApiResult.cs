using System;

namespace LYBT.Desktop.Core.Models.Common
{
    /// <summary>
    /// API调用结果包装类
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    public class ApiResult<T>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 返回数据
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static ApiResult<T> Success(T data)
        {
            return new ApiResult<T>
            {
                IsSuccess = true,
                Data = data
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static ApiResult<T> Failure(string errorMessage, string? errorCode = null)
        {
            return new ApiResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };
        }
    }
}