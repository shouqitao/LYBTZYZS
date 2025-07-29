namespace LYBT.WPF.Client.Core.Models.Common
{
    /// <summary>
    /// API响应基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResponse<T>
    {
        /// <summary>是否成功</summary>
        public bool Success { get; set; }
        
        /// <summary>响应消息</summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>响应数据</summary>
        public T? Data { get; set; }
        
        /// <summary>错误代码</summary>
        public string? ErrorCode { get; set; }
    }
}