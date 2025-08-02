using System;

namespace LYBT.WPF.Client.Core.Models.DTOs
{
    /// <summary>
    /// 创建药材DTO
    /// </summary>
    public class HerbCreateDto
    {
        /// <summary>编码</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>别名</summary>
        public string? Alias { get; set; }

        /// <summary>分类</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>性味归经</summary>
        public string? Properties { get; set; }

        /// <summary>功效</summary>
        public string? Effects { get; set; }

        /// <summary>用法用量</summary>
        public string? Usage { get; set; }

        /// <summary>禁忌</summary>
        public string? Contraindications { get; set; }

        /// <summary>产地</summary>
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        public string? Specification { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>成本价</summary>
        public decimal CostPrice { get; set; }

        /// <summary>销售价</summary>
        public decimal SalePrice { get; set; }

        /// <summary>库存</summary>
        public decimal Stock { get; set; }

        /// <summary>最小库存</summary>
        public decimal MinStock { get; set; }

        /// <summary>最大库存</summary>
        public decimal MaxStock { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}