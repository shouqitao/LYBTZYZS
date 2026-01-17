namespace LYBT.Desktop.Infrastructure.Models.Options;

/// <summary>
/// 分页配置选项（不可变）
/// OpenSpec: unify-control-data-binding
/// </summary>
public record PaginationOptions(
    int DefaultPageSize = 20,
    int[] PageSizeOptions = null!,
    bool ShowPageSizeSelector = true
)
{
    /// <summary>可选的分页大小列表</summary>
    public int[] PageSizeOptions { get; init; } = PageSizeOptions ?? new[] { 10, 20, 50, 100 };
}
