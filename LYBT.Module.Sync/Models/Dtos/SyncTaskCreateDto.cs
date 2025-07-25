using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Sync.Models.Dtos {

    /// <summary>
    /// 新增同步任务 DTO
    /// </summary>
    public class SyncTaskCreateDto {

        /// <summary>任务类型（如“手动同步”/“自动同步”）</summary>
        [Required(ErrorMessage = "任务类型不能为空")]
        [DisplayName("任务类型（如“手动同步”/“自动同步”）")]
        public string TaskType { get; set; } = string.Empty;

        /// <summary>任务状态（如“进行中”、“已完成”）</summary>
        [Required(ErrorMessage = "任务状态不能为空")]
        [DisplayName("任务状态（如“进行中”、“已完成”）")]
        public string Status { get; set; } = string.Empty;

        /// <summary>任务触发时间</summary>
        [DisplayName("任务触发时间")]
        public DateTime TriggerTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}