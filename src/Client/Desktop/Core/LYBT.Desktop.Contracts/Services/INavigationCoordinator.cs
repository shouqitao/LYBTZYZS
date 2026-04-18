using LYBT.Desktop.Contracts.Models;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.Services;
/// <summary>
/// 导航协调器接口 - 统一导航入口
/// OpenSpec: unify-navigation-architecture (ADR-3 + ADR-7)
/// 整合NavigationManager、ViewNavigationService、RoleNavigationService功能
/// 
/// 导航架构改进方案 v1.0 — 增加面包屑、前进导航、状态快照
/// </summary>
public interface INavigationCoordinator
{
    #region 基础导航 (原有)

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
    /// 导航到指定角色主页
    /// </summary>
    /// <param name="role">用户角色</param>
    void NavigateToHome(UserRole role);

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

    #endregion

    #region 前进导航 (导航架构改进方案 v1.0)

    /// <summary>
    /// 导航前进
    /// </summary>
    void NavigateForward();

    /// <summary>
    /// 是否可以前进
    /// </summary>
    bool CanNavigateForward { get; }

    #endregion

    #region 面包屑导航 (导航架构改进方案 v1.0)

    /// <summary>
    /// 当前面包屑列表
    /// </summary>
    IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; }

    /// <summary>
    /// 跳转到指定面包屑层级
    /// </summary>
    void NavigateToBreadcrumb(BreadcrumbItem item);

    #endregion

    #region 历史导航 (从ViewNavigationService整合)

    /// <summary>
    /// 导航历史记录
    /// </summary>
    IReadOnlyList<string> NavigationHistory { get; }

    /// <summary>
    /// 清除导航历史
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// 导航变更事件
    /// </summary>
    event EventHandler<NavigationChangedEventArgs>? NavigationChanged;

    #endregion

    #region Region管理 (从NavigationManager整合)

    /// <summary>
    /// 显示登录对话框
    /// </summary>
    void ShowLoginDialog();

    /// <summary>
    /// 清除登录区域
    /// </summary>
    void ClearLoginRegion();

    /// <summary>
    /// 清除内容区域
    /// </summary>
    void ClearContentRegion();

    #endregion

    #region 事件订阅 (从NavigationManager整合)

    /// <summary>
    /// 订阅Region集合变化事件（用于导航监控）
    /// </summary>
    void SubscribeToRegionCollection();

    /// <summary>
    /// 取消Region集合变化事件订阅
    /// </summary>
    void UnsubscribeFromRegionCollection();

    #endregion
}

/// <summary>
/// 导航变更事件参数
/// OpenSpec: unify-navigation-architecture (ADR-7)
/// </summary>
public class NavigationChangedEventArgs : EventArgs
{
    /// <summary>源视图</summary>
    public string? FromView { get; }

    /// <summary>目标视图</summary>
    public string ToView { get; }

    /// <summary>导航参数</summary>
    public IDictionary<string, object>? Parameters { get; }

    public NavigationChangedEventArgs(string? fromView, string toView, IDictionary<string, object>? parameters = null)
    {
        FromView = fromView;
        ToView = toView;
        Parameters = parameters;
    }
}
