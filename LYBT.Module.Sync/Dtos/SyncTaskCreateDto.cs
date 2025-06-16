using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Sync.Dtos {
    /// <summary>
    /// 新增同步任务 DTO
    /// </summary>
    public class SyncTaskCreateDto {
        /// <summary>任务类型（如“手动同步”/“自动同步”）</summary>
        [Required(ErrorMessage = "任务类型不能为空")]
        public string TaskType { get; set; } = string.Empty;

        /// <summary>任务状态（如“进行中”、“已完成”）</summary>
        [Required(ErrorMessage = "任务状态不能为空")]
        public string Status { get; set; } = string.Empty;

        /// <summary>任务触发时间</summary>
        public DateTime TriggerTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
