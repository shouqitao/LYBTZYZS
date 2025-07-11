using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Queueing.Dtos {

    /// <summary>
    /// 新增排队信息 DTO
    /// </summary>
    public class QueueingCreateDto {

        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>排队类型（如“普通”、“急诊”）</summary>
        [Required(ErrorMessage = "排队类型不能为空")]
        public string QueueType { get; set; } = "普通";

        /// <summary>排队时间</summary>
        public DateTime QueueTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}