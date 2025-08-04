using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Billing;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Billing
{
    /// <summary>
    /// 账单实体 - 继承共享基础模型，数据库映射
    /// </summary>
    [Table("Billings")]
    public class BillingModel : BaseBillingModel
    {
        /// <summary>患者（导航属性）</summary>
        [ForeignKey("PatientId")]
        public virtual Patients.PatientModel? Patient { get; set; }

        /// <summary>挂号记录（导航属性）</summary>
        [ForeignKey("RegistrationId")]
        public virtual Registration.RegistrationModel? Registration { get; set; }

        /// <summary>病历记录（导航属性）</summary>
        [ForeignKey("RecordId")]
        public virtual Records.RecordModel? Record { get; set; }

        /// <summary>处方记录（导航属性）</summary>
        [ForeignKey("PrescriptionId")]
        public virtual Prescriptions.PrescriptionModel? Prescription { get; set; }

        /// <summary>开单医生（导航属性）</summary>
        [ForeignKey("DoctorId")]
        public virtual Doctors.DoctorModel? Doctor { get; set; }

        /// <summary>收费员（导航属性）</summary>
        [ForeignKey("CashierId")]
        public virtual Users.UserModel? Cashier { get; set; }

        /// <summary>退款操作员（导航属性）</summary>
        [ForeignKey("RefundOperatorId")]
        public virtual Users.UserModel? RefundOperator { get; set; }

        /// <summary>账单明细项目（导航属性）</summary>
        public virtual ICollection<BillingItemModel> Items { get; set; } = new List<BillingItemModel>();
    }

    /// <summary>
    /// 账单明细实体 - 数据库映射
    /// </summary>
    [Table("BillingItems")]
    public class BillingItemModel
    {
        /// <summary>明细项ID</summary>
        [Key]
        [DisplayName("明细项ID")]
        public Guid ItemId { get; set; } = Guid.NewGuid();

        /// <summary>所属账单ID</summary>
        [Required]
        [DisplayName("所属账单ID")]
        public Guid BillingId { get; set; }

        /// <summary>所属账单（导航属性）</summary>
        [ForeignKey("BillingId")]
        public virtual BillingModel? Billing { get; set; }

        /// <summary>项目类型</summary>
        [Required]
        [StringLength(50)]
        [DisplayName("项目类型")]
        public string ItemType { get; set; } = string.Empty;

        /// <summary>项目编码</summary>
        [Required]
        [StringLength(50)]
        [DisplayName("项目编码")]
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>项目名称</summary>
        [Required]
        [StringLength(200)]
        [DisplayName("项目名称")]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>规格</summary>
        [StringLength(100)]
        [DisplayName("规格")]
        public string? Specification { get; set; }

        /// <summary>单位</summary>
        [Required]
        [StringLength(20)]
        [DisplayName("单位")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>单价</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        /// <summary>数量</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("数量")]
        public decimal Quantity { get; set; }

        /// <summary>折扣率（0-1）</summary>
        [Column(TypeName = "decimal(18,4)")]
        [DisplayName("折扣率")]
        public decimal DiscountRate { get; set; } = 1;

        /// <summary>折扣金额</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("折扣金额")]
        public decimal DiscountAmount { get; set; }

        /// <summary>小计</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("小计")]
        public decimal SubTotal { get; set; }

        /// <summary>关联ID（如药品ID、理疗项目ID等）</summary>
        [DisplayName("关联ID")]
        public Guid? RelatedId { get; set; }

        /// <summary>排序号</summary>
        [DisplayName("排序号")]
        public int SortOrder { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>计算折扣金额和小计</summary>
        public void CalculateAmount()
        {
            DiscountAmount = UnitPrice * Quantity * (1 - DiscountRate);
            SubTotal = UnitPrice * Quantity * DiscountRate;
        }
    }
}