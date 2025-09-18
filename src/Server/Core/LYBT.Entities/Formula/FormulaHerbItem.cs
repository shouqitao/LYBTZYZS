using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Entities.Common;

namespace LYBT.Entities.Formula
{

    /// <summary>
    /// 验方明细 - 验方中的药材组成，包含药材名称和剂量倍数.
    /// </summary>
    public class FormulaHerbItem : IHerbItem
    {

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
        /// Gets or sets 剂量倍数（药材规格的倍数，如：5倍）
        /// 实际用量 = 药材规格 × 剂量倍数.
        /// </summary>
        [DisplayName("剂量倍数")]
        public decimal Quantity { get; set; } = 1;

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
    }
}
