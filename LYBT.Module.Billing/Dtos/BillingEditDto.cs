using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Billing.Dtos {
    /// <summary>
    /// 编辑账单 DTO
    /// </summary>
    public class BillingEditDto {
        /// <summary>账单ID</summary>
        [Required(ErrorMessage = "账单ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>账单明细列表</summary>
        public List<BillingItemDto> Items { get; set; } = new();

        /// <summary>账单总金额</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>已缴金额</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>缴费状态</summary>
        public string PaymentStatus { get; set; } = "未缴费";

        /// <summary>
        /// 账单时间（如有二次缴费等场景可与 CreateTime 区分）
        /// </summary>
        public DateTime BillingTime { get; set; } = DateTime.Now;

        /// <summary>缴费方式</summary>
        public string? PaymentMethod { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
