using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Doctors.Dtos {
    /// <summary>
    /// 医生修改密码 DTO
    /// </summary>
    public class ChangePasswordDto {
        [Required]
        public Guid DoctorId { get; set; }

        [Required]
        [StringLength(32, MinimumLength = 6)]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(32, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
