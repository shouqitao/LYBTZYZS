using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Cashier
{
    /// <summary>
    /// 更新收银记录DTO
    /// </summary>
    public class CashierUpdateDto
    {
        /// <summary>支付状态</summary>
        public PaymentStatus? PaymentStatus { get; set; }

        /// <summary>实收金额</summary>
        [Range(0, 999999.99, ErrorMessage = "实收金额必须在0-999999.99之间")]
        public decimal? PaidAmount { get; set; }

        /// <summary>支付时间</summary>
        public DateTime? PaymentTime { get; set; }

        /// <summary>发票号</summary>
        [StringLength(50, ErrorMessage = "发票号长度不能超过50个字符")]
        public string? InvoiceNumber { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
    }
}