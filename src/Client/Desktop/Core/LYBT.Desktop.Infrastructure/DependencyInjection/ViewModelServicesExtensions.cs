using LYBT.Desktop.Contracts.Performance;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Performance;
using LYBT.Desktop.Infrastructure.Services;
using Prism.Ioc;

namespace LYBT.Desktop.Infrastructure.DependencyInjection
{
    /// <summary>
    /// ViewModel服务DI注册扩展
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 使用Prism.Ioc的IContainerRegistry接口，与Shell层注册模式保持一致
    /// </summary>
    public static class ViewModelServicesExtensions
    {
        /// <summary>
        /// 注册所有ViewModel服务
        /// </summary>
        /// <param name="containerRegistry">Prism容器注册表</param>
        /// <returns>容器注册表实例</returns>
        public static IContainerRegistry AddViewModelServices(this IContainerRegistry containerRegistry)
        {
            // OpenSpec: enhance-viewmodel-architecture - ViewModel服务聚合
            containerRegistry.RegisterSingleton<IViewModelServices, ViewModelServices>();

            // 共享服务 - Singleton
            containerRegistry.RegisterSingleton<IDialogManager, DialogManager>();
            // [已删除] IViewNavigationService - OpenSpec: unify-navigation-architecture (ADR-7)
            containerRegistry.RegisterSingleton<IAsyncExecutor, AsyncExecutor>();

            // 有状态服务 - Transient (每个ViewModel实例独立)
            containerRegistry.Register<ILoadingStateManager, LoadingStateManager>();
            containerRegistry.Register<IPaginationService, PaginationService>();
            containerRegistry.Register<ISearchService, SearchService>();
            containerRegistry.Register<IErrorHandler, ErrorHandler>();

            // 泛型服务 - Transient
            containerRegistry.Register(typeof(ISelectionService<>), typeof(SelectionService<>));
            containerRegistry.Register(typeof(IDetailEditorService<>), typeof(DetailEditorService<>));

            // 组合服务 - Transient
            containerRegistry.Register(typeof(IListViewServices<>), typeof(ListViewServices<>));
            containerRegistry.Register(typeof(IMasterDetailServices<,>), typeof(MasterDetailServices<,>));

            // 性能监控服务 - Singleton
            containerRegistry.RegisterSingleton<IPerformanceMonitor, PerformanceMonitor>();

            return containerRegistry;
        }

        /// <summary>
        /// 注册列表视图服务
        /// </summary>
        /// <typeparam name="T">列表项类型</typeparam>
        /// <param name="containerRegistry">Prism容器注册表</param>
        /// <returns>容器注册表实例</returns>
        public static IContainerRegistry AddListViewServices<T>(this IContainerRegistry containerRegistry) where T : class
        {
            containerRegistry.Register<ISelectionService<T>, SelectionService<T>>();
            containerRegistry.Register<IListViewServices<T>, ListViewServices<T>>();
            return containerRegistry;
        }

        /// <summary>
        /// 注册Master-Detail视图服务
        /// </summary>
        /// <typeparam name="TListItem">列表项类型</typeparam>
        /// <typeparam name="TDetail">详情模型类型</typeparam>
        /// <param name="containerRegistry">Prism容器注册表</param>
        /// <returns>容器注册表实例</returns>
        public static IContainerRegistry AddMasterDetailServices<TListItem, TDetail>(this IContainerRegistry containerRegistry)
            where TListItem : class
            where TDetail : class
        {
            containerRegistry.Register<ISelectionService<TListItem>, SelectionService<TListItem>>();
            containerRegistry.Register<IDetailEditorService<TDetail>, DetailEditorService<TDetail>>();
            containerRegistry.Register<IListViewServices<TListItem>, ListViewServices<TListItem>>();
            containerRegistry.Register<IMasterDetailServices<TListItem, TDetail>, MasterDetailServices<TListItem, TDetail>>();
            return containerRegistry;
        }
    }
}
