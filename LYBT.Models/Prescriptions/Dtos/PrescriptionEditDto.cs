using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums.Prescriptions;

namespace LYBT.Module.Prescriptions.Dtos {
    public class PrescriptionEditDto {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public Guid PatientId { get; set; }
        [Required]
        public Guid DoctorId { get; set; }
        public string? Diagnosis { get; set; }
        public string? Remark { get; set; }
        public PrescriptionStatus Status { get; set; }
        public List<PrescriptionItemCreateDto> Items { get; set; } = new();
    }
}
