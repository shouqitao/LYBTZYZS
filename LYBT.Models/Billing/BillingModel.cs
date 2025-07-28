using LYBT.Common.Enums.System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Billing {

    /// <summary>
    /// 账单主表实体
    /// </summary>
    public class BillingModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        /// <summary>
        /// 账单业务编码（如流水号，可选）
        /// </summary>
        [StringLength(64)]
        [DisplayName("账单业务编码（如流水号，可选）")]
        public string BillingId { get; set; } = string.Empty;

        /// <summary>
        /// 病人ID
        /// </summary>
        [Required]
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>
        /// 对应处方ID
        /// </summary>
        [DisplayName("对应处方ID")]
        public Guid? PrescriptionId { get; set; }

        /// <summary>
        /// 账单明细项目（建议用 Json 字段保存）
        /// </summary>
        [Required]
        [DisplayName("账单明细项目（建议用 Json 字段保存）")]
        public List<BillingItem> Items { get; set; } = new();

        /// <summary>
        /// 账单总金额
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("账单总金额")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 已缴金额
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("已缴金额")]
        public decimal PaidAmount { get; set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        [Required]
        [DisplayName("当前状态")]
        public BillingStatus Status { get; set; } = BillingStatus.Pending;

        /// <summary>
        /// 缴费方式（现金、微信等）
        /// </summary>
        [StringLength(32)]
        [DisplayName("缴费方式（现金、微信等）")]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// 开单医生ID
        /// </summary>
        [Required]
        [DisplayName("开单医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [DisplayName("创建时间")]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 支付时间
        /// </summary>
        [DisplayName("支付时间")]
        public DateTime? PaidTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        [DisplayName("完成时间")]
        public DateTime? CompletedTime { get; set; }

        /// <summary>
        /// 退款时间
        /// </summary>
        [DisplayName("退款时间")]
        public DateTime? RefundTime { get; set; }

        /// <summary>
        /// 退款理由
        /// </summary>
        [StringLength(128)]
        [DisplayName("退款理由")]
        public string? RefundReason { get; set; }

        /// <summary>
        /// 是否删除
        /// </summary>
        [DisplayName("是否删除")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 账单时间（如有二次缴费等场景可与 CreateTime 区分）
        /// </summary>
        [Required]
        [DisplayName("账单时间（如有二次缴费等场景可与 CreateTime 区分）")]
        public DateTime BillingTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(256)]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 账单明细实体（可单独为表，也可作为 Json 字段保存）
    /// </summary>
    public class BillingItem {

        /// <summary>
        /// 明细主键ID（如不用单独建表可省略）
        /// </summary>
        [DisplayName("明细主键ID（如不用单独建表可省略）")]
        public Guid ItemId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 项目名称
        /// </summary>
        [Required, StringLength(64)]
        [DisplayName("项目名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 单价
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("数量")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 小计（单价 × 数量，自动计算）
        /// </summary>
        [NotMapped]
        public decimal SubTotal => UnitPrice * Quantity;
    }
}