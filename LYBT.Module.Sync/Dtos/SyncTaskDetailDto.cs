using System;

namespace LYBT.Module.Sync.Dtos {
    /// <summary>
    /// 同步任务详情 DTO
    /// </summary>
    public class SyncTaskDetailDto {
        /// <summary>同步任务ID</summary>
        public Guid Id { get; set; }

        /// <summary>任务类型</summary>
        public string TaskType { get; set; } = string.Empty;

        /// <summary>任务状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>任务触发时间</summary>
        public DateTime TriggerTime { get; set; }

        /// <summary>实际执行时间</summary>
        public DateTime? ExecuteTime { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
