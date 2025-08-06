using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Models.Registration;
using LYBT.Models.Consultation;
using LYBT.Models.TreatmentPlan;
using LYBT.Models.Pharmacy;

namespace LYBT.Models.MedicalCase
{
    /// <summary>
    /// 医疗案例实体 - 包含整个诊疗流程
    /// </summary>
    [Table("MedicalCases")]
    public class MedicalCaseModel
    {
        /// <summary>医疗案例ID</summary>
        [Key]
        [DisplayName("医疗案例ID")]
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>主治医生ID</summary>
        [Required]
        [DisplayName("主治医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>科室</summary>
        [StringLength(50)]
        [DisplayName("科室")]
        public string Department { get; set; } = "中医内科";

        /// <summary>挂号信息</summary>
        [DisplayName("挂号信息")]
        public virtual RegistrationModel? Registration { get; set; }

        /// <summary>挂号ID</summary>
        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }

        /// <summary>看诊信息</summary>
        [DisplayName("看诊信息")]
        public virtual ConsultationModel? Consultation { get; set; }

        /// <summary>看诊ID</summary>
        [DisplayName("看诊ID")]
        public Guid? ConsultationId { get; set; }

        /// <summary>治疗方案</summary>
        [DisplayName("治疗方案")]
        public virtual TreatmentPlanModel? TreatmentPlan { get; set; }

        /// <summary>治疗方案ID</summary>
        [DisplayName("治疗方案ID")]
        public Guid? TreatmentPlanId { get; set; }

        /// <summary>收银ID（预留）</summary>
        [DisplayName("收银ID")]
        public Guid? CashierId { get; set; }

        /// <summary>药房服务ID</summary>
        [DisplayName("药房服务ID")]
        public Guid? PharmacyId { get; set; }

        /// <summary>治疗室服务ID（预留）</summary>
        [DisplayName("治疗室服务ID")]
        public Guid? TreatmentRoomServiceId { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Registered;

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

        /// <summary>完成时间</summary>
        [DisplayName("完成时间")]
        public DateTime? CompleteTime { get; set; }

        /// <summary>是否有效</summary>
        [DisplayName("是否有效")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 医疗案例状态枚举
    /// </summary>
    public enum MedicalCaseStatus
    {
        /// <summary>已挂号</summary>
        [Description("已挂号")]
        Registered = 0,

        /// <summary>看诊中</summary>
        [Description("看诊中")]
        InConsultation = 1,

        /// <summary>待付费</summary>
        [Description("待付费")]
        WaitingPayment = 2,

        /// <summary>已付费</summary>
        [Description("已付费")]
        Paid = 3,

        /// <summary>取药中</summary>
        [Description("取药中")]
        InPharmacy = 4,

        /// <summary>理疗中</summary>
        [Description("理疗中")]
        InTreatment = 5,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 6,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 7
    }
}