using LYBT.Common.Enums;
using LYBT.Common.Enums.System;
using System.ComponentModel;

namespace LYBT.Module.Billing.Models.Dtos {

    /// <summary>
    /// 账单列表 DTO
    /// </summary>
    public class BillingDto {

        /// <summary>账单ID</summary>
        [DisplayName("账单ID")]
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>账单总金额</summary>
        [DisplayName("账单总金额")]
        public decimal TotalAmount { get; set; }

        /// <summary>已缴金额</summary>
        [DisplayName("已缴金额")]
        public decimal PaidAmount { get; set; }

        /// <summary>账单状态</summary>
        [DisplayName("账单状态")]
        public BillingStatus Status { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; }

        /// <summary>账单时间</summary>
        [DisplayName("账单时间")]
        public DateTime BillingTime { get; set; }
    }
}