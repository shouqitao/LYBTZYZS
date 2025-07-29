using System.ComponentModel;

namespace LYBT.Models.Sync {

    /// <summary>
    /// 同步任务详情 DTO
    /// </summary>
    public class SyncTaskDetailDto {

        /// <summary>同步任务ID</summary>
        [DisplayName("同步任务ID")]
        public Guid Id { get; set; }

        /// <summary>任务类型</summary>
        [DisplayName("任务类型")]
        public string TaskType { get; set; } = string.Empty;

        /// <summary>任务状态</summary>
        [DisplayName("任务状态")]
        public string Status { get; set; } = string.Empty;

        /// <summary>任务触发时间</summary>
        [DisplayName("任务触发时间")]
        public DateTime TriggerTime { get; set; }

        /// <summary>实际执行时间</summary>
        [DisplayName("实际执行时间")]
        public DateTime? ExecuteTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}