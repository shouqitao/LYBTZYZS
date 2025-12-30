namespace LYBT.Desktop.Contracts.Services.MasterDetail;

/// <summary>
/// 导航参数
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 避免直接依赖Prism.Regions.NavigationParameters
/// </summary>
public class ViewNavigationParameters : Dictionary<string, object>
{
    /// <summary>
    /// 创建空参数
    /// </summary>
    public ViewNavigationParameters() { }

    /// <summary>
    /// 从键值对创建参数
    /// </summary>
    public ViewNavigationParameters(string key, object value)
    {
        Add(key, value);
    }

    /// <summary>
    /// 链式添加参数
    /// </summary>
    public ViewNavigationParameters With(string key, object value)
    {
        this[key] = value;
        return this;
    }

    /// <summary>
    /// 尝试获取参数值
    /// </summary>
    public T? GetValue<T>(string key)
    {
        if (TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}

/// <summary>
/// 导航结果
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// </summary>
public class ViewNavigationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// 目标视图名称
    /// </summary>
    public string? ViewName { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ViewNavigationResult Succeeded(string viewName) => new() { Success = true, ViewName = viewName };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ViewNavigationResult Failed(string error) => new() { Success = false, Error = error };
}

/// <summary>
/// 视图导航服务接口
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 为MasterDetail模式提供统一的视图导航操作
/// </summary>
public interface IViewNavigationService
{
    /// <summary>
    /// 导航到指定视图
    /// </summary>
    /// <param name="viewName">视图名称</param>
    /// <param name="parameters">导航参数</param>
    void NavigateTo(string viewName, ViewNavigationParameters? parameters = null);

    /// <summary>
    /// 导航到指定Region的视图
    /// </summary>
    /// <param name="regionName">Region名称</param>
    /// <param name="viewName">视图名称</param>
    /// <param name="parameters">导航参数</param>
    void NavigateToRegion(string regionName, string viewName, ViewNavigationParameters? parameters = null);

    /// <summary>
    /// 返回上一视图
    /// </summary>
    void GoBack();

    /// <summary>
    /// 是否可以返回
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// 刷新当前视图
    /// </summary>
    void Refresh();

    /// <summary>
    /// 导航完成事件
    /// </summary>
    event EventHandler<ViewNavigationResult>? NavigationCompleted;
}
