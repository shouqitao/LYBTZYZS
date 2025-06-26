namespace LYBT.Models {

    /// <summary>
    /// 药材明细实体（用于药方组成）
    /// </summary>
    public class HerbItemModel {

        /// <summary>
        /// 药材ID（关联药材主数据）
        /// </summary>
        public Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 剂量
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 小计（单价 × 剂量）
        /// </summary>
        public decimal TotalPrice => UnitPrice * Amount;
    }
}