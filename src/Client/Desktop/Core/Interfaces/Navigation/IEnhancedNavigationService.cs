using System;
using System.Threading.Tasks;
using Prism.Regions;

namespace LYBT.Desktop.Core.Interfaces.Navigation
{
    /// <summary>
    /// 增强导航服务接口
    /// 提供NavigationJournal支持，实现前进/后退功能
    /// </summary>
    public interface IEnhancedNavigationService
    {
        #region 导航操作

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameters">导航参数</param>
        /// <returns>导航结果</returns>
        Task<NavigationResult> NavigateAsync(string regionName, string viewName, NavigationParameters? parameters = null);

        /// <summary>
        /// 导航到指定视图（同步）
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameters">导航参数</param>
        void Navigate(string regionName, string viewName, NavigationParameters? parameters = null);

        /// <summary>
        /// 后退到上一个视图
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <returns>是否成功后退</returns>
        bool GoBack(string regionName);

        /// <summary>
        /// 后退到上一个视图（异步）
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <returns>是否成功后退</returns>
        Task<bool> GoBackAsync(string regionName);

        /// <summary>
        /// 前进到下一个视图
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <returns>是否成功前进</returns>
        bool GoForward(string regionName);

        /// <summary>
        /// 前进到下一个视图（异步）
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <returns>是否成功前进</returns>
        Task<bool> GoForwardAsync(string regionName);

        #endregion

        #region 导航状态

        /// <summary>
        /// 是否可以后退
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <returns>是否可以后退</returns>
        bool CanGoBack(string regionName);

        /// <summary>
        /// 是否可以前进
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <returns>是否可以前进</returns>
        bool CanGoForward(string regionName);

        /// <summary>
        /// 获取当前活动视图名称
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <returns>视图名称</returns>
        string? GetCurrentView(string regionName);

        /// <summary>
        /// 获取导航历史记录
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <returns>导航历史</returns>
        IRegionNavigationJournal? GetNavigationJournal(string regionName);

        #endregion

        #region 导航管理

        /// <summary>
        /// 清除指定区域的导航历史
        /// </summary>
        /// <param name="regionName">区域名称</param>
        void ClearHistory(string regionName);

        /// <summary>
        /// 清除所有区域的导航历史
        /// </summary>
        void ClearAllHistory();

        /// <summary>
        /// 移除指定区域的视图
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="viewName">视图名称</param>
        /// <returns>是否成功移除</returns>
        bool RemoveView(string regionName, string viewName);

        /// <summary>
        /// 判断视图是否已加载
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="viewName">视图名称</param>
        /// <returns>是否已加载</returns>
        bool IsViewLoaded(string regionName, string viewName);

        #endregion

        #region 事件

        /// <summary>
        /// 导航开始事件
        /// </summary>
        event EventHandler<NavigatingEventArgs> Navigating;

        /// <summary>
        /// 导航完成事件
        /// </summary>
        event EventHandler<NavigatedEventArgs> Navigated;

        /// <summary>
        /// 导航失败事件
        /// </summary>
        event EventHandler<NavigationFailedEventArgs> NavigationFailed;

        #endregion
    }

    #region 事件参数

    /// <summary>
    /// 导航开始事件参数
    /// </summary>
    public class NavigatingEventArgs : EventArgs
    {
        public string RegionName { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public NavigationParameters? Parameters { get; set; }
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// 导航完成事件参数
    /// </summary>
    public class NavigatedEventArgs : EventArgs
    {
        public string RegionName { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public NavigationParameters? Parameters { get; set; }
        public NavigationResult? Result { get; set; }
    }

    /// <summary>
    /// 导航失败事件参数
    /// </summary>
    public class NavigationFailedEventArgs : EventArgs
    {
        public string RegionName { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public Exception? Error { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    #endregion

    /// <summary>
    /// 导航结果
    /// </summary>
    public class NavigationResult
    {
        public bool Success { get; set; }
        public Exception? Error { get; set; }
        public NavigationContext? Context { get; set; }
    }

    /// <summary>
    /// 导航上下文
    /// </summary>
    public class NavigationContext
    {
        public Uri Uri { get; set; }
        public NavigationParameters Parameters { get; set; }

        public NavigationContext(Uri uri, NavigationParameters parameters)
        {
            Uri = uri;
            Parameters = parameters;
        }
    }
}