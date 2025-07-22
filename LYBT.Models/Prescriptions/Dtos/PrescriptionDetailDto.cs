using LYBT.Common.Enums.Prescriptions;
using System.ComponentModel;

namespace LYBT.Module.Prescriptions.Dtos {
/// <summary>
/// 表示PrescriptionDetailDto。
/// </summary>
    public class PrescriptionDetailDto {
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }
        [DisplayName("PatientId")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public Guid PatientId { get; set; }
        [DisplayName("DoctorId")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public Guid DoctorId { get; set; }
        [DisplayName("CreateTime")]
/// <summary>
/// CreateTime 属性。
/// </summary>
        public DateTime CreateTime { get; set; }
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
        public List<PrescriptionItemDto> Items { get; set; } = new();
    }

/// <summary>
/// 表示PrescriptionItemDto。
/// </summary>
    public class PrescriptionItemDto {
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }
        [DisplayName("HerbId")]
/// <summary>
/// HerbId 属性。
/// </summary>
        public Guid HerbId { get; set; }
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
