using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.TreatmentRoom {

    /// <summary>
    /// 治疗室单详情 DTO
    /// </summary>
    public class TreatmentRoomDetailDto {

        /// <summary>治疗室单ID</summary>
        [DisplayName("治疗室单ID")]
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>诊疗项目</summary>
        [DisplayName("诊疗项目")]
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>治疗次数</summary>
        [DisplayName("治疗次数")]
        public int Count { get; set; }

        /// <summary>治疗状态</summary>
        [DisplayName("治疗状态")]
        public int Status { get; set; }

        /// <summary>治疗开始时间</summary>
        [DisplayName("治疗开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>治疗结束时间</summary>
        [DisplayName("治疗结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}