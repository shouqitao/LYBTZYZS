using LYBT.Common.Enums.Diagnostics;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Prescriptions.Models.Dtos {

    /// <summary>
    /// 表示PrescriptionCreateDto。
    /// </summary>
    public class PrescriptionCreateDto {

        [Required]
        [DisplayName("PatientId")]
        public Guid PatientId { get; set; }

        [Required]
        [DisplayName("DoctorId")]
        public Guid DoctorId { get; set; }

        [DisplayName("Diagnosis")]
        public string? Diagnosis { get; set; }

        [DisplayName("Remark")]
        public string? Remark { get; set; }

        [DisplayName("Status")]
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

        [DisplayName("Items")]
        public List<PrescriptionItemCreateDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 表示PrescriptionItemCreateDto。
    /// </summary>
    public class PrescriptionItemCreateDto {

        [Required]
        [DisplayName("HerbId")]
        public Guid HerbId { get; set; }

        [Required]
        [DisplayName("HerbName")]
        public string HerbName { get; set; } = string.Empty;

        [DisplayName("Quantity")]
        public decimal Quantity { get; set; }

        [DisplayName("Unit")]
        public string? Unit { get; set; }

        [DisplayName("Usage")]
        public string? Usage { get; set; }
    }
}