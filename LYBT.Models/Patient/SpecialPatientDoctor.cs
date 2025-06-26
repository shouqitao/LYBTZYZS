using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Patient {

    /// <summary>
    /// 特殊患者-授权医生关系表
    /// </summary>
    public class SpecialPatientDoctor {

        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 患者ID
        /// </summary>
        [Required]
        public Guid PatientId { get; set; }

        /// <summary>
        /// 医生ID
        /// </summary>
        [Required]
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 授权时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}