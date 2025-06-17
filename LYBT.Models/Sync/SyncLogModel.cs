using System;
using LYBT.Common.Enums;

namespace LYBT.Models {
    /// <summary>
    /// 数据同步日志实体
    /// </summary>
    public class SyncLogModel {
        /// <summary>同步日志ID</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>同步时间</summary>
        public DateTime SyncTime { get; set; } = DateTime.Now;

        /// <summary>同步模式</summary>
        public SyncMode Mode { get; set; } = SyncMode.Auto;

        /// <summary>同步状态</summary>
        public SyncStatus Status { get; set; } = SyncStatus.Success;

        /// <summary>错误或成功信息</summary>
        public string? Message { get; set; }
    }
}
