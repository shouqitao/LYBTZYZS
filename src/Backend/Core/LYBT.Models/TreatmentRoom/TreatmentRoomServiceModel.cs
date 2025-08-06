using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.TreatmentRoom
{
    /// <summary>
    /// 治疗室服务实体 - 表示患者在治疗室接受的服务
    /// </summary>
    [Table("TreatmentRoomServices")]
    public class TreatmentRoomServiceModel
    {
        /// <summary>治疗室服务ID</summary>
        [Key]
        [DisplayName("治疗室服务ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [Required]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>理疗项目ID列表（JSON存储）</summary>
        [DisplayName("理疗项目ID列表")]
        public string? PhysiotherapyItemIds { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public TreatmentRoomServiceStatus Status { get; set; } = TreatmentRoomServiceStatus.Pending;

        /// <summary>治疗师ID</summary>
        [DisplayName("治疗师ID")]
        public Guid? TherapistId { get; set; }

        /// <summary>治疗室ID</summary>
        [DisplayName("治疗室ID")]
        public Guid? RoomId { get; set; }

        /// <summary>治疗室号</summary>
        [StringLength(20)]
        [DisplayName("治疗室号")]
        public string? RoomNumber { get; set; }

        /// <summary>排队号</summary>
        [StringLength(20)]
        [DisplayName("排队号")]
        public string? QueueNumber { get; set; }

        /// <summary>预约时间</summary>
        [DisplayName("预约时间")]
        public DateTime? AppointmentTime { get; set; }

        /// <summary>开始治疗时间</summary>
        [DisplayName("开始治疗时间")]
        public DateTime? StartTime { get; set; }

        /// <summary>结束治疗时间</summary>
        [DisplayName("结束治疗时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>治疗记录</summary>
        [StringLength(1000)]
        [DisplayName("治疗记录")]
        public string? TreatmentNotes { get; set; }

        /// <summary>治疗效果</summary>
        [StringLength(500)]
        [DisplayName("治疗效果")]
        public string? TreatmentEffect { get; set; }

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
    /// 治疗室服务状态枚举
    /// </summary>
    public enum TreatmentRoomServiceStatus
    {
        /// <summary>待治疗</summary>
        [Description("待治疗")]
        Pending = 0,

        /// <summary>排队中</summary>
        [Description("排队中")]
        Queuing = 1,

        /// <summary>治疗中</summary>
        [Description("治疗中")]
        InTreatment = 2,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 3,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 4
    }
}