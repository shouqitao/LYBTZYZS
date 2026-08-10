using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Tests.Desktop.Infrastructure;

/// <summary>
/// Desktop 纯 VM 单元测试基类（T2-2: 解除对 LocalDB/UserJourneyFixture 的耦合）
///
/// 只提供纯内存能力：WPF 环境初始化 + ViewModel 装配（mock 服务）+ 常用 mock 工厂。
/// 不再继承 IClassFixture&lt;UserJourneyFixture&gt;——12 个纯 VM 测试类（148 测试）不再被
/// SQL LocalDB 数据库初始化污染（P0-06 根因之一）。
///
/// 真需要 LocalDB 的测试（如 FrameworkVerificationTests）直接使用 UserJourneyFixture。
/// </summary>
public abstract class UserJourneyTestBase : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// 构造函数：确保 WPF 环境已初始化（Dispatcher/Application 资源）
    /// </summary>
    protected UserJourneyTestBase()
    {
        WpfTestHelper.InitializeWpf();
    }

    /// <summary>
    /// 创建 ViewModel 实例（纯内存 DI：mock 服务 + 可选额外配置，无数据库）
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel 类型</typeparam>
    /// <param name="additionalConfiguration">额外的服务配置</param>
    /// <returns>ViewModel 实例</returns>
    protected TViewModel CreateViewModel<TViewModel>(Action<IServiceCollection>? additionalConfiguration = null)
        where TViewModel : class
    {
        var services = new ServiceCollection();

        // 添加基础服务
        ConfigureBaseServices(services);

        // 添加 ViewModel
        services.AddTransient<TViewModel>();

        // 应用额外配置
        additionalConfiguration?.Invoke(services);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<TViewModel>();
    }

    /// <summary>
    /// 创建 ViewModel 实例（带工厂）
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel 类型</typeparam>
    /// <param name="factory">工厂方法</param>
    /// <returns>ViewModel 实例</returns>
    protected TViewModel CreateViewModel<TViewModel>(Func<IServiceProvider, TViewModel> factory)
        where TViewModel : class
    {
        var services = new ServiceCollection();
        ConfigureBaseServices(services);

        var provider = services.BuildServiceProvider();
        return factory(provider);
    }

    /// <summary>
    /// 创建 IViewModelServices mock
    /// </summary>
    protected IViewModelServices CreateViewModelServicesMock()
    {
        var mock = Substitute.For<IViewModelServices>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        loggerFactory.CreateLogger(default!).ReturnsForAnyArgs(logger);
        var eventAggregator = Substitute.For<IEventAggregator>();
        var regionManager = Substitute.For<IRegionManager>();
        var sessionManager = Substitute.For<ISessionManager>();
        var userNotificationService = Substitute.For<IUserNotificationService>();
        var commonDialogService = Substitute.For<ICommonDialogService>();
        var roleRegistry = Substitute.For<IRoleRegistry>();

        mock.LoggerFactory.Returns(loggerFactory);
        mock.EventAggregator.Returns(eventAggregator);
        mock.RegionManager.Returns(regionManager);
        mock.SessionManager.Returns(sessionManager);
        mock.UserNotificationService.Returns(userNotificationService);
        mock.CommonDialogService.Returns(commonDialogService);
        mock.RoleRegistry.Returns(roleRegistry);
        mock.UiThreadDispatcher.Returns(Substitute.For<IUiThreadDispatcher>());

        return mock;
    }

    /// <summary>
    /// 创建 IMasterDetailServices mock（子服务全部 mock，ExecuteWithLoadingAsync 实际执行）
    /// </summary>
    protected IMasterDetailServices<TList, TDetail> CreateMasterDetailServicesMock<TList, TDetail>()
        where TList : class
        where TDetail : class
    {
        var mock = Substitute.For<IMasterDetailServices<TList, TDetail>>();

        // 设置子服务 mocks
        var listViewServices = Substitute.For<IListViewServices<TList>>();
        var detailEditor = Substitute.For<IDetailEditorService<TDetail>>();
        var dialogManager = Substitute.For<IDialogManager>();
        var navigationCoordinator = Substitute.For<INavigationCoordinator>();
        var loadingState = Substitute.For<ILoadingStateManager>();
        var pagination = Substitute.For<IPaginationService>();
        var search = Substitute.For<ISearchService>();
        var selection = Substitute.For<ISelectionService<TList>>();
        var errorHandler = Substitute.For<IErrorHandler>();

        // 设置 ListViewServices 返回子服务
        listViewServices.Loading.Returns(loadingState);
        listViewServices.Pagination.Returns(pagination);
        listViewServices.Search.Returns(search);
        listViewServices.Selection.Returns(selection);
        listViewServices.ErrorHandler.Returns(errorHandler);

        // 设置 ExecuteWithLoadingAsync 实际执行传入的函数
        loadingState.ExecuteWithLoadingAsync(Arg.Any<Func<Task>>(), Arg.Any<string?>(), Arg.Any<bool>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>()());

        mock.List.Returns(listViewServices);
        mock.DetailEditor.Returns(detailEditor);
        mock.Dialog.Returns(dialogManager);
        mock.Navigation.Returns(navigationCoordinator);
        mock.Loading.Returns(loadingState);
        mock.Pagination.Returns(pagination);
        mock.Search.Returns(search);
        mock.Selection.Returns(selection);
        mock.ErrorHandler.Returns(errorHandler);

        return mock;
    }

    /// <summary>
    /// 配置基础服务（纯内存：日志 + 当前用户 Provider，无数据库）
    /// </summary>
    protected virtual void ConfigureBaseServices(IServiceCollection services)
    {
        // 日志
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // 当前用户 Provider
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        currentUserProvider.CurrentUserId.Returns(Guid.NewGuid());
        services.AddSingleton(currentUserProvider);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
