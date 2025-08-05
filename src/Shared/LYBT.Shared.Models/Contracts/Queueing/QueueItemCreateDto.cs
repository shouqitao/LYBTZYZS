using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Queueing {

    /// <summary>
    /// 新增排队信息 DTO
    /// </summary>
    public class QueueingCreateDto {

        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>排队类型（如"普通"、"急诊"）</summary>
        [Required(ErrorMessage = "排队类型不能为空")]
        [DisplayName("排队类型（如\"普通\"、\"急诊\"）")]
        public string QueueType { get; set; } = "普通";

        /// <summary>排队时间</summary>
        [DisplayName("排队时间")]
        public DateTime QueueTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}