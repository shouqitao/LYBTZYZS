using System;

namespace LYBT.Shared.Models.Frontend.Registration
{
    /// <summary>
    /// 挂号前端模型
    /// </summary>
    public class RegistrationInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 医生姓名
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 挂号类型
        /// </summary>
        public string RegistrationType { get; set; } = string.Empty;

        /// <summary>
        /// 挂号时间
        /// </summary>
        public DateTime RegistrationTime { get; set; }

        /// <summary>
        /// 预约时间
        /// </summary>
        public DateTime? AppointmentTime { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 挂号费
        /// </summary>
        public decimal RegistrationFee { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}