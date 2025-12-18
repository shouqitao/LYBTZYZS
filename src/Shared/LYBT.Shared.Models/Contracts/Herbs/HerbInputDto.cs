using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs
{

    // OpenSpec: dto-architecture-specification
    /// <summary>
    /// 中药材输入DTO - 统一创建和更新
    /// Phase 3: 合并HerbCreateDto和HerbUpdateDto
    /// </summary>
    public class HerbInputDto
    {
        /// &lt;summary&gt;药材ID（更新时必填，创建时为null）&lt;/summary&gt;
        [DisplayName("药材ID")]
        public Guid? Id { get; set; }

        /// &lt;summary&gt;药材名称&lt;/summary&gt;
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// &lt;summary&gt;拼音码&lt;/summary&gt;
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// &lt;summary&gt;分类（如：补血药、补气药）Epic #1962&lt;/summary&gt;
        [StringLength(50, ErrorMessage = "分类长度不能超过50个字符")]
        [DisplayName("分类")]
        public string? Category { get; set; }

        /// &lt;summary&gt;产地&lt;/summary&gt;
        [StringLength(100, ErrorMessage = "产地长度不能超过100个字符")]
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [StringLength(50, ErrorMessage = "规格长度不能超过50个字符")]
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位</summary>
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        [DisplayName("单位")]
        public string Unit { get; set; } = "克";

        /// <summary>单价</summary>
        [Required(ErrorMessage = "单价不能为空")]
        [Range(0, 999999.99, ErrorMessage = "单价必须在0-999999.99之间")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>成本价</summary>
        [Range(0, 999999.99, ErrorMessage = "成本价必须在0-999999.99之间")]
        [DisplayName("成本价")]
        public decimal? CostPrice { get; set; }

        /// <summary>功效说明</summary>
        [StringLength(1000, ErrorMessage = "功效说明长度不能超过1000个字符")]
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>用法</summary>
        [StringLength(500, ErrorMessage = "用法长度不能超过500个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        // OpenSpec: refactor-dto-simplification - Status字段已移除
        // InputDto不应包含Status字段，状态变更应通过专用API进行
    }

}
