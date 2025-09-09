namespace LYBT.Shared.Models.Contracts.Common
{

    /// <summary>
    /// 分页查询请求 - UltraThink极简重构：删除验证，删除冗余
    /// </summary>
    public class PagedQueryBaseDto
    {
        public string? Keyword { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortField { get; set; }
        public bool IsDescending { get; set; }

        public int Skip => (PageIndex - 1) * PageSize;
        public Dictionary<string, object> Extensions { get; set; } = new();

        // 兼容性别名
        public int CurrentPage { get => PageIndex; set => PageIndex = value; }

        public string? SearchKeyword { get => Keyword; set => Keyword = value; }
    }
}
