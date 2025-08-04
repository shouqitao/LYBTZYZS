using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Shared.Models.Core
{
    /// <summary>
    /// 账单基础模型 - 前后端共享核心字段
    /// 包含所有通用的账单信息字段，各层可基于此模型扩展
    /// </summary>
    public class BaseBillingModel
    {
        /// <summary>账单唯一标识</summary>
        [DisplayName("账单ID")]
        public Guid Id { get; set; }

        /// <summary>账单编号（统一命名）</summary>
        [DisplayName("账单编号")]
        [Required]
        [StringLength(50)]
        [Column("BillingNumber")]
        public string BillingId { get; set; } = string.Empty;

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        [Required]
        public Guid PatientId { get; set; }

        /// <summary>挂号ID</summary>
        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }

        /// <summary>病历ID</summary>
        [DisplayName("病历ID")]
        public Guid? RecordId { get; set; }

        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
        public Guid? PrescriptionId { get; set; }

        /// <summary>账单总金额</summary>
        [DisplayName("总金额")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>已付金额</summary>
        [DisplayName("已付金额")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        /// <summary>折扣金额</summary>
        [DisplayName("折扣金额")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        /// <summary>应付金额（计算属性）</summary>
        [DisplayName("应付金额")]
        [NotMapped]
        public decimal PayableAmount => TotalAmount - DiscountAmount;

        /// <summary>账单状态</summary>
        [DisplayName("账单状态")]
        public BillingStatus Status { get; set; } = BillingStatus.Pending;

        /// <summary>支付方式</summary>
        [DisplayName("支付方式")]
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>开单医生ID</summary>
        [DisplayName("开单医生ID")]
        [Required]
        public Guid DoctorId { get; set; }

        /// <summary>收费员ID</summary>
        [DisplayName("收费员ID")]
        public Guid? CashierId { get; set; }

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        [Column("CreateTime")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>支付时间</summary>
        [DisplayName("支付时间")]
        public DateTime? PaidTime { get; set; }

        /// <summary>退款时间</summary>
        [DisplayName("退款时间")]
        public DateTime? RefundTime { get; set; }

        /// <summary>退款金额</summary>
        [DisplayName("退款金额")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        /// <summary>退款原因</summary>
        [DisplayName("退款原因")]
        [StringLength(500)]
        public string? RefundReason { get; set; }

        /// <summary>退款操作员ID</summary>
        [DisplayName("退款操作员ID")]
        public Guid? RefundOperatorId { get; set; }

        /// <summary>发票号</summary>
        [DisplayName("发票号")]
        [StringLength(100)]
        public string? InvoiceNumber { get; set; }

        /// <summary>是否已开发票</summary>
        [DisplayName("是否已开发票")]
        public bool IsInvoiced { get; set; } = false;

        /// <summary>是否已删除</summary>
        [DisplayName("是否已删除")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>删除时间</summary>
        [DisplayName("删除时间")]
        public DateTime? DeleteTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Remark { get; set; }
    }
}