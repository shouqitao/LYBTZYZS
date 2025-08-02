using System;

namespace LYBT.WPF.Client.Core.Models.DTOs
{
    /// <summary>
    /// 药材导入DTO
    /// </summary>
    public class HerbImportDto
    {
        /// <summary>编码</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>分类</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>销售价</summary>
        public decimal SalePrice { get; set; }

        /// <summary>库存</summary>
        public decimal Stock { get; set; }

        /// <summary>产地</summary>
        public string? Origin { get; set; }
    }
}