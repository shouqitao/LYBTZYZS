using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums.Prescriptions;
using System.ComponentModel;

namespace LYBT.Module.Prescriptions.Dtos {
/// <summary>
/// 表示PrescriptionCreateDto。
/// </summary>
    public class PrescriptionCreateDto {
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
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
        [DisplayName("Items")]
/// <summary>
/// Items 属性。
/// </summary>
        public List<PrescriptionItemCreateDto> Items { get; set; } = new();
    }

/// <summary>
/// 表示PrescriptionItemCreateDto。
/// </summary>
    public class PrescriptionItemCreateDto {
        [Required]
        [DisplayName("HerbId")]
/// <summary>
/// HerbId 属性。
/// </summary>
        public Guid HerbId { get; set; }
        [Required]
        [DisplayName("HerbName")]
/// <summary>
/// HerbName 属性。
/// </summary>
        public string HerbName { get; set; } = string.Empty;
        [DisplayName("Quantity")]
/// <summary>
/// Quantity 属性。
/// </summary>
        public decimal Quantity { get; set; }
        [DisplayName("Unit")]
/// <summary>
/// Unit 属性。
/// </summary>
        public string? Unit { get; set; }
        [DisplayName("Usage")]
/// <summary>
/// Usage 属性。
/// </summary>
        public string? Usage { get; set; }
    }
}
