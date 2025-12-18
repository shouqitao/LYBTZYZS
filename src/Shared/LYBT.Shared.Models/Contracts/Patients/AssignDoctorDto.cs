using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 分配医生DTO - 前后端共享API契约
    /// 用于为患者分配主治医生的请求模型
    /// </summary>
    public class AssignDoctorDto
    {

        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>分配原因</summary>
        [StringLength(200, ErrorMessage = "分配原因长度不能超过200个字符")]
        [DisplayName("分配原因")]
        public string? Reason { get; set; }

        /// <summary>分配时间</summary>
        [DisplayName("分配时间")]
        public DateTime AssignTime { get; set; } = DateTime.Now;

        /// <summary>是否设为主治医生</summary>
        [DisplayName("是否主治医生")]
        public bool IsPrimary { get; set; } = true;
    }
}
