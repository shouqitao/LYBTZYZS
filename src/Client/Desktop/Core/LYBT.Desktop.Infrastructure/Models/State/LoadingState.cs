using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models.State;

/// <summary>
/// 可复用的加载状态对象
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class LoadingState : ObservableObject
{
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadingMessage;

    /// <summary>开始加载</summary>
    public void StartLoading(string? message = null)
    {
        IsLoading = true;
        LoadingMessage = message;
    }

    /// <summary>停止加载</summary>
    public void StopLoading()
    {
        IsLoading = false;
        LoadingMessage = null;
    }
}
