using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 分页查询基础DTO
    /// </summary>
    public class PagedQueryBaseDto
    {
        /// <summary>搜索关键词</summary>
        [DisplayName("搜索关键词")]
        [StringLength(100)]
        public string? Keyword { get; set; }

        /// <summary>页码（从1开始）</summary>
        [DisplayName("页码")]
        [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
        public int PageIndex { get; set; } = 1;

        /// <summary>每页记录数</summary>
        [DisplayName("每页记录数")]
        [Range(1, 100, ErrorMessage = "每页记录数必须在1-100之间")]
        public int PageSize { get; set; } = 20;

        /// <summary>排序字段</summary>
        [DisplayName("排序字段")]
        [StringLength(50)]
        public string? SortField { get; set; }

        /// <summary>是否降序</summary>
        [DisplayName("是否降序")]
        public bool IsDescending { get; set; } = false;

        /// <summary>跳过记录数（计算属性）</summary>
        [DisplayName("跳过记录数")]
        public int Skip => (PageIndex - 1) * PageSize;
    }
}