using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Core.Entities.Formula
{

    /// <summary>
    /// 验方明细 - 验方中的药材组成，包含药材名称和剂量
    /// 根据用户要求：剂量使用整数，不继承IHerbItem接口
    /// </summary>
    [Table("FormulaHerbItems")]
    public class FormulaHerbItem
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [DisplayName("ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 所属验方ID
        /// </summary>
        [DisplayName("验方ID")]
        public Guid FormulaId { get; set; }

        /// <summary>
        /// Gets or sets 药材ID（关联药材库）.
        /// </summary>
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }

        /// <summary>
        /// Gets or sets 药材名称.
        /// </summary>
        [Required]
        [StringLength(100)]
        [DisplayName("药材名称")]
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets 剂量（整数，根据用户要求）
        /// </summary>
        [DisplayName("剂量")]
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Gets or sets 剂量（与Quantity同义，为兼容性保留）
        /// </summary>
        [NotMapped]
        public int Dosage
        {
            get => Quantity;
            set => Quantity = value;
        }

        /// <summary>
        /// Gets or sets 单位（从药材库继承，如：克、钱、两等）.
        /// </summary>
        [StringLength(16)]
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

        /// <summary>
        /// Gets or sets 用法说明（该药材的特殊用法）.
        /// </summary>
        [StringLength(200)]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>
        /// Gets or sets 备注信息.
        /// </summary>
        [StringLength(200)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// Gets or sets 炮制方法.
        /// </summary>
        [StringLength(100)]
        [DisplayName("炮制方法")]
        public string? ProcessingMethod { get; set; }
    }
}
