using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 方剂药材成分DTO - 前后端共享API契约
    /// 用于在药方中表示单味药材的用量和计价信息
    /// </summary>
    public class FormulaIngredientDto
    {

        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>单位</summary>
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>数量</summary>
        [Required(ErrorMessage = "数量不能为空")]
        [Range(0.1, 999999, ErrorMessage = "数量必须大于0")]
        [DisplayName("数量")]
        public decimal Quantity { get; set; }

        /// <summary>小计（自动计算）</summary>
        [DisplayName("小计")]
        public decimal TotalPrice => Price * Quantity;

        /// <summary>备注</summary>
        [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}