using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Queueing {

    /// <summary>
    /// 排队列表 DTO
    /// </summary>
    public class QueueingDto {

        /// <summary>排队ID</summary>
        [DisplayName("排队ID")]
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>排队类型</summary>
        [DisplayName("排队类型")]
        public string QueueType { get; set; } = string.Empty;

        /// <summary>排队时间</summary>
        [DisplayName("排队时间")]
        public DateTime QueueTime { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public string Status { get; set; } = string.Empty;
    }
}