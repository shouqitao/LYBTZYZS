using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// Master-Detail视图服务组合接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 组合Master-Detail视图所需的所有服务：列表服务 + 详情编辑服务
    /// </summary>
    /// <typeparam name="TListItem">列表项类型</typeparam>
    /// <typeparam name="TDetail">详情模型类型</typeparam>
    public interface IMasterDetailServices<TListItem, TDetail>
        where TListItem : class
        where TDetail : class
    {
        /// <summary>列表视图服务</summary>
        IListViewServices<TListItem> List { get; }

        /// <summary>详情编辑服务</summary>
        IDetailEditorService<TDetail> DetailEditor { get; }

        /// <summary>对话框管理服务</summary>
        IDialogManager Dialog { get; }

        /// <summary>导航协调器 (OpenSpec: unify-navigation-architecture ADR-7)</summary>
        INavigationCoordinator Navigation { get; }

        // === 便捷属性委托 ===

        /// <summary>加载状态管理服务（委托到List.Loading）</summary>
        ILoadingStateManager Loading { get; }

        /// <summary>分页服务（委托到List.Pagination）</summary>
        IPaginationService Pagination { get; }

        /// <summary>搜索服务（委托到List.Search）</summary>
        ISearchService Search { get; }

        /// <summary>选择服务（委托到List.Selection）</summary>
        ISelectionService<TListItem> Selection { get; }

        /// <summary>错误处理服务（委托到List.ErrorHandler）</summary>
        IErrorHandler ErrorHandler { get; }

        /// <summary>异步执行服务（委托到List.AsyncExecutor）</summary>
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
