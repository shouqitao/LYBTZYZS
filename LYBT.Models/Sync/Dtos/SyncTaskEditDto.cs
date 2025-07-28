using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Sync {

    /// <summary>
    /// 编辑同步任务 DTO
    /// </summary>
    public class SyncTaskEditDto {

        /// <summary>同步任务ID</summary>
        [Required(ErrorMessage = "同步任务ID不能为空")]
        [DisplayName("同步任务ID")]
        public Guid Id { get; set; }

        /// <summary>任务状态</summary>
        [Required(ErrorMessage = "任务状态不能为空")]
        [DisplayName("任务状态")]
        public string Status { get; set; } = string.Empty;

        /// <summary>实际执行时间</summary>
        [DisplayName("实际执行时间")]
        public DateTime? ExecuteTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}