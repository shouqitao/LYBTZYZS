using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Billing
{
    /// <summary>
    /// 新增账单 DTO
    /// </summary>
    public class BillingCreateDto
    {
        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>挂号ID（可选）</summary>
        [DisplayName("挂号ID")]
        public Guid? RegistrationId { get; set; }

        /// <summary>病历ID（可选）</summary>
        [DisplayName("病历ID")]
        public Guid? RecordId { get; set; }

        /// <summary>处方ID（可选）</summary>
        [DisplayName("处方ID")]
        public Guid? PrescriptionId { get; set; }

        /// <summary>开单医生ID</summary>
        [Required(ErrorMessage = "开单医生ID不能为空")]
        [DisplayName("开单医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>账单明细列表</summary>
        [DisplayName("账单明细列表")]
        public List<BillingItemDto> Items { get; set; } = new();

        /// <summary>账单总金额</summary>
        [DisplayName("账单总金额")]
        public decimal TotalAmount { get; set; }

        /// <summary>折扣金额</summary>
        [DisplayName("折扣金额")]
        public decimal DiscountAmount { get; set; }

        /// <summary>已缴金额</summary>
        [DisplayName("已缴金额")]
        public decimal PaidAmount { get; set; }

        /// <summary>账单状态</summary>
        [DisplayName("账单状态")]
        public BillingStatus Status { get; set; } = BillingStatus.Pending;

        /// <summary>支付方式</summary>
        [DisplayName("支付方式")]
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 账单明细 DTO
    /// </summary>
    public class BillingItemDto
    {
        /// <summary>明细项ID</summary>
        [DisplayName("明细项ID")]
        public Guid ItemId { get; set; } = Guid.NewGuid();

        /// <summary>项目类型（Medicine=药品, Treatment=理疗, Consultation=诊疗, Examination=检查, Material=材料, Other=其他）</summary>
        [DisplayName("项目类型")]
        [Required]
        [StringLength(50)]
        public string ItemType { get; set; } = string.Empty;

        /// <summary>项目编码</summary>
        [DisplayName("项目编码")]
        [Required]
        [StringLength(50)]
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>项目名称</summary>
        [DisplayName("项目名称")]
        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>规格</summary>
        [DisplayName("规格")]
        [StringLength(100)]
        public string? Specification { get; set; }

        /// <summary>单位</summary>
        [DisplayName("单位")]
        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>单价</summary>
        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        /// <summary>数量</summary>
        [DisplayName("数量")]
        public decimal Quantity { get; set; }

        /// <summary>折扣率（0-1）</summary>
        [DisplayName("折扣率")]
        public decimal DiscountRate { get; set; } = 1;

        /// <summary>折扣金额（计算属性）</summary>
        [DisplayName("折扣金额")]
        public decimal DiscountAmount => UnitPrice * Quantity * (1 - DiscountRate);

        /// <summary>小计（计算属性）</summary>
        [DisplayName("小计")]
        public decimal SubTotal => UnitPrice * Quantity * DiscountRate;

        /// <summary>关联ID（如药品ID、理疗项目ID等）</summary>
        [DisplayName("关联ID")]
        public Guid? RelatedId { get; set; }

        /// <summary>排序号</summary>
        [DisplayName("排序号")]
        public int SortOrder { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Remark { get; set; }
    }
}