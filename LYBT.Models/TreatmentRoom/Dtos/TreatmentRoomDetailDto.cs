using System.ComponentModel;
namespace LYBT.Module.TreatmentRoom.Dtos {

    /// <summary>
    /// 治疗室单详情 DTO
    /// </summary>
    public class TreatmentRoomDetailDto {

        /// <summary>治疗室单ID</summary>
        [DisplayName("治疗室单ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
/// <summary>
/// PatientName 属性。
/// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
/// <summary>
/// DoctorName 属性。
/// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>诊疗项目</summary>
        [DisplayName("诊疗项目")]
/// <summary>
/// TreatmentItem 属性。
/// </summary>
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>治疗次数</summary>
        [DisplayName("治疗次数")]
/// <summary>
/// Count 属性。
/// </summary>
        public int Count { get; set; }

        /// <summary>治疗状态</summary>
        [DisplayName("治疗状态")]
/// <summary>
/// Status 属性。
/// </summary>
        public int Status { get; set; }

        /// <summary>治疗开始时间</summary>
        [DisplayName("治疗开始时间")]
/// <summary>
/// StartTime 属性。
/// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>治疗结束时间</summary>
        [DisplayName("治疗结束时间")]
/// <summary>
/// EndTime 属性。
/// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
