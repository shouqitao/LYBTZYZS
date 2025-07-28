using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Herbs {

    /// <summary>
    /// 新增药材 DTO
    /// </summary>
    public class HerbCreateDto {

        /// <summary>药材名称</summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? Pinyin { get; set; }

        /// <summary>五笔码</summary>
        [DisplayName("五笔码")]
        public string? WuBi { get; set; }

        /// <summary>产地</summary>
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>基础规格数值（如：1，用于计算实际用量）</summary>
        [DisplayName("规格")]
        public decimal Spec { get; set; } = 1;

        /// <summary>单位</summary>
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [Range(0, double.MaxValue, ErrorMessage = "单价不能为负数")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        [DisplayName("库存数量")]
        public int Stock { get; set; }

        [DisplayName("批号")]
        public string? BatchNo { get; set; }

        [DisplayName("有效期")]
        public DateTime? ExpireDate { get; set; }

        /// <summary>功效说明</summary>
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}