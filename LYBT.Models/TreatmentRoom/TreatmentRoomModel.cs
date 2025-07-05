using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.TreatmentRoom {

    /// <summary>
    /// 治疗室任务记录
    /// </summary>
    public class TreatmentRoomModel {

        [Key]
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        public string ExecutionId { get; set; } = string.Empty;

        [Required, StringLength(64)]
        public string PlanId { get; set; } = string.Empty;

        [Required, StringLength(64)]
        public string PatientId { get; set; } = string.Empty;

        [Required, StringLength(32)]
        public string TreatmentType { get; set; } = string.Empty;

        public int ExecutedCount { get; set; }

        public int TotalCount { get; set; }

        [Required, StringLength(32)]
        public string Status { get; set; } = string.Empty;

        [StringLength(64)]
        public string Executor { get; set; } = string.Empty;

        public DateTime LastExecuteTime { get; set; }

        [StringLength(64)]
        public string DoctorId { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        [StringLength(64)]
        public string TreatmentItem { get; set; } = string.Empty;

        public DateTime EndTime { get; set; }

        [StringLength(256)]
        public string? Remark { get; set; }

        public int Count { get; set; }
    }
}