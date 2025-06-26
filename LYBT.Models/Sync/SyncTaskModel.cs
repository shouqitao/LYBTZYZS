namespace LYBT.Models {

    /// <summary>
    /// 数据同步任务实体（用于计划、自动或手动同步任务记录）
    /// </summary>
    public class SyncTaskModel {

        /// <summary>同步任务ID</summary>
        public Guid Id { get; set; }

        /// <summary>任务类型（如全量/增量/手动/自动等）</summary>
        public string TaskType { get; set; } = string.Empty;

        /// <summary>任务状态（如已完成/进行中/失败）</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>任务触发时间</summary>
        public DateTime TriggerTime { get; set; } = DateTime.Now;

        /// <summary>实际执行时间</summary>
        public DateTime? ExecuteTime { get; set; }

        /// <summary>日志说明</summary>
        public string? Remark { get; set; }
    }
}