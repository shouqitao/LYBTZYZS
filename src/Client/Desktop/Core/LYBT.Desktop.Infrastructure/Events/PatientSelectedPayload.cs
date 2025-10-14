using System;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 患者选择事件负载
    /// </summary>
    public class PatientSelectedPayload
    {
        /// <summary>
        /// 患者唯一标识符
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; }

        /// <summary>
        /// 患者性别
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// 患者年龄
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// 患者手机号码
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 最后就诊日期
        /// </summary>
        public DateTime? LastVisitDate { get; set; }

        /// <summary>
        /// 就诊次数
        /// </summary>
        public int VisitCount { get; set; }

        /// <summary>
        /// 过敏史
        /// </summary>
        public string AllergyHistory { get; set; }

        /// <summary>
        /// 选择时间
        /// </summary>
        public DateTime SelectedAt { get; set; }
    }
}