using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Cashier
{
    /// <summary>
    /// 收银记录基础DTO
    /// </summary>
    public class CashierRecordDto
    {
        public Guid Id { get; set; }
        public Guid MedicalCaseId { get; set; }
        public Guid PatientId { get; set; }
        public string? PatientName { get; set; }
        public Guid CashierId { get; set; }
        public string? CashierName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 收银记录详情DTO
    /// </summary>
    public class CashierRecordDetailDto : CashierRecordDto
    {
        public List<CashierItemDto> Items { get; set; } = new();
        public List<CashierPaymentDto> Payments { get; set; } = new();
        public string? RefundReason { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime? RefundTime { get; set; }
        public string? RefundOperator { get; set; }
    }

    /// <summary>
    /// 收银项目DTO
    /// </summary>
    public class CashierItemDto
    {
        public Guid Id { get; set; }
        public string ItemType { get; set; } = string.Empty; // 挂号费、处方费、理疗费等
        public string ItemName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public Guid? SourceId { get; set; } // 关联的处方或治疗方案ID
        public string? SourceType { get; set; } // Prescription、TreatmentPlan等
        public string? Description { get; set; }
    }

    /// <summary>
    /// 收银支付方式DTO
    /// </summary>
    public class CashierPaymentDto
    {
        public Guid Id { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // 现金、支付宝、微信、医保
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; } // 交易流水号
        public string? PaymentAccount { get; set; } // 支付账号
        public DateTime PaymentTime { get; set; }
        public string Status { get; set; } = string.Empty; // 成功、失败、退款
    }

    /// <summary>
    /// 创建收银记录DTO
    /// </summary>
    public class CashierRecordCreateDto
    {
        [Required]
        public Guid MedicalCaseId { get; set; }

        [Required]
        public Guid PatientId { get; set; }

        [Required]
        public List<CashierItemCreateDto> Items { get; set; } = new();

        [Required]
        public List<CashierPaymentCreateDto> Payments { get; set; } = new();

        [StringLength(500)]
        public string? Remark { get; set; }

        public bool PrintInvoice { get; set; } = true;
    }

    /// <summary>
    /// 创建收银项目DTO
    /// </summary>
    public class CashierItemCreateDto
    {
        [Required]
        [StringLength(50)]
        public string ItemType { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000)]
        public decimal UnitPrice { get; set; }

        [Required]
        [Range(0.01, 1000)]
        public decimal Quantity { get; set; }

        public Guid? SourceId { get; set; }

        [StringLength(50)]
        public string? SourceType { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 创建收银支付DTO
    /// </summary>
    public class CashierPaymentCreateDto
    {
        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000)]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(100)]
        public string? PaymentAccount { get; set; }
    }

    /// <summary>
    /// 退费DTO
    /// </summary>
    public class RefundRequestDto
    {
        [Required]
        public Guid CashierRecordId { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal RefundAmount { get; set; }

        [Required]
        [StringLength(500)]
        public string RefundReason { get; set; } = string.Empty;

        [StringLength(20)]
        public string RefundMethod { get; set; } = "原路退回";
    }

    /// <summary>
    /// 收银查询DTO
    /// </summary>
    public class CashierQueryDto
    {
        public Guid? PatientId { get; set; }
        public Guid? CashierId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchKeyword { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string OrderBy { get; set; } = "CreateTime";
        public bool IsAscending { get; set; } = false;
    }

    /// <summary>
    /// 收银统计DTO
    /// </summary>
    public class CashierStatisticsDto
    {
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal NetAmount { get; set; }
        public Dictionary<string, decimal> PaymentMethodStats { get; set; } = new();
        public Dictionary<string, decimal> ItemTypeStats { get; set; } = new();
        public Dictionary<string, int> DailyStats { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// 日结对账DTO
    /// </summary>
    public class DailySettlementDto
    {
        public Guid Id { get; set; }
        public DateTime SettlementDate { get; set; }
        public Guid CashierId { get; set; }
        public string? CashierName { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal NetAmount { get; set; }
        public Dictionary<string, decimal> PaymentBreakdown { get; set; } = new();
        public Dictionary<string, decimal> ItemTypeBreakdown { get; set; } = new();
        public string Status { get; set; } = string.Empty; // 未结算、已结算、已审核
        public DateTime CreateTime { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 发票信息DTO
    /// </summary>
    public class InvoiceDto
    {
        public Guid Id { get; set; }
        public Guid CashierRecordId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string InvoiceType { get; set; } = string.Empty; // 普通发票、专用发票
        public string BuyerInfo { get; set; } = string.Empty;
        public string SellerInfo { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();
        public DateTime IssueTime { get; set; }
        public string Status { get; set; } = string.Empty; // 正常、作废、红冲
        public string? PrintPath { get; set; }
    }

    /// <summary>
    /// 发票项目DTO
    /// </summary>
    public class InvoiceItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
    }

    /// <summary>
    /// 费用明细汇总DTO（用于预结算）
    /// </summary>
    public class BillingSummaryDto
    {
        public Guid MedicalCaseId { get; set; }
        public Guid PatientId { get; set; }
        public string? PatientName { get; set; }
        public decimal RegistrationFee { get; set; } = 0;
        public decimal PrescriptionFee { get; set; } = 0;
        public decimal TreatmentFee { get; set; } = 0;
        public decimal OtherFee { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;
        public List<BillingItemDto> Items { get; set; } = new();
        public bool CanPay { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 费用项目DTO
    /// </summary>
    public class BillingItemDto
    {
        public string ItemType { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public Guid? SourceId { get; set; }
        public string? SourceType { get; set; }
        public bool IsOptional { get; set; } = false;
        public bool IsSelected { get; set; } = true;
    }
}