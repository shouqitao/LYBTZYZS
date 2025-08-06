using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 创建医疗案例DTO
    /// </summary>
    public class MedicalCaseCreateDto
    {
        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        public Guid PatientId { get; set; }

        /// <summary>挂号ID（可选，如果已有挂号）</summary>
        public Guid? RegistrationId { get; set; }

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        public Guid DoctorId { get; set; }

        /// <summary>科室</summary>
        [StringLength(50, ErrorMessage = "科室名称长度不能超过50个字符")]
        public string Department { get; set; } = "中医内科";

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
    }
}