using LYBT.Common.Enums;
using LYBT.Common.Enums.System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Sync.Models.Dtos {

    /// <summary>
    /// 新增同步日志 DTO
    /// </summary>
    public class SyncLogCreateDto {

        /// <summary>同步模式</summary>
        [Required(ErrorMessage = "同步模式不能为空")]
        [DisplayName("同步模式")]
        public SyncMode Mode { get; set; } = SyncMode.Auto;

        /// <summary>同步状态</summary>
        [Required(ErrorMessage = "同步状态不能为空")]
        [DisplayName("同步状态")]
        public SyncStatus Status { get; set; } = SyncStatus.Completed;

        /// <summary>同步时间</summary>
        [DisplayName("同步时间")]
        public DateTime SyncTime { get; set; } = DateTime.Now;

        /// <summary>错误或成功信息</summary>
        [DisplayName("错误或成功信息")]
        public string? Message { get; set; }
    }
}