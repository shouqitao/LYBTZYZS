namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 列表视图服务组合接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 组合列表视图所需的所有服务：加载状态、分页、搜索、选择
    /// </summary>
    /// <typeparam name="T">列表项类型</typeparam>
    public interface IListViewServices<T> where T : class
    {
        /// <summary>加载状态管理服务</summary>
        ILoadingStateManager Loading { get; }

        /// <summary>分页服务</summary>
        IPaginationService Pagination { get; }

        /// <summary>搜索服务</summary>
        ISearchService Search { get; }

        /// <summary>选择服务</summary>
        ISelectionService<T> Selection { get; }

        /// <summary>错误处理服务</summary>
        IErrorHandler ErrorHandler { get; }

        /// <summary>异步执行服务</summary>
        IAsyncExecutor AsyncExecutor { get; }

        /// <summary>
        /// 释放所有服务资源
        /// </summary>
        void Dispose();

        /// <summary>
        /// 重置所有服务状态
        /// </summary>
        void ResetAll();
    }
}
