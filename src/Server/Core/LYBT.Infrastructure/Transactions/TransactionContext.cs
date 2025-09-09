using System;
using System.Collections.Generic;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 事务执行上下文基类
    /// </summary>
    public abstract class TransactionContext
    {
        /// <summary>
        /// 获取或设置事务ID
        /// </summary>
        public Guid TransactionId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 获取或设置事务开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 获取或设置用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 获取或设置事务名称
        /// </summary>
        public string TransactionName { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置事务属性集合
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// 获取或设置事务执行状态
        /// </summary>
        public TransactionStatus Status { get; set; } = TransactionStatus.NotStarted;

        /// <summary>
        /// 获取事务执行时长
        /// </summary>
        public TimeSpan Duration => DateTime.UtcNow - StartTime;

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="key">属性键</param>
        /// <param name="value">属性值</param>
        public void SetProperty(string key, object value)
        {
            Properties[key] = value;
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="key">属性键</param>
        /// <returns>属性值</returns>
        public T? GetProperty<T>(string key)
        {
            return Properties.TryGetValue(key, out var value) ? (T?)value : default;
        }
    }

    /// <summary>
    /// 事务执行状态
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        NotStarted,

        /// <summary>
        /// 执行中
        /// </summary>
        Running,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 已失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已回滚
        /// </summary>
        RolledBack
    }
}