using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Common.Enums.Prescriptions;

namespace LYBT.Models.Prescriptions {
    /// <summary>
    /// 处方主表实体
    /// </summary>
    public class PrescriptionModel {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PatientId { get; set; }

        [Required]
        public Guid DoctorId { get; set; }

        [Required]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [StringLength(256)]
        public string? Diagnosis { get; set; }

        [StringLength(256)]
        public string? Remark { get; set; }

        [Required]
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

        public List<PrescriptionItemModel> Items { get; set; } = new();
    }
}
