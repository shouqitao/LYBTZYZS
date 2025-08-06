using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Queueing
{
    /// <summary>
    /// 排队项目实体 - 统一排队管理系统
    /// 用于管理整个诊疗流程的排队：挂号->看诊->缴费->药房->理疗
    /// </summary>
    [Table("QueueItems")]
    public class QueueItemModel
    {
        /// <summary>排队项目ID</summary>
        [Key]
        [DisplayName("排队项目ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [Required]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [Required]
        [StringLength(100)]
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>排队类型（看诊、缴费、药房、理疗）</summary>
        [Required]
        [StringLength(20)]
        [DisplayName("排队类型")]
        public QueueType QueueType { get; set; }

        /// <summary>队列号</summary>
        [DisplayName("队列号")]
        public int QueueNumber { get; set; }

        /// <summary>目标服务点ID（医生ID、收费台ID、药房ID、理疗室ID）</summary>
        [DisplayName("服务点ID")]
        public Guid? ServicePointId { get; set; }

        /// <summary>目标服务点名称</summary>
        [StringLength(100)]
        [DisplayName("服务点名称")]
        public string? ServicePointName { get; set; }

        /// <summary>排队时间</summary>
        [DisplayName("排队时间")]
        public DateTime QueueTime { get; set; } = DateTime.Now;

        /// <summary>叫号时间</summary>
        [DisplayName("叫号时间")]
        public DateTime? CallTime { get; set; }

        /// <summary>开始服务时间</summary>
        [DisplayName("开始服务时间")]
        public DateTime? StartServiceTime { get; set; }

        /// <summary>完成服务时间</summary>
        [DisplayName("完成服务时间")]
        public DateTime? CompleteServiceTime { get; set; }

        /// <summary>排队状态</summary>
        [DisplayName("排队状态")]
        public QueueItemStatus Status { get; set; } = QueueItemStatus.Waiting;

        /// <summary>优先级（0=普通，1=优先，2=急诊）</summary>
        [DisplayName("优先级")]
        public int Priority { get; set; } = 0;

        /// <summary>估计等待时间（分钟）</summary>
        [DisplayName("估计等待时间")]
        public int? EstimatedWaitTime { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>是否有效</summary>
        [DisplayName("是否有效")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 排队类型枚举
    /// </summary>
    public enum QueueType
    {
        /// <summary>看诊排队</summary>
        [Description("看诊排队")]
        Consultation = 1,

        /// <summary>缴费排队</summary>
        [Description("缴费排队")]
        Payment = 2,

        /// <summary>药房排队</summary>
        [Description("药房排队")]
        Pharmacy = 3,

        /// <summary>理疗排队</summary>
        [Description("理疗排队")]
        TreatmentRoom = 4
    }

    /// <summary>
    /// 排队项目状态枚举
    /// </summary>
    public enum QueueItemStatus
    {
        /// <summary>等待中</summary>
        [Description("等待中")]
        Waiting = 0,

        /// <summary>已叫号</summary>
        [Description("已叫号")]
        Called = 1,

        /// <summary>服务中</summary>
        [Description("服务中")]
        InService = 2,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 3,

        /// <summary>已跳过</summary>
        [Description("已跳过")]
        Skipped = 4,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 5
    }
}