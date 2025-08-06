using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Cashier
{
    /// <summary>
    /// 创建收银记录DTO
    /// </summary>
    public class CashierCreateDto
    {
        /// <summary>医疗案例ID</summary>
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>应收金额</summary>
        [Required(ErrorMessage = "应收金额不能为空")]
        [Range(0.01, 999999.99, ErrorMessage = "应收金额必须在0.01-999999.99之间")]
        public decimal TotalAmount { get; set; }

        /// <summary>优惠金额</summary>
        [Range(0, 999999.99, ErrorMessage = "优惠金额必须在0-999999.99之间")]
        public decimal DiscountAmount { get; set; }

        /// <summary>实收金额</summary>
        [Required(ErrorMessage = "实收金额不能为空")]
        [Range(0, 999999.99, ErrorMessage = "实收金额必须在0-999999.99之间")]
        public decimal PaidAmount { get; set; }

        /// <summary>支付方式</summary>
        [Required(ErrorMessage = "支付方式不能为空")]
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>收银员ID</summary>
        public Guid? CashierId { get; set; }

        /// <summary>发票号</summary>
        [StringLength(50, ErrorMessage = "发票号长度不能超过50个字符")]
        public string? InvoiceNumber { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
    }
}