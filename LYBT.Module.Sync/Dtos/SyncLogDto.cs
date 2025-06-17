using System;
using LYBT.Common.Enums;

namespace LYBT.Module.Sync.Dtos {
    /// <summary>
    /// 同步日志列表 DTO
    /// </summary>
    public class SyncLogDto {
        /// <summary>同步日志ID</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>同步时间</summary>
        public DateTime SyncTime { get; set; }

        /// <summary>同步模式</summary>
        public SyncMode Mode { get; set; }

        /// <summary>同步状态</summary>
        public SyncStatus Status { get; set; }

        /// <summary>错误或成功信息</summary>
        public string? Message { get; set; }
    }
}
