using System;

namespace LYBT.Module.Sync.Dtos {
    /// <summary>
    /// 同步任务列表 DTO
    /// </summary>
    public class SyncTaskDto {
        /// <summary>同步任务ID</summary>
        public Guid Id { get; set; }

        /// <summary>任务类型</summary>
        public string TaskType { get; set; } = string.Empty;

        /// <summary>任务状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>任务触发时间</summary>
        public DateTime TriggerTime { get; set; }
    }
}
