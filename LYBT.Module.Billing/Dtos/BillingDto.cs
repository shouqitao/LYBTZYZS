using LYBT.Common.Enums;

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

        /// <summary>账单状态</summary>
        public BillingStatus Status { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>账单时间</summary>
        public DateTime BillingTime { get; set; }
    }
}