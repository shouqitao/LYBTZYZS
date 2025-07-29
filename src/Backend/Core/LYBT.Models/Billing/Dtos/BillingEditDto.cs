using LYBT.Common.Enums.System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Billing {

    /// <summary>
    /// 编辑账单 DTO
    /// </summary>
    public class BillingEditDto {

        /// <summary>账单ID</summary>
        [Required(ErrorMessage = "账单ID不能为空")]
        [DisplayName("账单ID")]
        public Guid Id { get; set; }

        /// <summary>账单明细列表</summary>
        [DisplayName("账单明细列表")]
        public List<BillingItemDto> Items { get; set; } = new();

        /// <summary>账单总金额</summary>
        [DisplayName("账单总金额")]
        public decimal TotalAmount { get; set; }

        /// <summary>已缴金额</summary>
        [DisplayName("已缴金额")]
        public decimal PaidAmount { get; set; }

        /// <summary>账单状态</summary>
        [DisplayName("账单状态")]
        public BillingStatus Status { get; set; } = BillingStatus.Pending;

        /// <summary>
        /// 账单时间（如有二次缴费等场景可与 CreateTime 区分）
        /// </summary>
        [DisplayName("账单时间（如有二次缴费等场景可与 CreateTime 区分）")]
        public DateTime BillingTime { get; set; } = DateTime.Now;

        /// <summary>缴费方式</summary>
        [DisplayName("缴费方式")]
        public string? PaymentMethod { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}