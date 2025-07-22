using LYBT.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Sync.Dtos {

    /// <summary>
    /// 新增同步日志 DTO
    /// </summary>
    public class SyncLogCreateDto {

        /// <summary>同步模式</summary>
        [Required(ErrorMessage = "同步模式不能为空")]
        [DisplayName("同步模式")]
/// <summary>
/// Mode 属性。
/// </summary>
        public SyncMode Mode { get; set; } = SyncMode.Auto;

        /// <summary>同步状态</summary>
        [Required(ErrorMessage = "同步状态不能为空")]
        [DisplayName("同步状态")]
/// <summary>
/// Status 属性。
/// </summary>
        public SyncStatus Status { get; set; } = SyncStatus.Success;

        /// <summary>同步时间</summary>
        [DisplayName("同步时间")]
/// <summary>
/// SyncTime 属性。
/// </summary>
        public DateTime SyncTime { get; set; } = DateTime.Now;

        /// <summary>错误或成功信息</summary>
        [DisplayName("错误或成功信息")]
/// <summary>
/// Message 属性。
/// </summary>
        public string? Message { get; set; }
    }
}
