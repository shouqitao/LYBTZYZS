using System;
using System.Collections.Generic;

namespace LYBT.WPF.Client.Core.Models.DTOs
{
    /// <summary>
    /// 药材详情DTO
    /// </summary>
    public class HerbDetailDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

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

        /// <summary>状态（0:正常 1:缺货 2:停用）</summary>
        public int Status { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>更新时间</summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>库存批次信息</summary>
        public List<StockBatchInfo> StockBatches { get; set; } = new();
    }

    /// <summary>
    /// 库存批次信息
    /// </summary>
    public class StockBatchInfo
    {
        /// <summary>批次号</summary>
        public string BatchNo { get; set; } = string.Empty;

        /// <summary>生产日期</summary>
        public DateTime ProductionDate { get; set; }

        /// <summary>过期日期</summary>
        public DateTime ExpiryDate { get; set; }

        /// <summary>数量</summary>
        public decimal Quantity { get; set; }

        /// <summary>供应商</summary>
        public string? Supplier { get; set; }
    }
}