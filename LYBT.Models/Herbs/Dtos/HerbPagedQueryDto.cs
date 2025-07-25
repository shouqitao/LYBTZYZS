using System.ComponentModel;
using LYBT.Common.Enums.Herbs;

namespace LYBT.Module.Herbs.Dtos {
    /// <summary>
    /// 药材分页查询参数
    /// </summary>
    public class HerbPagedQueryDto {
        /// <summary>关键词（名称或拼音）</summary>
        [DisplayName("关键词")]
        public string Keyword { get; set; } = string.Empty;

        /// <summary>页码（从1开始）</summary>
        [DisplayName("页码")]
        public int Page { get; set; } = 1;

        /// <summary>每页数量</summary>
        [DisplayName("每页数量")]
        public int PageSize { get; set; } = 20;

        /// <summary>药材状态筛选</summary>
        [DisplayName("药材状态筛选")]
        public HerbStatus? Status { get; set; }

        /// <summary>是否包含停用药材</summary>
        [DisplayName("是否包含停用药材")]
        public bool IncludeInactive { get; set; } = false;

        /// <summary>是否查询库存不足药材</summary>
        [DisplayName("是否查询库存不足药材")]
        public bool OnlyLowStock { get; set; } = false;

        /// <summary>库存不足阈值</summary>
        [DisplayName("库存不足阈值")]
        public int LowStockThreshold { get; set; } = 10;

        /// <summary>是否查询即将过期药材</summary>
        [DisplayName("是否查询即将过期药材")]
        public bool OnlyExpiring { get; set; } = false;

        /// <summary>过期预警天数</summary>
        [DisplayName("过期预警天数")]
        public int ExpiringDays { get; set; } = 30;
    }
}