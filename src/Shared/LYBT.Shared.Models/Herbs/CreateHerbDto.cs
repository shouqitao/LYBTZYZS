using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Herbs
{
    /// <summary>
    /// 创建药材DTO
    /// </summary>
    public class CreateHerbDto
    {
        /// <summary>药材编码</summary>
        [Required(ErrorMessage = "药材编码不能为空")]
        [StringLength(50, ErrorMessage = "药材编码长度不能超过50个字符")]
        public string Code { get; set; } = string.Empty;

        /// <summary>药材名称</summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        public string? PinyinCode { get; set; }

        /// <summary>别名</summary>
        [StringLength(200, ErrorMessage = "别名长度不能超过200个字符")]
        public string? Alias { get; set; }

        /// <summary>分类</summary>
        [StringLength(50, ErrorMessage = "分类长度不能超过50个字符")]
        public string? Category { get; set; }

        /// <summary>性味归经</summary>
        [StringLength(200, ErrorMessage = "性味归经长度不能超过200个字符")]
        public string? Properties { get; set; }

        /// <summary>功效</summary>
        [StringLength(500, ErrorMessage = "功效长度不能超过500个字符")]
        public string? Effects { get; set; }

        /// <summary>用法用量</summary>
        [StringLength(200, ErrorMessage = "用法用量长度不能超过200个字符")]
        public string? Usage { get; set; }

        /// <summary>禁忌</summary>
        [StringLength(200, ErrorMessage = "禁忌长度不能超过200个字符")]
        public string? Contraindications { get; set; }

        /// <summary>产地</summary>
        [StringLength(100, ErrorMessage = "产地长度不能超过100个字符")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [StringLength(50, ErrorMessage = "规格长度不能超过50个字符")]
        public string? Specification { get; set; }

        /// <summary>单位</summary>
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(10, ErrorMessage = "单位长度不能超过10个字符")]
        public string Unit { get; set; } = "g";

        /// <summary>进价</summary>
        [Required(ErrorMessage = "进价不能为空")]
        [Range(0, 99999.99, ErrorMessage = "进价必须在0-99999.99之间")]
        public decimal CostPrice { get; set; }

        /// <summary>售价</summary>
        [Required(ErrorMessage = "售价不能为空")]
        [Range(0, 99999.99, ErrorMessage = "售价必须在0-99999.99之间")]
        public decimal SalePrice { get; set; }

        /// <summary>库存量</summary>
        [Range(0, 99999.99, ErrorMessage = "库存量必须在0-99999.99之间")]
        public decimal Stock { get; set; }

        /// <summary>最小库存</summary>
        [Range(0, 99999.99, ErrorMessage = "最小库存必须在0-99999.99之间")]
        public decimal MinStock { get; set; }

        /// <summary>最大库存</summary>
        [Range(0, 99999.99, ErrorMessage = "最大库存必须在0-99999.99之间")]
        public decimal MaxStock { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
    }
}