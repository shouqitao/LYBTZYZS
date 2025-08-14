using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace LYBT.Infrastructure.Logging.Dtos
{

    /// <summary>
    /// 系统日志传输对象
    /// </summary>
    public class SystemLogDto
    {

        /// <summary>
        /// 日志ID
        /// </summary>
        [DisplayName("日志ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        [DisplayName("日志级别")]
        public LogLevel Level { get; set; }

        /// <summary>
        /// 日志来源
        /// </summary>
        [DisplayName("日志来源")]
        public string? Source { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        [DisplayName("日志消息")]
        public string? Message { get; set; }

        /// <summary>
        /// 异常详情
        /// </summary>
        [DisplayName("异常详情")]
        public string? Exception { get; set; }

        /// <summary>
        /// 日志时间
        /// </summary>
        [DisplayName("日志时间")]
        public DateTime LogTime { get; set; }

        /// <summary>
        /// 服务器信息
        /// </summary>
        [DisplayName("服务器信息")]
        public string? ServerInfo { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [DisplayName("用户ID")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// 请求ID
        /// </summary>
        [DisplayName("请求ID")]
        public string? RequestId { get; set; }
    }
}