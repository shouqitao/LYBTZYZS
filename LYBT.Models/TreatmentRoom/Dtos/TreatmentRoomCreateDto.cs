using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.TreatmentRoom.Dtos {

    /// <summary>
    /// 新增治疗室单 DTO
    /// </summary>
    public class TreatmentRoomCreateDto {

        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
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
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>诊疗项目</summary>
        [Required(ErrorMessage = "诊疗项目不能为空")]
        [DisplayName("诊疗项目")]
/// <summary>
/// TreatmentItem 属性。
/// </summary>
        public string TreatmentItem { get; set; } = string.Empty;

        /// <summary>治疗次数</summary>
        [Range(1, int.MaxValue, ErrorMessage = "次数必须大于0")]
        [DisplayName("治疗次数")]
/// <summary>
/// Count 属性。
/// </summary>
        public int Count { get; set; } = 1;

        /// <summary>治疗状态（0待治疗、1已完成）</summary>
        [DisplayName("治疗状态（0待治疗、1已完成）")]
/// <summary>
/// Status 属性。
/// </summary>
        public int Status { get; set; } = 0;

        /// <summary>治疗开始时间</summary>
        [DisplayName("治疗开始时间")]
/// <summary>
/// StartTime 属性。
/// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
