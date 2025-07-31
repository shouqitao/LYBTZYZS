using System;
using System.Text.Json.Serialization;

namespace LYBT.WPF.Client.Core.Models.Common
{
    /// <summary>
    /// API响应基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResponse<T>
    {
        /// <summary>是否成功</summary>
        [JsonPropertyName("success")]
        public bool IsSuccess { get; set; }
        
        /// <summary>响应消息</summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        
        /// <summary>响应数据</summary>
        [JsonPropertyName("data")]
        public T? Data { get; set; }
        
        /// <summary>HTTP状态码</summary>
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }
        
        /// <summary>时间戳</summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        /// <summary>错误代码</summary>
        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }
        
        /// <summary>追踪ID（用于日志关联）</summary>
        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }
        
        /// <summary>
        /// 向后兼容属性
        /// </summary>
        [JsonIgnore]
        public bool Success => IsSuccess;
    }
}