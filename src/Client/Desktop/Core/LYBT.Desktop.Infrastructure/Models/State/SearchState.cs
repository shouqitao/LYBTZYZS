using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models.State;

/// <summary>
/// 可复用的搜索状态对象
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class SearchState : ObservableObject
{
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearching;

    /// <summary>是否有搜索文本</summary>
    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>清除搜索状态</summary>
    public void Clear()
    {
        SearchText = string.Empty;
        IsSearching = false;
    }

    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(HasSearchText));
}
