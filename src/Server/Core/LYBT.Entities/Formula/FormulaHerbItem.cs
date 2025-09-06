using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Entities.Common;

namespace LYBT.Entities.Formula {

    /// <summary>
    /// 验方明细 - 验方中的药材组成，包含药材名称和剂量倍数
    /// </summary>
    public class FormulaHerbItem : IHerbItem {

        /// <summary>
        /// 药材ID（关联药材库）
        /// </summary>
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        [Required, StringLength(100)]
        [DisplayName("药材名称")]
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 剂量倍数（药材规格的倍数，如：5倍）
        /// 实际用量 = 药材规格 × 剂量倍数
        /// </summary>
        [DisplayName("剂量倍数")]
        public decimal Quantity { get; set; } = 1;

        /// <summary>
        /// 单位（从药材库继承，如：克、钱、两等）
        /// </summary>
        [StringLength(16)]
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

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
