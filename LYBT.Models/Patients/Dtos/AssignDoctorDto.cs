using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Patients {

    /// <summary>
    /// 给患者授权医生 DTO
    /// </summary>
    public class AssignDoctorDto {

        [Required]
        [DisplayName("DoctorId")]
        public Guid DoctorId { get; set; }
    }
}