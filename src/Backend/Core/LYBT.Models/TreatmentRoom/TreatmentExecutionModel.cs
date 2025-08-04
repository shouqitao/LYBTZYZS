using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Models.TreatmentRoom
{
    /// <summary>
    /// 理疗执行记录实体 - 数据库映射
    /// </summary>
    public class TreatmentExecutionModel
    {
        /// <summary>执行记录唯一标识</summary>
        [Key]
        [DisplayName("执行记录ID")]
        public Guid Id { get; set; }

        /// <summary>执行编号</summary>
        [Required]
        [MaxLength(50)]
        [DisplayName("执行编号")]
        public string ExecutionNumber { get; set; } = string.Empty;

        /// <summary>关联的病历ID</summary>
        [Required]
        [DisplayName("病历ID")]
        public Guid RecordId { get; set; }

        /// <summary>关联的理疗项目ID</summary>
        [Required]
        [DisplayName("理疗项目ID")]
        public Guid TreatmentCatalogId { get; set; }

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [Required]
        [MaxLength(50)]
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>开单医生ID</summary>
        [Required]
        [DisplayName("开单医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>开单医生姓名</summary>
        [Required]
        [MaxLength(50)]
        [DisplayName("开单医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>理疗师ID</summary>
        [DisplayName("理疗师ID")]
        public Guid? TherapistId { get; set; }

        /// <summary>理疗师姓名</summary>
        [MaxLength(50)]
        [DisplayName("理疗师姓名")]
        public string? TherapistName { get; set; }

        /// <summary>执行状态</summary>
        [Required]
        [DisplayName("执行状态")]
        public TreatmentExecutionStatus Status { get; set; } = TreatmentExecutionStatus.Pending;

        /// <summary>预约时间</summary>
        [DisplayName("预约时间")]
        public DateTime? AppointmentTime { get; set; }

        /// <summary>开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime? StartTime { get; set; }

        /// <summary>结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>实际时长(分钟)</summary>
        [DisplayName("实际时长")]
        public int? ActualDuration { get; set; }

        /// <summary>治疗部位</summary>
        [MaxLength(200)]
        [DisplayName("治疗部位")]
        public string? TreatmentArea { get; set; }

        /// <summary>治疗方法</summary>
        [MaxLength(500)]
        [DisplayName("治疗方法")]
        public string? TreatmentMethod { get; set; }

        /// <summary>治疗效果</summary>
        [MaxLength(500)]
        [DisplayName("治疗效果")]
        public string? TreatmentEffect { get; set; }

        /// <summary>患者反馈</summary>
        [MaxLength(500)]
        [DisplayName("患者反馈")]
        public string? PatientFeedback { get; set; }

        /// <summary>注意事项</summary>
        [MaxLength(500)]
        [DisplayName("注意事项")]
        public string? Precautions { get; set; }

        /// <summary>备注</summary>
        [MaxLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>费用</summary>
        [DisplayName("费用")]
        public decimal Fee { get; set; }

        /// <summary>是否已收费</summary>
        [DisplayName("是否已收费")]
        public bool IsPaid { get; set; }

        /// <summary>取消原因</summary>
        [MaxLength(200)]
        [DisplayName("取消原因")]
        public string? CancelReason { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>创建人</summary>
        [MaxLength(50)]
        [DisplayName("创建人")]
        public string? CreatedBy { get; set; }

        /// <summary>更新人</summary>
        [MaxLength(50)]
        [DisplayName("更新人")]
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// 理疗执行状态枚举
    /// </summary>
    public enum TreatmentExecutionStatus
    {
        /// <summary>待执行</summary>
        [Description("待执行")]
        Pending = 0,

        /// <summary>已预约</summary>
        [Description("已预约")]
        Appointed = 1,

        /// <summary>执行中</summary>
        [Description("执行中")]
        InProgress = 2,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 3,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 4,

        /// <summary>未到</summary>
        [Description("未到")]
        NoShow = 5
    }
}