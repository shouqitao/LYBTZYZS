using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Common.Enums;

namespace LYBT.Models.Billing {

    /// <summary>
    /// 账单主表实体
    /// </summary>
    public class BillingModel {

        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 账单业务编码（如流水号，可选）
        /// </summary>
        [StringLength(64)]
        public string BillingId { get; set; } = string.Empty;

        /// <summary>
        /// 病人ID
        /// </summary>
        [Required]
        public Guid PatientId { get; set; }

        /// <summary>
        /// 对应处方ID
        /// </summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>
        /// 账单明细项目（建议用 Json 字段保存）
        /// </summary>
        [Required]
        public List<BillingItem> Items { get; set; } = new();

        /// <summary>
        /// 账单总金额
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 已缴金额
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        [Required]
        public BillingStatus Status { get; set; } = BillingStatus.Pending;

        /// <summary>
        /// 缴费方式（现金、微信等）
        /// </summary>
        [StringLength(32)]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// 开单医生ID
        /// </summary>
        [Required]
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime? PaidTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedTime { get; set; }

        /// <summary>
        /// 退款时间
        /// </summary>
        public DateTime? RefundTime { get; set; }

        /// <summary>
        /// 退款理由
        /// </summary>
        [StringLength(128)]
        public string? RefundReason { get; set; }

        /// <summary>
        /// 是否删除
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 账单时间（如有二次缴费等场景可与 CreateTime 区分）
        /// </summary>
        [Required]
        public DateTime BillingTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(256)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 账单明细实体（可单独为表，也可作为 Json 字段保存）
    /// </summary>
    public class BillingItem {

        /// <summary>
        /// 明细主键ID（如不用单独建表可省略）
        /// </summary>
        public Guid ItemId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 项目名称
        /// </summary>
        [Required, StringLength(64)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 单价
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 小计（单价 × 数量，自动计算）
        /// </summary>
        [NotMapped]
        public decimal SubTotal => UnitPrice * Quantity;
    }
}