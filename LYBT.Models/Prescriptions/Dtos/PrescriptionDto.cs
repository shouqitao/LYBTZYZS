using LYBT.Common.Enums.Prescriptions;
using System.ComponentModel;

namespace LYBT.Module.Prescriptions.Dtos {
    /// <summary>
    /// 处方列表 DTO
    /// </summary>
    public class PrescriptionDto {
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }
        [DisplayName("PatientId")]
/// <summary>
/// PatientId 属性。
/// </summary>
        public Guid PatientId { get; set; }
        [DisplayName("DoctorId")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public Guid DoctorId { get; set; }
        [DisplayName("CreateTime")]
/// <summary>
/// CreateTime 属性。
/// </summary>
        public DateTime CreateTime { get; set; }
        [DisplayName("Status")]
/// <summary>
/// Status 属性。
/// </summary>
        public PrescriptionStatus Status { get; set; }
    }
}
