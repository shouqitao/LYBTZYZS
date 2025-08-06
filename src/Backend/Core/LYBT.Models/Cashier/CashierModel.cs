using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Cashier
{
    /// <summary>
    /// 收银实体 - 替代原BillingModel
    /// </summary>
    [Table("Cashiers")]
    public class CashierModel
    {
        /// <summary>收银记录ID</summary>
        [Key]
        [DisplayName("收银记录ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [Required]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>应收金额</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("应收金额")]
        public decimal TotalAmount { get; set; }

        /// <summary>优惠金额</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("优惠金额")]
        public decimal DiscountAmount { get; set; }

        /// <summary>实收金额</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("实收金额")]
        public decimal PaidAmount { get; set; }

        /// <summary>支付方式</summary>
        [DisplayName("支付方式")]
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>支付状态</summary>
        [DisplayName("支付状态")]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        /// <summary>收银员ID</summary>
        [DisplayName("收银员ID")]
        public Guid? CashierId { get; set; }

        /// <summary>支付时间</summary>
        [DisplayName("支付时间")]
        public DateTime? PaymentTime { get; set; }

        /// <summary>发票号</summary>
        [StringLength(50)]
        [DisplayName("发票号")]
        public string? InvoiceNumber { get; set; }

        /// <summary>支付凭证号</summary>
        [StringLength(100)]
        [DisplayName("支付凭证号")]
        public string? TransactionNumber { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>是否有效</summary>
        [DisplayName("是否有效")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 支付方式枚举
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>现金</summary>
        [Description("现金")]
        Cash = 0,

        /// <summary>支付宝</summary>
        [Description("支付宝")]
        Alipay = 1,

        /// <summary>微信支付</summary>
        [Description("微信支付")]
        WeChat = 2,

        /// <summary>银行卡</summary>
        [Description("银行卡")]
        BankCard = 3,

        /// <summary>医保</summary>
        [Description("医保")]
        MedicalInsurance = 4,

        /// <summary>混合支付</summary>
        [Description("混合支付")]
        Mixed = 5
    }

    /// <summary>
    /// 支付状态枚举
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>待支付</summary>
        [Description("待支付")]
        Pending = 0,

        /// <summary>已支付</summary>
        [Description("已支付")]
        Paid = 1,

        /// <summary>部分支付</summary>
        [Description("部分支付")]
        PartialPaid = 2,

        /// <summary>已退款</summary>
        [Description("已退款")]
        Refunded = 3,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 4
    }
}