using System.ComponentModel;
namespace LYBT.Models {

    /// <summary>
    /// 药材明细实体（用于药方组成）
    /// </summary>
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
        /// 剂量
        /// </summary>
        [DisplayName("剂量")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 小计（单价 × 剂量）
        /// </summary>
        public decimal TotalPrice => UnitPrice * Amount;
    }
}