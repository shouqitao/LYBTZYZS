namespace LYBT.Desktop.Contracts.Services.MasterDetail;

/// <summary>
/// MasterDetail模式的服务聚合接口
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 将8个服务接口聚合为一个注入点，简化ViewModel构造函数
/// </summary>
public interface IMasterDetailServices
{
    /// <summary>
    /// 加载状态管理
    /// </summary>
    ILoadingStateManager LoadingStateManager { get; }

    /// <summary>
    /// 分页服务
    /// </summary>
    IPaginationService PaginationService { get; }

    /// <summary>
    /// 搜索服务
    /// </summary>
    ISearchService SearchService { get; }

    /// <summary>
    /// 选择服务
    /// </summary>
    ISelectionService SelectionService { get; }

    /// <summary>
    /// 详情编辑服务
    /// </summary>
    IDetailEditorService DetailEditorService { get; }

    /// <summary>
    /// 对话框管理
    /// </summary>
    IDialogManager DialogManager { get; }

    /// <summary>
    /// 视图导航服务
    /// </summary>
    IViewNavigationService ViewNavigationService { get; }

    /// <summary>
    /// 错误处理
    /// </summary>
    IErrorHandler ErrorHandler { get; }
}

/// <summary>
/// 泛型MasterDetail服务接口
/// 提供类型安全的选择服务
/// </summary>
/// <typeparam name="TListItem">列表项类型</typeparam>
public interface IMasterDetailServices<TListItem> : IMasterDetailServices where TListItem : class
{
    /// <summary>
    /// 选择服务（强类型）
    /// </summary>
    new ISelectionService<TListItem> SelectionService { get; }
}
