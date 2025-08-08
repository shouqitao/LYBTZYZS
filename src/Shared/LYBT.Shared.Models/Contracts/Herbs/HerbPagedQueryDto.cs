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
        public string? PinYinCode { get; set; }

        // 五笔码字段已移除（按照字段标准化要求）

        /// <summary>产地关键词</summary>
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格关键词</summary>
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>状态筛选</summary>
        [DisplayName("状态")]
        public CommonStatus? Status { get; set; }

        // 库存管理字段已移除（按照字段标准化要求）

        /// <summary>最小单价</summary>
        [DisplayName("最小单价")]
        public decimal? MinPrice { get; set; }

        /// <summary>最大单价</summary>
        [DisplayName("最大单价")]
        public decimal? MaxPrice { get; set; }

        // 有效期和 IsActive 字段已移除（按照字段标准化要求）

        /// <summary>是否包含已删除的药材</summary>
        [DisplayName("包含已删除")]
        public bool IncludeInactive { get; set; } = false;

        // 库存相关查询字段已移除（按照字段标准化要求）
    }
}