namespace LYBT.Desktop.Core.Models
{

    /// <summary>
    /// 问题详情模型，用于解析 API 错误响应
    /// </summary>
    public class ProblemDetails
    {

        /// <summary>
        /// 错误类型
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// 错误标题
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// HTTP 状态码
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// 错误详情
        /// </summary>
        public string? Detail { get; set; }

        /// <summary>
        /// 实例标识
        /// </summary>
        public string? Instance { get; set; }

        /// <summary>
        /// 扩展信息
        /// </summary>
        public Dictionary<string, object>? Extensions { get; set; }
    }
}
