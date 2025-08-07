using System;

namespace LYBT.Shared.Models.Contracts.Formulas
{
    /// <summary>
    /// 验方模板药材DTO
    /// </summary>
    public class FormulaHerbDto
    {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>数量</summary>
        public decimal Quantity { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = "g";

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>排序</summary>
        public int SortOrder { get; set; }
    }
}