using System;
using LYBT.Shared.Models.Frontend.Registration;
using LYBT.Shared.Models.Frontend.Consultation;
using LYBT.Shared.Models.Frontend.TreatmentPlan;
using LYBT.Shared.Models.Frontend.Cashier;
using LYBT.Shared.Models.Frontend.Pharmacy;
using LYBT.Shared.Models.Frontend.TreatmentRoom;

namespace LYBT.Shared.Models.Frontend.MedicalCase
{
    /// <summary>
    /// 医疗案例前端模型
    /// </summary>
    public class MedicalCaseInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 挂号ID
        /// </summary>
        public Guid RegistrationId { get; set; }

        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 医生姓名
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 状态显示名称
        /// </summary>
        public string StatusName { get; set; } = string.Empty;

        /// <summary>
        /// 诊断摘要
        /// </summary>
        public string DiagnosisSummary { get; set; } = string.Empty;

        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 支付状态
        /// </summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompleteTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 医疗案例详情前端模型
    /// </summary>
    public class MedicalCaseDetailInfo : MedicalCaseInfo
    {
        /// <summary>
        /// 挂号信息
        /// </summary>
        public RegistrationInfo? Registration { get; set; }

        /// <summary>
        /// 看诊信息
        /// </summary>
        public ConsultationInfo? Consultation { get; set; }

        /// <summary>
        /// 治疗方案
        /// </summary>
        public TreatmentPlanInfo? TreatmentPlan { get; set; }

        /// <summary>
        /// 收费信息
        /// </summary>
        public CashierInfo? Cashier { get; set; }

        /// <summary>
        /// 药房信息
        /// </summary>
        public PharmacyInfo? Pharmacy { get; set; }

        /// <summary>
        /// 理疗室信息
        /// </summary>
        public TreatmentRoomInfo? TreatmentRoom { get; set; }
    }
}