using System;

namespace LYBT.Desktop.Core.Models.Events
{
    /// <summary>
    /// 数据变更事件参数
    /// </summary>
    public class DataChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 数据源
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 变更类型
        /// </summary>
        public DataChangeType ChangeType { get; set; }

        /// <summary>
        /// 变更的数据
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 旧数据
        /// </summary>
        public object? OldData { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangeTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 附加信息
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        public DataChangedEventArgs()
        {
        }

        public DataChangedEventArgs(string source, string dataType, DataChangeType changeType, object? data = null)
        {
            Source = source;
            DataType = dataType;
            ChangeType = changeType;
            Data = data;
        }
    }

    /// <summary>
    /// 数据变更类型
    /// </summary>
    public enum DataChangeType
    {
        /// <summary>
        /// 添加
        /// </summary>
        Added,

        /// <summary>
        /// 更新
        /// </summary>
        Updated,

        /// <summary>
        /// 删除
        /// </summary>
        Deleted,

        /// <summary>
        /// 批量变更
        /// </summary>
        Batch,

        /// <summary>
        /// 刷新
        /// </summary>
        Refreshed,

        /// <summary>
        /// 清空
        /// </summary>
        Cleared
    }

    /// <summary>
    /// 数据保存事件参数
    /// </summary>
    public class DataSavedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 保存的数据数量
        /// </summary>
        public int SavedCount { get; set; }

        /// <summary>
        /// 保存时间
        /// </summary>
        public DateTime SaveTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 数据验证事件参数
    /// </summary>
    public class DataValidationEventArgs : EventArgs
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证错误
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();

        /// <summary>
        /// 验证的字段
        /// </summary>
        public string? FieldName { get; set; }

        /// <summary>
        /// 验证的值
        /// </summary>
        public object? Value { get; set; }
    }
}