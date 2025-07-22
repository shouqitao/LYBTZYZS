using System.ComponentModel;
namespace LYBT.Module.Sync.Dtos {

    /// <summary>
    /// 同步任务详情 DTO
    /// </summary>
    public class SyncTaskDetailDto {

        /// <summary>同步任务ID</summary>
        [DisplayName("同步任务ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>任务类型</summary>
        [DisplayName("任务类型")]
/// <summary>
/// TaskType 属性。
/// </summary>
        public string TaskType { get; set; } = string.Empty;

        /// <summary>任务状态</summary>
        [DisplayName("任务状态")]
/// <summary>
/// Status 属性。
/// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>任务触发时间</summary>
        [DisplayName("任务触发时间")]
/// <summary>
/// TriggerTime 属性。
/// </summary>
        public DateTime TriggerTime { get; set; }

        /// <summary>实际执行时间</summary>
        [DisplayName("实际执行时间")]
/// <summary>
/// ExecuteTime 属性。
/// </summary>
        public DateTime? ExecuteTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
