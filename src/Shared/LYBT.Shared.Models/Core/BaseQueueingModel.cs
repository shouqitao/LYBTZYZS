using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 排队基础模型 - 前后端共享核心字段
    /// 包含所有通用的排队信息字段，各层可基于此模型扩展
    /// </summary>
    public class BaseQueueingModel {

        /// <summary>排队唯一标识</summary>
        [DisplayName("排队ID")]
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>排队类型</summary>
        [DisplayName("排队类型")]
        public string QueueType { get; set; } = "普通";

        /// <summary>排队时间</summary>
        [DisplayName("排队时间")]
        public DateTime QueueTime { get; set; } = DateTime.Now;

        /// <summary>当前状态</summary>
        [DisplayName("当前状态")]
        public QueueStatus Status { get; set; } = QueueStatus.Waiting;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}