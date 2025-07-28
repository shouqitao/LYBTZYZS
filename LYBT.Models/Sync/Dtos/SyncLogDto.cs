using LYBT.Common.Enums.System;
using System.ComponentModel;

namespace LYBT.Models.Sync {

    /// <summary>
    /// 同步日志列表 DTO
    /// </summary>
    public class SyncLogDto {

        /// <summary>同步日志ID</summary>
        [DisplayName("同步日志ID")]
        public string Id { get; set; } = string.Empty;

        /// <summary>同步时间</summary>
        [DisplayName("同步时间")]
        public DateTime SyncTime { get; set; }

        /// <summary>同步模式</summary>
        [DisplayName("同步模式")]
        public SyncMode Mode { get; set; }

        /// <summary>同步状态</summary>
        [DisplayName("同步状态")]
        public SyncStatus Status { get; set; }

        /// <summary>错误或成功信息</summary>
        [DisplayName("错误或成功信息")]
        public string? Message { get; set; }
    }
}