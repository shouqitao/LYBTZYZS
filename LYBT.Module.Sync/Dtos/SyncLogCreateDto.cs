using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Sync.Dtos {
    /// <summary>
    /// 新增同步日志 DTO
    /// </summary>
    public class SyncLogCreateDto {
        /// <summary>同步类型（如“手动”、“自动”）</summary>
        [Required(ErrorMessage = "同步类型不能为空")]
        public string SyncType { get; set; } = string.Empty;

        /// <summary>同步状态（如“成功”、“失败”）</summary>
        [Required(ErrorMessage = "同步状态不能为空")]
        public string Status { get; set; } = string.Empty;

        /// <summary>同步时间</summary>
        public DateTime SyncTime { get; set; } = DateTime.Now;

        /// <summary>日志说明</summary>
        public string? Remark { get; set; }
    }
}
