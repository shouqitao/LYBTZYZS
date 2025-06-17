using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums;

namespace LYBT.Module.Sync.Dtos {
    /// <summary>
    /// 新增同步日志 DTO
    /// </summary>
    public class SyncLogCreateDto {
        /// <summary>同步模式</summary>
        [Required(ErrorMessage = "同步模式不能为空")]
        public SyncMode Mode { get; set; } = SyncMode.Auto;

        /// <summary>同步状态</summary>
        [Required(ErrorMessage = "同步状态不能为空")]
        public SyncStatus Status { get; set; } = SyncStatus.Success;

        /// <summary>同步时间</summary>
        public DateTime SyncTime { get; set; } = DateTime.Now;

        /// <summary>错误或成功信息</summary>
        public string? Message { get; set; }
    }
}
