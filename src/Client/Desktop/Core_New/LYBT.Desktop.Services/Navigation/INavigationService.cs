namespace LYBT.Desktop.Services.Navigation
{
    /// <summary>
    /// 导航服务接口 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则，提供基本的页面导航功能
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// 导航到指定视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameters">导航参数</param>
        /// <returns>导航是否成功</returns>
        Task<bool> NavigateAsync(string viewName, object? parameters = null);

        /// <summary>
        /// 返回上一页
        /// </summary>
        /// <returns>返回是否成功</returns>
        Task<bool> GoBackAsync();

        /// <summary>
        /// 是否可以返回
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// 清除导航历史
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// 导航到主页
        /// </summary>
        Task<bool> NavigateToHomeAsync();
    }

    /// <summary>
    /// 导航参数接口
    /// </summary>
    public interface INavigationAware
    {
        /// <summary>
        /// 导航到当前页面时调用
        /// </summary>
        void OnNavigatedTo(object? parameters);

        /// <summary>
        /// 导航离开当前页面时调用
        /// </summary>
        void OnNavigatedFrom();

        /// <summary>
        /// 是否可以导航离开
        /// </summary>
        bool CanNavigateAway();
    }
}
