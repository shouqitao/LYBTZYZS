using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Cashier
{
    /// <summary>
    /// 收银记录模型
    /// </summary>
    [Table("CashierRecords")]
    public class CashierRecord
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid MedicalCaseId { get; set; }

        [Required]
        public Guid PatientId { get; set; }

        [Required]
        public Guid CashierId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ChangeAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "已支付"; // 已支付、已退费、部分退费

        [Required]
        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }

        [StringLength(50)]
        public string? InvoiceNumber { get; set; }

        [StringLength(500)]
        public string? Remark { get; set; }

        // 退费相关字段
        [StringLength(500)]
        public string? RefundReason { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; } = 0;

        public DateTime? RefundTime { get; set; }

        [StringLength(50)]
        public string? RefundOperator { get; set; }

        // 导航属性
        public virtual ICollection<CashierItem> Items { get; set; } = new List<CashierItem>();
        public virtual ICollection<CashierPayment> Payments { get; set; } = new List<CashierPayment>();
    }

    /// <summary>
    /// 收银项目模型
    /// </summary>
    [Table("CashierItems")]
    public class CashierItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CashierRecordId { get; set; }

        [Required]
        [StringLength(50)]
        public string ItemType { get; set; } = string.Empty; // 挂号费、处方费、理疗费等

        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public Guid? SourceId { get; set; } // 关联的处方或治疗方案ID

        [StringLength(50)]
        public string? SourceType { get; set; } // Prescription、TreatmentPlan、Registration等

        [StringLength(500)]
        public string? Description { get; set; }

        // 导航属性
        public virtual CashierRecord CashierRecord { get; set; } = null!;
    }

    /// <summary>
    /// 收银支付方式模型
    /// </summary>
    [Table("CashierPayments")]
    public class CashierPayment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CashierRecordId { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = string.Empty; // 现金、支付宝、微信、医保

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? TransactionId { get; set; } // 交易流水号

        [StringLength(100)]
        public string? PaymentAccount { get; set; } // 支付账号

        [Required]
        public DateTime PaymentTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "成功"; // 成功、失败、退款

        // 导航属性
        public virtual CashierRecord CashierRecord { get; set; } = null!;
    }

    /// <summary>
    /// 日结对账模型
    /// </summary>
    [Table("DailySettlements")]
    public class DailySettlement
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public DateTime SettlementDate { get; set; }

        [Required]
        public Guid CashierId { get; set; }

        [Required]
        public int TotalTransactions { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "未结算"; // 未结算、已结算、已审核

        [Required]
        public DateTime CreateTime { get; set; }

        [StringLength(500)]
        public string? Remark { get; set; }

        // 支付方式分解JSON存储
        [Column(TypeName = "nvarchar(max)")]
        public string PaymentBreakdownJson { get; set; } = "{}";

        // 项目类型分解JSON存储
        [Column(TypeName = "nvarchar(max)")]
        public string ItemTypeBreakdownJson { get; set; } = "{}";
    }

    /// <summary>
    /// 发票模型
    /// </summary>
    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CashierRecordId { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string InvoiceType { get; set; } = "普通发票"; // 普通发票、专用发票

        [Required]
        [StringLength(500)]
        public string BuyerInfo { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string SellerInfo { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Required]
        public DateTime IssueTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "正常"; // 正常、作废、红冲

        [StringLength(500)]
        public string? PrintPath { get; set; }

        // 发票项目JSON存储
        [Column(TypeName = "nvarchar(max)")]
        public string ItemsJson { get; set; } = "[]";

        // 导航属性
        public virtual CashierRecord CashierRecord { get; set; } = null!;
    }
}