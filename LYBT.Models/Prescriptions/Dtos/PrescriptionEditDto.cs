using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums.Prescriptions;
using System.ComponentModel;

namespace LYBT.Module.Prescriptions.Dtos {
/// <summary>
/// 表示PrescriptionEditDto。
/// </summary>
    public class PrescriptionEditDto {
        [Required]
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }
        [Required]
        [DisplayName("PatientId")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public Guid PatientId { get; set; }
        [Required]
        [DisplayName("DoctorId")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public Guid DoctorId { get; set; }
        [DisplayName("Diagnosis")]
/// <summary>
/// Diagnosis 属性。
/// </summary>
        public string? Diagnosis { get; set; }
        [DisplayName("Remark")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
        [DisplayName("Status")]
/// <summary>
/// Status 属性。
/// </summary>
        public PrescriptionStatus Status { get; set; }
        [DisplayName("Items")]
/// <summary>
/// Items 属性。
/// </summary>
        public List<PrescriptionItemCreateDto> Items { get; set; } = new();
    }
}
