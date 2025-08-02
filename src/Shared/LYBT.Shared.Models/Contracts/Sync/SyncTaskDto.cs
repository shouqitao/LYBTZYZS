using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Sync {

    /// <summary>
    /// 同步任务列表 DTO
    /// </summary>
    public class SyncTaskDto {

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
    }
}