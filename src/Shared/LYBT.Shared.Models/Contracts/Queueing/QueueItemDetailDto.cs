using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Queueing {

    /// <summary>
    /// 排队详情 DTO
    /// </summary>
    public class QueueingDetailDto {

        /// <summary>排队ID</summary>
        [DisplayName("排队ID")]
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>排队类型</summary>
        [DisplayName("排队类型")]
        public string QueueType { get; set; } = string.Empty;

        /// <summary>排队时间</summary>
        [DisplayName("排队时间")]
        public DateTime QueueTime { get; set; }

        /// <summary>当前状态（如"排队中"、"已叫号"、"已就诊"、"已取消"）</summary>
        [DisplayName("当前状态（如\"排队中\"、\"已叫号\"、\"已就诊\"、\"已取消\"）")]
        public string Status { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}