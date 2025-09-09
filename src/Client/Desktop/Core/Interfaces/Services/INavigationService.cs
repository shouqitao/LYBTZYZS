namespace LYBT.Desktop.Core.Interfaces.Services
{

    /// <summary>
    /// 导航服务接口
    /// </summary>
    public interface INavigationService
    {

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        Task NavigateToAsync(string viewName);

        /// <summary>
        /// 导航到指定视图并传递参数
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameters">导航参数</param>
        Task NavigateToAsync(string viewName, object parameters);

        /// <summary>
        /// 返回上一页
        /// </summary>
        Task GoBackAsync();

        /// <summary>
        /// 是否可以返回
        /// </summary>
        bool CanGoBack { get; }
    }
}
