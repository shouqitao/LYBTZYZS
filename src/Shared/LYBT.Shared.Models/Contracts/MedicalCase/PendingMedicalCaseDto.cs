using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 待看诊队列项DTO
    /// Epic #1583 - Phase 5: Server端API
    /// 用于患者选择界面的待看诊队列显示
    /// </summary>
    public class PendingMedicalCaseDto
    {
        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>手机号（原始）</summary>
        [DisplayName("手机号")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>手机号（脱敏）</summary>
        [DisplayName("手机号脱敏")]
        public string PhoneMasked { get; set; } = string.Empty;

        /// <summary>待处理类型</summary>
        [DisplayName("类型")]
        public PendingCaseType Type { get; set; }

        /// <summary>医案ID（如果有未完成医案，则有值；挂号患者为null）</summary>
        [DisplayName("医案ID")]
        public Guid? MedicalCaseId { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; }
    }
}
