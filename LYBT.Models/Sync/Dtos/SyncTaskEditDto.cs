using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Sync.Dtos {

    /// <summary>
    /// 编辑同步任务 DTO
    /// </summary>
    public class SyncTaskEditDto {

        /// <summary>同步任务ID</summary>
        [Required(ErrorMessage = "同步任务ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>任务状态</summary>
        [Required(ErrorMessage = "任务状态不能为空")]
        public string Status { get; set; } = string.Empty;

        /// <summary>实际执行时间</summary>
        public DateTime? ExecuteTime { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}