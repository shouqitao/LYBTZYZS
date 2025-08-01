using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Herbs
{
    /// <summary>
    /// 中药材分页查询DTO - 前后端共享API契约
    /// 用于中药材档案的分页查询和筛选
    /// </summary>
    public class HerbPagedQueryDto : PaginationRequest
    {
        /// <summary>药材名称关键词</summary>
        [DisplayName("药材名称")]
        public string? Name { get; set; }

        /// <summary>拼音码关键词</summary>
        [DisplayName("拼音码")]
        public string? Pinyin { get; set; }

        /// <summary>五笔码关键词</summary>
        [DisplayName("五笔码")]
        public string? WuBi { get; set; }

        /// <summary>产地关键词</summary>
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格关键词</summary>
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>药材状态筛选</summary>
        [DisplayName("药材状态")]
        public HerbStatus? Status { get; set; }

        /// <summary>批号关键词</summary>
        [DisplayName("批号")]
        public string? BatchNo { get; set; }

        /// <summary>最小库存数量</summary>
        [DisplayName("最小库存")]
        public int? MinStock { get; set; }

        /// <summary>最大库存数量</summary>
        [DisplayName("最大库存")]
        public int? MaxStock { get; set; }

        /// <summary>最小单价</summary>
        [DisplayName("最小单价")]
        public decimal? MinPrice { get; set; }

        /// <summary>最大单价</summary>
        [DisplayName("最大单价")]
        public decimal? MaxPrice { get; set; }

        /// <summary>有效期范围-开始日期</summary>
        [DisplayName("有效期开始")]
        public DateTime? ExpireStartDate { get; set; }

        /// <summary>有效期范围-结束日期</summary>
        [DisplayName("有效期结束")]
        public DateTime? ExpireEndDate { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool? IsActive { get; set; }

        /// <summary>是否包含已删除的药材</summary>
        [DisplayName("包含已删除")]
        public bool IncludeInactive { get; set; } = false;

        /// <summary>仅显示库存不足的药材</summary>
        [DisplayName("仅库存不足")]
        public bool OnlyLowStock { get; set; } = false;

        /// <summary>仅显示即将过期的药材（30天内）</summary>
        [DisplayName("仅即将过期")]
        public bool OnlyExpiring { get; set; } = false;

        /// <summary>库存不足的阈值（默认10）</summary>
        [DisplayName("库存阈值")]
        public int LowStockThreshold { get; set; } = 10;

        /// <summary>即将过期的天数阈值（默认30天）</summary>
        [DisplayName("过期阈值天数")]
        public int ExpiringDaysThreshold { get; set; } = 30;
    }
}