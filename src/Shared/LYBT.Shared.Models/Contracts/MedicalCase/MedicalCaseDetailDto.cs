using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
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


        /// <summary>看诊信息</summary>
        [DisplayName("看诊信息")]
        public ConsultationDetailDto? Consultation { get; set; }


        // /// <summary>收银信息</summary> // 模块已删除
        // [DisplayName("收银信息")] // 模块已删除
        // public CashierDto? Cashier { get; set; } // 模块已删除

        // /// <summary>药房信息</summary> // 模块已删除
        // [DisplayName("药房信息")] // 模块已删除
        // public PharmacyDto? Pharmacy { get; set; } // 模块已删除


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

}