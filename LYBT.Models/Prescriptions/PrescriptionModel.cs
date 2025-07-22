using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Common.Enums.Prescriptions;
using System.ComponentModel;

namespace LYBT.Models.Prescriptions {
    /// <summary>
    /// 处方主表实体
    /// </summary>
    public class PrescriptionModel {
        [Key]
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

        [Required]
        [DisplayName("CreateTime")]
/// <summary>
/// CreateTime 属性。
/// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [StringLength(256)]
        [DisplayName("Diagnosis")]
/// <summary>
/// Diagnosis 属性。
/// </summary>
        public string? Diagnosis { get; set; }

        [StringLength(256)]
        [DisplayName("Remark")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }

        [Required]
        [DisplayName("Status")]
/// <summary>
/// Status 属性。
/// </summary>
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

        [DisplayName("Items")]
/// <summary>
/// Items 属性。
/// </summary>
        public List<PrescriptionItemModel> Items { get; set; } = new();
    }
}
