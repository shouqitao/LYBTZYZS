using LYBT.Common.Enums.Prescriptions;
using System.ComponentModel;

namespace LYBT.Module.Prescriptions.Models.Dtos {

    /// <summary>
    /// 处方列表 DTO
    /// </summary>
    public class PrescriptionDto {

        [DisplayName("Id")]
        public Guid Id { get; set; }

        [DisplayName("PatientId")]
        public Guid PatientId { get; set; }

        [DisplayName("DoctorId")]
        public Guid DoctorId { get; set; }

        [DisplayName("CreateTime")]
        public DateTime CreateTime { get; set; }

        [DisplayName("Status")]
        public PrescriptionStatus Status { get; set; }
    }
}