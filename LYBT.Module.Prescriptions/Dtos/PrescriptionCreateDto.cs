using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums.Prescriptions;

namespace LYBT.Module.Prescriptions.Dtos {
    public class PrescriptionCreateDto {
        [Required]
        public Guid PatientId { get; set; }
        [Required]
        public Guid DoctorId { get; set; }
        public string? Diagnosis { get; set; }
        public string? Remark { get; set; }
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
        public List<PrescriptionItemCreateDto> Items { get; set; } = new();
    }

    public class PrescriptionItemCreateDto {
        [Required]
        public Guid HerbId { get; set; }
        [Required]
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Usage { get; set; }
    }
}
