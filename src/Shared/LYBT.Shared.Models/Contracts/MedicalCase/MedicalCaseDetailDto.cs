using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.TreatmentPlan;
using LYBT.Shared.Models.Contracts.Cashier;
using LYBT.Shared.Models.Contracts.Pharmacy;
using LYBT.Shared.Models.Contracts.TreatmentRoom;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医疗案例详情DTO
    /// </summary>
    public class MedicalCaseDetailDto
    {
        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>挂号信息</summary>
        [DisplayName("挂号信息")]
        public RegistrationDto? Registration { get; set; }

        /// <summary>看诊信息</summary>
        [DisplayName("看诊信息")]
        public ConsultationDetailDto? Consultation { get; set; }

        /// <summary>治疗方案</summary>
        [DisplayName("治疗方案")]
        public TreatmentPlanDto? TreatmentPlan { get; set; }

        /// <summary>收银信息</summary>
        [DisplayName("收银信息")]
        public CashierDto? Cashier { get; set; }

        /// <summary>药房信息</summary>
        [DisplayName("药房信息")]
        public PharmacyDto? Pharmacy { get; set; }

        /// <summary>治疗室信息</summary>
        [DisplayName("治疗室信息")]
        public TreatmentRoomDto? TreatmentRoom { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public MedicalCaseStatus Status { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>完成时间</summary>
        [DisplayName("完成时间")]
        public DateTime? CompleteTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 医疗案例状态枚举
    /// </summary>
    public enum MedicalCaseStatus
    {
        /// <summary>已挂号</summary>
        Registered = 0,

        /// <summary>看诊中</summary>
        InConsultation = 1,

        /// <summary>待付费</summary>
        WaitingPayment = 2,

        /// <summary>已付费</summary>
        Paid = 3,

        /// <summary>取药中</summary>
        InPharmacy = 4,

        /// <summary>理疗中</summary>
        InTreatment = 5,

        /// <summary>已完成</summary>
        Completed = 6,

        /// <summary>已取消</summary>
        Cancelled = 7
    }
}