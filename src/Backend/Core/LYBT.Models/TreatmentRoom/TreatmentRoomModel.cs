using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.TreatmentRoom {

    /// <summary>
    /// 治疗室任务记录
    /// </summary>
    public class TreatmentRoomModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("ExecutionId")]
        public string ExecutionId { get; set; } = string.Empty;

        [Required, StringLength(64)]
        [DisplayName("PlanId")]
        public string PlanId { get; set; } = string.Empty;

        [Required, StringLength(64)]
        [DisplayName("PatientId")]
        public string PatientId { get; set; } = string.Empty;

        [Required, StringLength(32)]
        [DisplayName("TreatmentType")]
        public string TreatmentType { get; set; } = string.Empty;

        [DisplayName("ExecutedCount")]
        public int ExecutedCount { get; set; }

        [DisplayName("TotalCount")]
        public int TotalCount { get; set; }

        [Required, StringLength(32)]
        [DisplayName("Status")]
        public string Status { get; set; } = string.Empty;

        [StringLength(64)]
        [DisplayName("Executor")]
        public string Executor { get; set; } = string.Empty;

        [DisplayName("LastExecuteTime")]
        public DateTime LastExecuteTime { get; set; }

        [StringLength(64)]
        [DisplayName("DoctorId")]
        public string DoctorId { get; set; } = string.Empty;

        [DisplayName("StartTime")]
        public DateTime StartTime { get; set; }

        [StringLength(64)]
        [DisplayName("TreatmentItem")]
        public string TreatmentItem { get; set; } = string.Empty;

        [DisplayName("EndTime")]
        public DateTime EndTime { get; set; }

        [StringLength(256)]
        [DisplayName("Remark")]
        public string? Remark { get; set; }

        [DisplayName("Count")]
        public int Count { get; set; }
    }
}