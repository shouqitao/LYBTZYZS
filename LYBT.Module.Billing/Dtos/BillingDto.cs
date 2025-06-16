using System;

namespace LYBT.Module.Billing.Dtos {
    /// <summary>
    /// 账单列表 DTO
    /// </summary>
    public class BillingDto {
        /// <summary>账单ID</summary>
        public Guid Id { get; set; }

        /// <summary>病人姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>账单总金额</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>已缴金额</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>缴费状态</summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>账单时间</summary>
        public DateTime BillingTime { get; set; }
    }
}
