using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models.State;

/// <summary>
/// 可复用的分页状态对象
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class PaginationState : ObservableObject
{
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;

    /// <summary>总页数</summary>
    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 1;

    /// <summary>是否有上一页</summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>是否有下一页</summary>
    public bool HasNext => CurrentPage < TotalPages;

    /// <summary>跳转到指定页（自动限制范围）</summary>
    public void GoToPage(int page) => CurrentPage = Math.Clamp(page, 1, TotalPages);

    /// <summary>重置分页状态</summary>
    public void Reset()
    {
        CurrentPage = 1;
        TotalCount = 0;
    }

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(HasPrevious));
    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasNext));
    }
    partial void OnPageSizeChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasNext));
    }
}
