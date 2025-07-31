using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Common
{
    /// <summary>
    /// API统一返回格式 - 前后端共享
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>是否成功</summary>
        [DisplayName("是否成功")]
        [JsonPropertyName("success")]
        public bool IsSuccess { get; set; }

        /// <summary>状态码</summary>
        [DisplayName("状态码")]
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>消息</summary>
        [DisplayName("消息")]
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>数据</summary>
        [DisplayName("数据")]
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        /// <summary>时间戳</summary>
        [DisplayName("时间戳")]
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>创建成功响应</summary>
        public static ApiResponse<T> Success(T data, string message = "操作成功")
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                StatusCode = 200,
                Message = message,
                Data = data
            };
        }

        /// <summary>创建失败响应</summary>
        public static ApiResponse<T> Fail(string message, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                StatusCode = statusCode,
                Message = message,
                Data = default(T)
            };
        }
    }
}