using System;

namespace LYBT.Module.Sync.Dtos {
    /// <summary>
    /// 同步日志列表 DTO
    /// </summary>
    public class SyncLogDto {
        /// <summary>同步日志ID</summary>
        public Guid Id { get; set; }

        /// <summary>同步类型</summary>
        public string SyncType { get; set; } = string.Empty;

        /// <summary>同步状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>同步时间</summary>
        public DateTime SyncTime { get; set; }

        /// <summary>日志说明</summary>
        public string? Remark { get; set; }
    }
}
