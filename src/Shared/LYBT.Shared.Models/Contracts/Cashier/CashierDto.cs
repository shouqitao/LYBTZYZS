using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Cashier
{
    /// <summary>
    /// 收银信息DTO
    /// </summary>
    public class CashierDto
    {
        /// <summary>收银记录ID</summary>
        [DisplayName("收银记录ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>应收金额</summary>
        [DisplayName("应收金额")]
        public decimal TotalAmount { get; set; }

        /// <summary>优惠金额</summary>
        [DisplayName("优惠金额")]
        public decimal DiscountAmount { get; set; }

        /// <summary>实收金额</summary>
        [DisplayName("实收金额")]
        public decimal PaidAmount { get; set; }

        /// <summary>支付方式</summary>
        [DisplayName("支付方式")]
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>支付状态</summary>
        [DisplayName("支付状态")]
        public PaymentStatus PaymentStatus { get; set; }

        /// <summary>收银员ID</summary>
        [DisplayName("收银员ID")]
        public Guid? CashierId { get; set; }

        /// <summary>收银员姓名</summary>
        [DisplayName("收银员姓名")]
        public string? CashierName { get; set; }

        /// <summary>支付时间</summary>
        [DisplayName("支付时间")]
        public DateTime? PaymentTime { get; set; }

        /// <summary>发票号</summary>
        [DisplayName("发票号")]
        public string? InvoiceNumber { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 支付方式枚举
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>现金</summary>
        Cash = 0,

        /// <summary>支付宝</summary>
        Alipay = 1,

        /// <summary>微信支付</summary>
        WeChat = 2,

        /// <summary>银行卡</summary>
        BankCard = 3,

        /// <summary>医保</summary>
        MedicalInsurance = 4,

        /// <summary>混合支付</summary>
        Mixed = 5
    }

    /// <summary>
    /// 支付状态枚举
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>待支付</summary>
        Pending = 0,

        /// <summary>已支付</summary>
        Paid = 1,

        /// <summary>部分支付</summary>
        PartialPaid = 2,

        /// <summary>已退款</summary>
        Refunded = 3,

        /// <summary>已取消</summary>
        Cancelled = 4
    }
}