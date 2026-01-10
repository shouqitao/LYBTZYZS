namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 导航协调器接口 - 统一导航入口
/// OpenSpec: unify-navigation-architecture (ADR-3)
/// 整合NavigationManager和RoleNavigationService功能
/// </summary>
public interface INavigationCoordinator
{
    /// <summary>
    /// 导航到指定视图
    /// </summary>
    /// <param name="viewName">视图名称（建议使用ViewNames常量）</param>
    /// <param name="parameters">导航参数（键值对形式）</param>
    void NavigateTo(string viewName, IDictionary<string, object>? parameters = null);

    /// <summary>
    /// 导航到当前角色主页
    /// </summary>
    void NavigateToHome();

    /// <summary>
    /// 导航后退
    /// </summary>
    void NavigateBack();

    /// <summary>
    /// 是否可以后退
    /// </summary>
    bool CanNavigateBack { get; }

    /// <summary>
    /// 当前视图名称
    /// </summary>
    string? CurrentView { get; }
}
