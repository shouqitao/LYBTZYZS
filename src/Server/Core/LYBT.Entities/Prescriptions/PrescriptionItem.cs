using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Prescriptions
{

    /// <summary>
    /// 处方药材项 - 处方中的药材明细，包含药材名称、剂量和单价，用于收费计算
    /// 根据文档要求：剂量使用整数，不继承IHerbItem接口
    /// </summary>
    public class PrescriptionItem
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
        /// 剂量（整数）
        /// </summary>
        [DisplayName("剂量")]
        public int Dosage { get; set; }

        /// <summary>
        /// 单位（如：克、钱、两等）
        /// </summary>
        [StringLength(16)]
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

        /// <summary>
        /// 煎法（先煎、后下、烊化等）
        /// </summary>
        [DisplayName("煎法")]
        public DecocteMethod DecocteMethod { get; set; } = DecocteMethod.Default;

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
        public decimal Amount => UnitPrice * Dosage;

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
