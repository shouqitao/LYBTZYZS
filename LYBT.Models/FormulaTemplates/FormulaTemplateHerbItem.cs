using System.ComponentModel;

namespace LYBT.Models.FormulaTemplates {

    /// <summary>
    /// 经验方模板药材明细实体
    /// </summary>
    public class FormulaTemplateHerbItem {

        /// <summary>
        /// 药材ID（关联药材主数据）
        /// </summary>
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 药材别名
        /// </summary>
        [DisplayName("药材别名")]
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 剂量
        /// </summary>
        [DisplayName("剂量")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [DisplayName("数量")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

        /// <summary>
        /// 单价
        /// </summary>
        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 小计（单价 × 剂量）
        /// </summary>
        public decimal TotalPrice => UnitPrice * Amount;

        /// <summary>
        /// 用法
        /// </summary>
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}