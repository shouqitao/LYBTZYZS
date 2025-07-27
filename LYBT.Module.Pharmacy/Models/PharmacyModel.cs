using LYBT.Common.Enums;
using LYBT.Common.Enums.System;
using LYBT.Module.Herbs.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Pharmacy.Models {

    /// <summary>
    /// 药房任务模型
    /// </summary>
    public class PharmacyModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required]
        [DisplayName("TaskId")]
        public Guid TaskId { get; set; }

        [Required]
        [DisplayName("PrescriptionId")]
        public Guid PrescriptionId { get; set; }

        [Required]
        [DisplayName("PatientId")]
        public Guid PatientId { get; set; }

        [Required]
        [DisplayName("Herbs")]
        public List<HerbModel> Herbs { get; set; } = new();

        [DisplayName("NeedDecoction")]
        public bool NeedDecoction { get; set; }

        [Required]
        [DisplayName("Status")]
        public PharmacyStatus Status { get; set; }

        [Required]
        [DisplayName("CreateTime")]
        public DateTime CreateTime { get; set; }

        [Required]
        [DisplayName("DoctorId")]
        public Guid DoctorId { get; set; }

        [Required]
        [DisplayName("OperatorId")]
        public Guid OperatorId { get; set; }

        [DisplayName("DispenseTime")]
        public DateTime DispenseTime { get; set; }

        [StringLength(256)]
        [DisplayName("Remark")]
        public string? Remark { get; set; }
    }
}