using System;

namespace LYBT.Models {
    /// <summary>
    /// 数据同步日志实体
    /// </summary>
    public class SyncLogModel {
        /// <summary>同步日志ID</summary>
        public Guid Id { get; set; }

        /// <summary>同步类型</summary>
        public string SyncType { get; set; } = string.Empty;

        /// <summary>同步时间</summary>
        public DateTime SyncTime { get; set; } = DateTime.Now;

        /// <summary>同步状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>日志说明</summary>
        public string? Remark { get; set; }
    }
}
