using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Models.TreatmentRoom {

    /// <summary>
    /// 治疗室任务记录
    /// </summary>
    public class TreatmentRoomModel {

        [Key]
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        [Required, StringLength(64)]
        [DisplayName("ExecutionId")]
/// <summary>
/// ExecutionId 属性。
/// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        [Required, StringLength(64)]
        [DisplayName("PlanId")]
/// <summary>
/// PlanId 属性。
/// </summary>
        public string PlanId { get; set; } = string.Empty;

        [Required, StringLength(64)]
        [DisplayName("PatientId")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public string PatientId { get; set; } = string.Empty;

        [Required, StringLength(32)]
        [DisplayName("TreatmentType")]
/// <summary>
/// TreatmentType 属性。
/// </summary>
        public string TreatmentType { get; set; } = string.Empty;

        [DisplayName("ExecutedCount")]
/// <summary>
/// ExecutedCount 属性。
/// </summary>
        public int ExecutedCount { get; set; }

        [DisplayName("TotalCount")]
/// <summary>
/// TotalCount 属性。
/// </summary>
        public int TotalCount { get; set; }

        [Required, StringLength(32)]
        [DisplayName("Status")]
/// <summary>
/// Status 属性。
/// </summary>
        public string Status { get; set; } = string.Empty;

        [StringLength(64)]
        [DisplayName("Executor")]
/// <summary>
/// Executor 属性。
/// </summary>
        public string Executor { get; set; } = string.Empty;

        [DisplayName("LastExecuteTime")]
/// <summary>
/// LastExecuteTime 属性。
/// </summary>
        public DateTime LastExecuteTime { get; set; }

        [StringLength(64)]
        [DisplayName("DoctorId")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public string DoctorId { get; set; } = string.Empty;

        [DisplayName("StartTime")]
/// <summary>
/// StartTime 属性。
/// </summary>
        public DateTime StartTime { get; set; }

        [StringLength(64)]
        [DisplayName("TreatmentItem")]
/// <summary>
/// TreatmentItem 属性。
/// </summary>
        public string TreatmentItem { get; set; } = string.Empty;

        [DisplayName("EndTime")]
/// <summary>
/// EndTime 属性。
/// </summary>
        public DateTime EndTime { get; set; }

        [StringLength(256)]
        [DisplayName("Remark")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }

        [DisplayName("Count")]
/// <summary>
/// Count 属性。
/// </summary>
        public int Count { get; set; }
    }
}
