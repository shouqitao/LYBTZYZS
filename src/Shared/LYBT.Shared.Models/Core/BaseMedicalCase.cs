using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core
{
    /// <summary>
    /// 医疗案例基础模型 - 前后端共享核心字段
    /// 包含整个诊疗流程的基础信息
    /// </summary>
    public class BaseMedicalCase
    {
        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>关联用户ID（医生）</summary>
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>看诊ID</summary>
        [DisplayName("看诊ID")]
        public Guid? ConsultationId { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Registered;

        /// <summary>备注</summary>
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
}