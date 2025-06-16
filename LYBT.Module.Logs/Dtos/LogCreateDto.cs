using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Logs.Dtos {
    /// <summary>
    /// 新增操作日志 DTO
    /// </summary>
    public class LogCreateDto {
        /// <summary>日志类型（如“操作”、“系统”）</summary>
        [Required(ErrorMessage = "日志类型不能为空")]
        public string LogType { get; set; } = string.Empty;

        /// <summary>操作内容</summary>
        [Required(ErrorMessage = "操作内容不能为空")]
        public string Content { get; set; } = string.Empty;

        /// <summary>操作人姓名</summary>
        public string? OperatorName { get; set; }

        /// <summary>日志记录时间</summary>
        public DateTime LogTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
