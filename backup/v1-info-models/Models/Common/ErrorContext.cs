using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Core.Models.Common
{
    /// <summary>
    /// 错误上下文信息
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// 错误发生的操作名称
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID（如果已登录）
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 用户名（如果已登录）
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        public string? UserRole { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 模块名称
        /// </summary>
        public string? ModuleName { get; set; }

        /// <summary>
        /// 视图/页面名称
        /// </summary>
        public string? ViewName { get; set; }

        /// <summary>
        /// 附加的上下文数据
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 错误发生时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 添加上下文数据
        /// </summary>
        public void AddData(string key, object value)
        {
            AdditionalData[key] = value;
        }

        /// <summary>
        /// 获取上下文数据
        /// </summary>
        public T? GetData<T>(string key)
        {
            if (AdditionalData.TryGetValue(key, out var value) && value is T result)
            {
                return result;
            }
            return default;
        }
    }
}