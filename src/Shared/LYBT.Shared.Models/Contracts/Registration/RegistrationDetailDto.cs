using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Registration {

    /// <summary>
    /// 挂号详情 DTO
    /// </summary>
    public class RegistrationDetailDto {

        /// <summary>挂号ID</summary>
        [DisplayName("挂号ID")]
        public Guid Id { get; set; }

        /// <summary>挂号编号</summary>
        [DisplayName("挂号编号")]
        public string? RegistrationNumber { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>病人电话</summary>
        [DisplayName("病人电话")]
        public string? PatientPhone { get; set; }

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>挂号类型</summary>
        [DisplayName("挂号类型")]
        public string RegistrationType { get; set; } = string.Empty;

        /// <summary>挂号费用</summary>
        [DisplayName("挂号费用")]
        public decimal RegistrationFee { get; set; }

        /// <summary>挂号时间</summary>
        [DisplayName("挂号时间")]
        public DateTime RegistrationTime { get; set; }

        /// <summary>预约日期</summary>
        [DisplayName("预约日期")]
        public DateTime? AppointmentDate { get; set; }

        /// <summary>预约时间段</summary>
        [DisplayName("预约时间段")]
        public string? AppointmentTimeSlot { get; set; }

        /// <summary>状态（如"待看诊"、"已完成"、"已取消"）</summary>
        [DisplayName("状态（如\"待看诊\"、\"已完成\"、\"已取消\"）")]
        public string Status { get; set; } = string.Empty;

        /// <summary>队列号</summary>
        [DisplayName("队列号")]
        public int? QueueNumber { get; set; }

        /// <summary>是否已支付</summary>
        [DisplayName("是否已支付")]
        public bool IsPaid { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }
    }
}