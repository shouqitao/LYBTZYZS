using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Models.DiagnosisTreatment {

    /// <summary>
    /// 药材明细实体（用于药方组成）
    /// </summary>
    [Owned]
    public class HerbItemModel {

        /// <summary>
        /// 药材ID（关联药材主数据）
        /// </summary>
        [DisplayName("药材ID（关联药材主数据）")]
        public Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 药材名称（别名）
        /// </summary>
        [DisplayName("药材名称")]
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 剂量
        /// </summary>
        [DisplayName("剂量")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        [DisplayName("单价")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 小计（单价 × 剂量）
        /// </summary>
        public decimal TotalPrice => UnitPrice * Amount;
    }
}