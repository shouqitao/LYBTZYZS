using LYBT.Common.Enums.Prescriptions;

namespace LYBT.Module.Prescriptions.Dtos {
    /// <summary>
    /// 处方列表 DTO
    /// </summary>
    public class PrescriptionDto {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime CreateTime { get; set; }
        public PrescriptionStatus Status { get; set; }
    }
}
