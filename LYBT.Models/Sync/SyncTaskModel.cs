using System.ComponentModel;

namespace LYBT.Models.Sync {

    /// <summary>
    /// 数据同步任务实体（用于计划、自动或手动同步任务记录）
    /// </summary>
    public class SyncTaskModel {

        /// <summary>同步任务ID</summary>
        [DisplayName("同步任务ID")]
        public Guid Id { get; set; }

        /// <summary>任务类型（如全量/增量/手动/自动等）</summary>
        [DisplayName("任务类型（如全量/增量/手动/自动等）")]
        public string TaskType { get; set; } = string.Empty;

        /// <summary>任务状态（如已完成/进行中/失败）</summary>
        [DisplayName("任务状态（如已完成/进行中/失败）")]
        public string Status { get; set; } = string.Empty;

        /// <summary>任务触发时间</summary>
        [DisplayName("任务触发时间")]
        public DateTime TriggerTime { get; set; } = DateTime.Now;

        /// <summary>实际执行时间</summary>
        [DisplayName("实际执行时间")]
        public DateTime? ExecuteTime { get; set; }

        /// <summary>日志说明</summary>
        [DisplayName("日志说明")]
        public string? Remark { get; set; }
    }
}