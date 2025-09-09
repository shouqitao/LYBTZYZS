using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;

namespace LYBT.Entities.Prescriptions
{

    /// <summary>
    /// 处方药材项 - 处方中的药材明细，包含药材名称、剂量和单价，用于收费计算
    /// </summary>
    public class PrescriptionItemModel : IHerbItem
    {

        /// <summary>
        /// 处方项唯一标识（主键）
        /// </summary>
        [Key]
        [DisplayName("处方项ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 关联处方ID
        /// </summary>
        [Required]
        [DisplayName("处方ID")]
        public Guid PrescriptionId { get; set; }

        /// <summary>
        /// 药材ID（关联药材库）
        /// </summary>
        [Required]
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        [Required]
        [StringLength(100)]
        [DisplayName("药材名称")]
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 实际用量（已计算好的具体剂量）
        /// </summary>
        [Column(TypeName = "decimal(10,3)")]
        [DisplayName("用量")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单位（如：克、钱、两等）
        /// </summary>
        [StringLength(16)]
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

        /// <summary>
        /// 药材单价（用于收费计算）
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("单价")]
        public decimal UnitPrice { get; set; } = 0;

        /// <summary>
        /// 小计金额（单价 × 用量）
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("小计")]
        public decimal Amount => UnitPrice * Quantity;

        /// <summary>
        /// 用法说明（该药材的特殊用法）
        /// </summary>
        [StringLength(200)]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        [StringLength(200)]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}
