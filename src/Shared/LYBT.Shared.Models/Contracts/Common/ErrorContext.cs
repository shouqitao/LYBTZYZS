using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 错误上下文信息
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// 操作名称
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// 模块名称
        /// </summary>
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// 用户信息
        /// </summary>
        public string User { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 附加数据
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }
}