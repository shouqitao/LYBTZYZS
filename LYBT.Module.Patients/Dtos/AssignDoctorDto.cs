using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Patients.Dtos {
    /// <summary>
    /// 给患者授权医生 DTO
    /// </summary>
    public class AssignDoctorDto {
        [Required]
        public Guid DoctorId { get; set; }
    }
}
