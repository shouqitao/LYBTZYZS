using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Models {

    /// <summary>
    /// 数据同步日志实体
    /// </summary>
    public class SyncLogModel {

        /// <summary>同步日志ID</summary>
        [DisplayName("同步日志ID")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>同步时间</summary>
        [DisplayName("同步时间")]
        public DateTime SyncTime { get; set; } = DateTime.Now;

        /// <summary>同步模式</summary>
        [DisplayName("同步模式")]
        public SyncMode Mode { get; set; } = SyncMode.Auto;

        /// <summary>同步状态</summary>
        [DisplayName("同步状态")]
        public SyncStatus Status { get; set; } = SyncStatus.Success;

        /// <summary>错误或成功信息</summary>
        [DisplayName("错误或成功信息")]
        public string? Message { get; set; }
    }
}