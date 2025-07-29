using LYBT.Common.Enums.Herbs;
using System.ComponentModel;

namespace LYBT.Models.Herbs {

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

        /// <summary>状态筛选</summary>
        [DisplayName("状态筛选")]
        public HerbStatus? Status { get; set; }

        /// <summary>是否包含停用</summary>
        [DisplayName("是否包含停用")]
        public bool IncludeInactive { get; set; } = false;

        /// <summary>只显示低库存</summary>
        [DisplayName("只显示低库存")]
        public bool OnlyLowStock { get; set; } = false;

        /// <summary>低库存阈值</summary>
        [DisplayName("低库存阈值")]
        public int LowStockThreshold { get; set; } = 10;

        /// <summary>只显示即将过期</summary>
        [DisplayName("只显示即将过期")]
        public bool OnlyExpiring { get; set; } = false;

        /// <summary>过期天数阈值</summary>
        [DisplayName("过期天数阈值")]
        public int ExpiringDays { get; set; } = 30;
    }
}