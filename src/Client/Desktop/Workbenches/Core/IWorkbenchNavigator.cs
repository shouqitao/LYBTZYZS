using Prism.Regions;

namespace LYBT.Desktop.Workbench.Core
{

    /// <summary>
    /// 工作台导航器接口
    /// 每个工作台实现自己的导航逻辑
    /// </summary>
    public interface IWorkbenchNavigator
    {

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameters">导航参数</param>
        /// <returns>导航任务</returns>
        Task NavigateToAsync(string viewName, NavigationParameters? parameters = null);

        /// <summary>
        /// 导航到默认视图
        /// </summary>
        /// <returns>导航任务</returns>
        Task NavigateToDefaultAsync();

        /// <summary>
        /// 返回上一个视图
        /// </summary>
        /// <returns>导航任务</returns>
        Task GoBackAsync();

        /// <summary>
        /// 检查是否可以导航到指定视图
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <returns>是否可以导航</returns>
        bool CanNavigateTo(string viewName);

        /// <summary>
        /// 获取当前视图名称
        /// </summary>
        /// <returns>当前视图名称</returns>
        string GetCurrentView();

        /// <summary>
        /// 清除导航历史
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// 设置导航区域
        /// </summary>
        /// <param name="regionName">区域名称</param>
        void SetRegion(string regionName);

        /// <summary>
        /// 获取导航区域名称
        /// </summary>
        /// <returns>区域名称</returns>
        string GetRegionName();
    }
}
