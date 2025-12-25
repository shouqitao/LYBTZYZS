namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 视图导航服务接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 提供区域导航、参数传递、导航历史功能，集成Prism IRegionManager
    /// </summary>
    public interface IViewNavigationService
    {
        /// <summary>当前视图名称</summary>
        string? CurrentView { get; }

        /// <summary>导航历史</summary>
        IReadOnlyList<string> NavigationHistory { get; }

        /// <summary>是否可以后退</summary>
        bool CanNavigateBack { get; }

        /// <summary>
        /// 导航变更事件
        /// </summary>
        event EventHandler<NavigationChangedEventArgs>? NavigationChanged;

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="regionName">区域名称</param>
        /// <param name="parameters">导航参数</param>
        Task NavigateToAsync(string viewName, string? regionName = null, IDictionary<string, object>? parameters = null);

        /// <summary>
        /// 后退
        /// </summary>
        Task NavigateBackAsync();

        /// <summary>
        /// 导航到详情视图
        /// </summary>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <param name="viewName">视图名称</param>
        /// <param name="id">详情ID</param>
        /// <param name="regionName">区域名称</param>
        Task NavigateToDetailAsync<TKey>(string viewName, TKey id, string? regionName = null);

        /// <summary>
        /// 清除导航历史
        /// </summary>
        void ClearHistory();
    }

    /// <summary>
    /// 导航变更事件参数
    /// </summary>
    public class NavigationChangedEventArgs : EventArgs
    {
        /// <summary>源视图</summary>
        public string? FromView { get; }

        /// <summary>目标视图</summary>
        public string ToView { get; }

        /// <summary>导航参数</summary>
        public IDictionary<string, object>? Parameters { get; }

        public NavigationChangedEventArgs(string? fromView, string toView, IDictionary<string, object>? parameters)
        {
            FromView = fromView;
            ToView = toView;
            Parameters = parameters;
        }
    }
}
