using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Common.Enums;
using System.ComponentModel;
using LYBT.Models.Herbs;

namespace LYBT.Models.Pharmacy {

    /// <summary>
    /// 药房任务模型
    /// </summary>
    public class PharmacyModel {

        [Key]
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        [Required]
        [DisplayName("TaskId")]
/// <summary>
/// TaskId 属性。
/// </summary>
        public Guid TaskId { get; set; }

        [Required]
        [DisplayName("PrescriptionId")]
/// <summary>
/// PrescriptionId 属性。
/// </summary>
        public Guid PrescriptionId { get; set; }

        [Required]
        [DisplayName("PatientId")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public Guid PatientId { get; set; }

        [Required]
        [DisplayName("Herbs")]
/// <summary>
/// Herbs 属性。
/// </summary>
        public List<HerbModel> Herbs { get; set; } = new();

        [DisplayName("NeedDecoction")]
/// <summary>
/// NeedDecoction 属性。
/// </summary>
        public bool NeedDecoction { get; set; }

        [Required]
        [DisplayName("Status")]
/// <summary>
/// Status 属性。
/// </summary>
        public PharmacyStatus Status { get; set; }

        [Required]
        [DisplayName("CreateTime")]
/// <summary>
/// CreateTime 属性。
/// </summary>
        public DateTime CreateTime { get; set; }

        [Required]
        [DisplayName("DoctorId")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public Guid DoctorId { get; set; }

        [Required]
        [DisplayName("OperatorId")]
/// <summary>
/// OperatorId 属性。
/// </summary>
        public Guid OperatorId { get; set; }

        [DisplayName("DispenseTime")]
/// <summary>
/// DispenseTime 属性。
/// </summary>
        public DateTime DispenseTime { get; set; }

        [StringLength(256)]
        [DisplayName("Remark")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; }
    }
}
