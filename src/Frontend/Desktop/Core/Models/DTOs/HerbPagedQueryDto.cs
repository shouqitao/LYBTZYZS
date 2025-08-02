using System;

namespace LYBT.WPF.Client.Core.Models.DTOs
{
    /// <summary>
    /// 药材分页查询DTO
    /// </summary>
    public class HerbPagedQueryDto
    {
        /// <summary>搜索关键词</summary>
        public string? Keyword { get; set; }

        /// <summary>分类</summary>
        public string? Category { get; set; }

        /// <summary>产地</summary>
        public string? Origin { get; set; }

        /// <summary>状态</summary>
        public int? Status { get; set; }

        /// <summary>是否启用</summary>
        public bool? IsEnabled { get; set; }

        /// <summary>当前页</summary>
        public int Page { get; set; } = 1;

        /// <summary>每页数量</summary>
        public int PageSize { get; set; } = 20;

        /// <summary>排序字段</summary>
        public string? SortBy { get; set; }

        /// <summary>是否降序</summary>
        public bool IsDescending { get; set; }
    }
}