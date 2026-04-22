using System.Collections.ObjectModel;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// 增强型导航服务接口 - Phase 2.1: Navigation Improvements
    /// 提供集中化、统一的导航功能，包括历史记录、面包屑、状态恢复等
    /// </summary>
    public interface IEnhancedNavigationService
    {
        /// <summary>
        /// 导航到指定 URI，自动跟踪历史记录和状态
        /// </summary>
        /// <param name="uri">目标导航 URI</param>
        /// <param name="parameters">导航参数</param>
        /// <returns>导航是否成功</returns>
        Task<bool> NavigateAsync(string uri, NavigationParameters parameters = null!);

        /// <summary>
        /// 导航到指定区域和视图名称
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameters">导航参数</param>
        /// <returns>导航是否成功</returns>
        Task<bool> NavigateToRegionAsync(string regionName, string viewName, NavigationParameters parameters = null!);

        /// <summary>
        /// 返回到上一个导航位置
        /// </summary>
        /// <returns>是否成功返回</returns>
        Task<bool> GoBackAsync();

        /// <summary>
        /// 前进到下一个导航位置（在返回后）
        /// </summary>
        /// <returns>是否成功前进</returns>
        Task<bool> GoForwardAsync();

        /// <summary>
        /// 导航到指定主页
        /// </summary>
        /// <returns>是否成功导航</returns>
        Task<bool> NavigateHomeAsync();

        /// <summary>
        /// 清除导航历史
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// 获取当前导航状态
        /// </summary>
        NavigationEntry CurrentEntry { get; }

        /// <summary>
        /// 导航历史记录（只读）
        /// </summary>
        ReadOnlyObservableCollection<NavigationEntry> History { get; }

        /// <summary>
        /// 前进栈（只读）
        /// </summary>
        ReadOnlyObservableCollection<NavigationEntry> ForwardStack { get; }

        /// <summary>
        /// 面包屑导航路径（只读）
        /// </summary>
        ReadOnlyObservableCollection<BreadcrumbItem> Breadcrumbs { get; }

        /// <summary>
        /// 是否可以返回
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// 是否可以前进
        /// </summary>
        bool CanGoForward { get; }

        /// <summary>
        /// 获取导航建议（基于上下文和频率）
        /// </summary>
        /// <param name="count">返回的建议数量</param>
        /// <returns>导航建议列表</returns>
        IEnumerable<NavigationSuggestion> GetSuggestions(int count = 5);

        /// <summary>
        /// 导航完成事件
        /// </summary>
        event EventHandler<NavigatedEventArgs> Navigated;

        /// <summary>
        /// 导航取消事件
        /// </summary>
        event EventHandler<NavigationCancelledEventArgs> NavigationCancelled;

        /// <summary>
        /// 导航失败事件
        /// </summary>
        event EventHandler<NavigationFailedEventArgs> NavigationFailed;
    }

    #region Event Arguments

    /// <summary>
    /// 导航完成事件参数
    /// </summary>
    public class NavigatedEventArgs : EventArgs
    {
        public NavigationEntry Entry { get; init; } = null!;
        public bool IsBack { get; init; }
        public bool IsForward { get; init; }
    }

    /// <summary>
    /// 导航取消事件参数
    /// </summary>
    public class NavigationCancelledEventArgs : EventArgs
    {
        public string Uri { get; init; } = null!;
        public string Reason { get; init; } = null!;
    }

    /// <summary>
    /// 导航失败事件参数
    /// </summary>
    public class NavigationFailedEventArgs : EventArgs
    {
        public string Uri { get; init; } = null!;
        public Exception Exception { get; init; } = null!;
        public string ErrorMessage { get; init; } = null!;
    }

    #endregion
}
