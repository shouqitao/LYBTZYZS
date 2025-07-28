using LYBT.Common.Enums.Prescriptions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Prescriptions {

    /// <summary>
    /// 处方主表实体
    /// </summary>
    public class PrescriptionModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required]
        [DisplayName("PatientId")]
        public Guid PatientId { get; set; }

        [Required]
        [DisplayName("DoctorId")]
        public Guid DoctorId { get; set; }

        [Required]
        [DisplayName("CreateTime")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [StringLength(256)]
        [DisplayName("Diagnosis")]
        public string? Diagnosis { get; set; }

        [StringLength(256)]
        [DisplayName("Remark")]
        public string? Remark { get; set; }

        [Required]
        [DisplayName("Status")]
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

        [DisplayName("Items")]
        public List<PrescriptionItemModel> Items { get; set; } = new();
    }
}