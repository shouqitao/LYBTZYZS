using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.LocalData.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Tests.Desktop.Infrastructure;

/// <summary>
/// User Journey 测试基类
/// 提供 ViewModel 实例化、ServiceProvider 创建和测试数据管理功能
/// </summary>
public abstract class UserJourneyTestBase : IClassFixture<UserJourneyFixture>, IDisposable
{
    private readonly UserJourneyFixture _fixture;
    private readonly IServiceScope _scope;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    protected UserJourneyTestBase(UserJourneyFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _scope = _fixture.CreateScope();

        // 确保 WPF 环境已初始化
        WpfTestHelper.InitializeWpf();
    }

    /// <summary>
    /// 获取当前作用域的 ServiceProvider
    /// </summary>
    protected IServiceProvider ServiceProvider => _scope.ServiceProvider;

    /// <summary>
    /// 获取 LocalDbContext 实例
    /// </summary>
    protected LocalDbContext DbContext => ServiceProvider.GetRequiredService<LocalDbContext>();

    /// <summary>
    /// 创建 ViewModel 实例（使用真实 Repository 和 mock 服务）
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
    /// 创建 ViewModel 实例（带参数）
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

        return mock;
    }

    /// <summary>
    /// 创建 IMasterDetailServices mock
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
        var asyncExecutor = Substitute.For<IAsyncExecutor>();

        // 设置 ListViewServices 返回子服务
        listViewServices.Loading.Returns(loadingState);
        listViewServices.Pagination.Returns(pagination);
        listViewServices.Search.Returns(search);
        listViewServices.Selection.Returns(selection);
        listViewServices.ErrorHandler.Returns(errorHandler);
        listViewServices.AsyncExecutor.Returns(asyncExecutor);

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
        mock.AsyncExecutor.Returns(asyncExecutor);

        return mock;
    }

    /// <summary>
    /// 配置基础服务
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

        // LocalDbContext (使用与夹具相同的连接)
        var connection = _fixture.DbContext.Database.GetDbConnection();
        services.AddDbContext<LocalDbContext>(options =>
        {
            options.UseSqlite(connection);
        }, ServiceLifetime.Scoped);

        // 添加真实 Repository（从夹具的作用域解析）
        services.AddScoped(_ => ServiceProvider.GetRequiredService<LocalDbContext>());
    }

    /// <summary>
    /// 异步保存更改到数据库
    /// </summary>
    protected async Task SaveChangesAsync()
    {
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 重置数据库状态
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _scope?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// 使用共享夹具的 User Journey 测试基类
/// 适用于需要跨测试保持状态的长时间运行测试
/// </summary>
[Collection("UserJourney")]
public abstract class UserJourneyTestBaseShared : IDisposable
{
    private readonly UserJourneyFixture _fixture;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    protected UserJourneyTestBaseShared(UserJourneyFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

        // 确保 WPF 环境已初始化
        WpfTestHelper.InitializeWpf();
    }

    /// <summary>
    /// 获取共享的 ServiceProvider
    /// </summary>
    protected IServiceProvider ServiceProvider => _fixture.ServiceProvider;

    /// <summary>
    /// 获取共享的 LocalDbContext
    /// </summary>
    protected LocalDbContext DbContext => _fixture.DbContext;

    /// <summary>
    /// 创建新的作用域
    /// </summary>
    protected IServiceScope CreateScope() => _fixture.CreateScope();

    /// <summary>
    /// 创建 ViewModelServices mock
    /// </summary>
    protected IViewModelServices CreateViewModelServicesMock()
    {
        var mock = Substitute.For<IViewModelServices>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
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

        return mock;
    }

    /// <summary>
    /// 创建 IMasterDetailServices mock
    /// </summary>
    protected IMasterDetailServices<TList, TDetail> CreateMasterDetailServicesMock<TList, TDetail>()
        where TList : class
        where TDetail : class
    {
        var mock = Substitute.For<IMasterDetailServices<TList, TDetail>>();

        var listViewServices = Substitute.For<IListViewServices<TList>>();
        var detailEditor = Substitute.For<IDetailEditorService<TDetail>>();
        var dialogManager = Substitute.For<IDialogManager>();
        var navigationCoordinator = Substitute.For<INavigationCoordinator>();
        var loadingState = Substitute.For<ILoadingStateManager>();
        var pagination = Substitute.For<IPaginationService>();
        var search = Substitute.For<ISearchService>();
        var selection = Substitute.For<ISelectionService<TList>>();
        var errorHandler = Substitute.For<IErrorHandler>();
        var asyncExecutor = Substitute.For<IAsyncExecutor>();

        listViewServices.Loading.Returns(loadingState);
        listViewServices.Pagination.Returns(pagination);
        listViewServices.Search.Returns(search);
        listViewServices.Selection.Returns(selection);
        listViewServices.ErrorHandler.Returns(errorHandler);
        listViewServices.AsyncExecutor.Returns(asyncExecutor);

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
        mock.AsyncExecutor.Returns(asyncExecutor);

        return mock;
    }

    /// <summary>
    /// 重置数据库状态
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // 共享夹具不在这里释放
            _disposed = true;
        }
    }
}
