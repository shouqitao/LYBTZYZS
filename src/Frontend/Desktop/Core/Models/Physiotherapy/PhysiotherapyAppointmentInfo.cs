using System;

namespace LYBT.WPF.Client.Core.Models.Physiotherapy
{
    /// <summary>
    /// 理疗预约信息
    /// </summary>
    public class PhysiotherapyAppointmentInfo
    {
        /// <summary>
        /// 预约ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 理疗类型
        /// </summary>
        public string TreatmentType { get; set; } = string.Empty;

        /// <summary>
        /// 理疗项目名称
        /// </summary>
        public string TreatmentName { get; set; } = string.Empty;

        /// <summary>
        /// 预约时间
        /// </summary>
        public DateTime AppointmentTime { get; set; }

        /// <summary>
        /// 理疗师
        /// </summary>
        public string TherapistName { get; set; } = string.Empty;

        /// <summary>
        /// 理疗师ID
        /// </summary>
        public Guid TherapistId { get; set; }

        /// <summary>
        /// 预约状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}