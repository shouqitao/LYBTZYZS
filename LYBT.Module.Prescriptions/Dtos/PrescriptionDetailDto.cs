using LYBT.Common.Enums.Prescriptions;

namespace LYBT.Module.Prescriptions.Dtos {
    public class PrescriptionDetailDto {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime CreateTime { get; set; }
        public string? Diagnosis { get; set; }
        public string? Remark { get; set; }
        public PrescriptionStatus Status { get; set; }
        public List<PrescriptionItemDto> Items { get; set; } = new();
    }

    public class PrescriptionItemDto {
        public Guid Id { get; set; }
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Usage { get; set; }
    }
}
