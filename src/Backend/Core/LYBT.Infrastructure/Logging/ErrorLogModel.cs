using System.ComponentModel;

namespace LYBT.Infrastructure.Logging
{

    /// <summary>
    /// 错误日志实体模型
    /// </summary>
    public class ErrorLogModel
    {

        /// <summary>
        /// 日志ID（主键）
        /// </summary>
        [DisplayName("日志ID（主键）")]
        public Guid Id { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        [DisplayName("错误消息")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 异常类型
        /// </summary>
        [DisplayName("异常类型")]
        public string? ExceptionType { get; set; }

        /// <summary>
        /// 堆栈跟踪
        /// </summary>
        [DisplayName("堆栈跟踪")]
        public string? StackTrace { get; set; }

        /// <summary>
        /// 内部异常
        /// </summary>
        [DisplayName("内部异常")]
        public string? InnerException { get; set; }

        /// <summary>
        /// 发生时间
        /// </summary>
        [DisplayName("发生时间")]
        public DateTime OccurredAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 用户ID（可选）
        /// </summary>
        [DisplayName("用户ID（可选）")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        [DisplayName("请求路径")]
        public string? RequestPath { get; set; }

        /// <summary>
        /// HTTP方法
        /// </summary>
        [DisplayName("HTTP方法")]
        public string? HttpMethod { get; set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        [DisplayName("客户端IP")]
        public string? ClientIP { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        [DisplayName("用户代理")]
        public string? UserAgent { get; set; }

        /// <summary>
        /// 服务器环境
        /// </summary>
        [DisplayName("服务器环境")]
        public string? Environment { get; set; }

        /// <summary>
        /// 错误级别
        /// </summary>
        [DisplayName("错误级别")]
        public string? Severity { get; set; }

        /// <summary>
        /// 是否已解决
        /// </summary>
        [DisplayName("是否已解决")]
        public bool IsResolved { get; set; } = false;

        /// <summary>
        /// 解决时间
        /// </summary>
        [DisplayName("解决时间")]
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// 解决备注
        /// </summary>
        [DisplayName("解决备注")]
        public string? ResolutionNotes { get; set; }
    }
}