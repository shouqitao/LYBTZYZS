using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums.Prescriptions;
using System.ComponentModel;

namespace LYBT.Module.Prescriptions.Dtos {
    public class PrescriptionEditDto {
        [Required]
        [DisplayName("Id")]
        public Guid Id { get; set; }
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
        public PrescriptionStatus Status { get; set; }
        [DisplayName("Items")]
        public List<PrescriptionItemCreateDto> Items { get; set; } = new();
    }
}
