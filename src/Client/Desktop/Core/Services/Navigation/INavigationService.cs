using Prism.Regions;

namespace LYBT.Desktop.Core.Services.Navigation;

/// <summary>
/// 集中式导航服务接口
/// 提供统一的导航管理，解决导航逻辑分散问题
/// 重构：Prism 8.1.97架构优化，集中管理所有导航操作
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 导航到指定视图
    /// </summary>
    /// <param name="viewName">视图名称</param>
    /// <param name="parameters">导航参数</param>
    void NavigateTo(string viewName, NavigationParameters? parameters = null);

    /// <summary>
    /// 导航到指定区域的指定视图
    /// </summary>
    /// <param name="regionName">区域名称</param>
    /// <param name="viewName">视图名称</param>
    /// <param name="parameters">导航参数</param>
    void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null);

    /// <summary>
    /// 异步导航到指定视图
    /// </summary>
    /// <param name="viewName">视图名称</param>
    /// <param name="parameters">导航参数</param>
    /// <returns>导航任务</returns>
    Task NavigateToAsync(string viewName, NavigationParameters? parameters = null);

    /// <summary>
    /// 异步导航到指定区域的指定视图
    /// </summary>
    /// <param name="regionName">区域名称</param>
    /// <param name="viewName">视图名称</param>
    /// <param name="parameters">导航参数</param>
    /// <returns>导航任务</returns>
    Task NavigateToAsync(string regionName, string viewName, NavigationParameters? parameters = null);

    /// <summary>
    /// 导航回退
    /// </summary>
    void NavigateBack();

    /// <summary>
    /// 导航回退到指定区域
    /// </summary>
    /// <param name="regionName">区域名称</param>
    void NavigateBack(string regionName);

    /// <summary>
    /// 是否可以导航回退
    /// </summary>
    bool CanNavigateBack { get; }

    /// <summary>
    /// 当前视图名称
    /// </summary>
    string? CurrentView { get; }

    /// <summary>
    /// 获取指定区域的当前视图
    /// </summary>
    /// <param name="regionName">区域名称</param>
    /// <returns>当前视图名称</returns>
    string? GetCurrentView(string regionName);

    /// <summary>
    /// 导航完成事件
    /// </summary>
    event EventHandler<NavigationEventArgs>? Navigated;

    /// <summary>
    /// 导航失败事件
    /// </summary>
    event EventHandler<NavigationFailedEventArgs>? NavigationFailed;
}

/// <summary>
/// 导航事件参数
/// </summary>
public class NavigationEventArgs : EventArgs
{
    public string ViewName { get; }
    public string? RegionName { get; }
    public NavigationParameters? Parameters { get; }
    public bool IsSuccess { get; }

    public NavigationEventArgs(string viewName, string? regionName = null,
        NavigationParameters? parameters = null, bool isSuccess = true)
    {
        ViewName = viewName;
        RegionName = regionName;
        Parameters = parameters;
        IsSuccess = isSuccess;
    }
}

/// <summary>
/// 导航失败事件参数
/// </summary>
public class NavigationFailedEventArgs : NavigationEventArgs
{
    public Exception Exception { get; }
    public string ErrorMessage { get; }

    public NavigationFailedEventArgs(string viewName, Exception exception,
        string? regionName = null, NavigationParameters? parameters = null)
        : base(viewName, regionName, parameters, false)
    {
        Exception = exception;
        ErrorMessage = exception.Message;
    }
}