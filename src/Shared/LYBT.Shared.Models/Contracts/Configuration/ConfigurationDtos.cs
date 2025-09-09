using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Configuration
{

    /// <summary>
    /// 日志DTO
    /// </summary>
    public class LogDto : BaseDto
    {

        /// <summary>日志级别</summary>
        [DisplayName("日志级别")]
        public string Level { get; set; } = string.Empty;

        /// <summary>日志消息</summary>
        [DisplayName("日志消息")]
        public string Message { get; set; } = string.Empty;

        /// <summary>日志来源</summary>
        [DisplayName("日志来源")]
        public string? Source { get; set; }

        /// <summary>异常信息</summary>
        [DisplayName("异常信息")]
        public string? Exception { get; set; }

        /// <summary>用户ID</summary>
        [DisplayName("用户ID")]
        public Guid? UserId { get; set; }

        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        public string? Username { get; set; }

        /// <summary>操作类型</summary>
        [DisplayName("操作类型")]
        public string? ActionType { get; set; }

        /// <summary>IP地址</summary>
        [DisplayName("IP地址")]
        public string? IpAddress { get; set; }

        /// <summary>请求路径</summary>
        [DisplayName("请求路径")]
        public string? RequestPath { get; set; }

        /// <summary>请求方法</summary>
        [DisplayName("请求方法")]
        public string? RequestMethod { get; set; }

        /// <summary>响应状态码</summary>
        [DisplayName("响应状态码")]
        public int? StatusCode { get; set; }

        /// <summary>执行时长(毫秒)</summary>
        [DisplayName("执行时长")]
        public long? Duration { get; set; }

        /// <summary>附加数据</summary>
        [DisplayName("附加数据")]
        public string? AdditionalData { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
