using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Registration
{
    /// <summary>
    /// 创建挂号DTO
    /// </summary>
    public class RegistrationCreateDto
    {
        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        public Guid DoctorId { get; set; }

        /// <summary>挂号类型</summary>
        [Required(ErrorMessage = "挂号类型不能为空")]
        public RegistrationType RegistrationType { get; set; }

        /// <summary>挂号费用</summary>
        [Required(ErrorMessage = "挂号费用不能为空")]
        [Range(0, 9999.99, ErrorMessage = "挂号费用必须在0-9999.99之间")]
        public decimal RegistrationFee { get; set; }

        /// <summary>预约日期</summary>
        [Required(ErrorMessage = "预约日期不能为空")]
        public DateTime AppointmentDate { get; set; }

        /// <summary>预约时间段</summary>
        [Required(ErrorMessage = "预约时间段不能为空")]
        [StringLength(20, ErrorMessage = "时间段长度不能超过20个字符")]
        public string AppointmentTimeSlot { get; set; } = string.Empty;

        /// <summary>是否已支付</summary>
        public bool IsPaid { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
    }
}