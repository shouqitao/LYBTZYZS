using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Patients.Dtos {

    /// <summary>
    /// 给患者授权医生 DTO
    /// </summary>
    public class AssignDoctorDto {

        [Required]
        [DisplayName("DoctorId")]
        public Guid DoctorId { get; set; }
    }
}