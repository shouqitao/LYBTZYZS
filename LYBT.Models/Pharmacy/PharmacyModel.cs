using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Common.Enums;

namespace LYBT.Models.Pharmacy {

    /// <summary>
    /// 药房任务模型
    /// </summary>
    public class PharmacyModel {

        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid TaskId { get; set; }

        [Required]
        public Guid PrescriptionId { get; set; }

        [Required]
        public Guid PatientId { get; set; }

        [Required]
        public List<HerbModel> Herbs { get; set; } = new();

        public bool NeedDecoction { get; set; }

        [Required]
        public PharmacyStatus Status { get; set; }

        [Required]
        public DateTime CreateTime { get; set; }

        [Required]
        public Guid DoctorId { get; set; }

        [Required]
        public Guid OperatorId { get; set; }

        public DateTime DispenseTime { get; set; }

        [StringLength(256)]
        public string? Remark { get; set; }
    }
}