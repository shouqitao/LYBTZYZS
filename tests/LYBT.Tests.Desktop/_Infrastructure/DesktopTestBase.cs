using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using System.Threading.Tasks;
using Xunit;

namespace LYBT.Tests.Desktop.Infrastructure;

/// <summary>
/// Desktop 统一测试基类（P0 冻结新规范）
/// 取代 UserJourneyTestBase / WebApiE2ETestBase 三套并存
/// </summary>
public abstract class DesktopTestBase : IAsyncLifetime
{
    protected ILoggerFactory LoggerFactory { get; }
    protected IUiThreadDispatcher UiDispatcher { get; }
    protected IViewModelServices Services { get; }

    protected DesktopTestBase()
    {
        LoggerFactory = Substitute.For<ILoggerFactory>();
        LoggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        // 同步版 UiThreadDispatcher（测试专用）
        UiDispatcher = Substitute.For<IUiThreadDispatcher>();
        UiDispatcher.InvokeAsync(Arg.Any<Action>()).Returns(ci => { ci.Arg<Action>()(); return Task.CompletedTask; });
        UiDispatcher.InvokeAsync(Arg.Any<Func<Task>>()).Returns(ci => ci.Arg<Func<Task>>()());

        Services = Substitute.For<IViewModelServices>();
        Services.UiThreadDispatcher.Returns(UiDispatcher);
        Services.LoggerFactory.Returns(LoggerFactory);
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;
    public virtual Task DisposeAsync() => Task.CompletedTask;

    // 统一 MasterDetail Services Mock 工厂（收敛 UserJourneyTestBase 重复）
    protected IMasterDetailServices<TList, TDetail> CreateMasterDetailServicesMock<TList, TDetail>()
        where TList : class where TDetail : class
    {
        var services = Substitute.For<IMasterDetailServices<TList, TDetail>>();
        return services;
    }

    protected IViewModelServices CreateViewModelServicesMock()
    {
        var mock = Substitute.For<IViewModelServices>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        loggerFactory.CreateLogger(default!).ReturnsForAnyArgs(logger);
        var eventAggregator = Substitute.For<Prism.Events.IEventAggregator>();
        var regionManager = Substitute.For<Prism.Regions.IRegionManager>();
        var sessionManager = Substitute.For<ISessionManager>();
        var userNotificationService = Substitute.For<IUserNotificationService>();
        var commonDialogService = Substitute.For<ICommonDialogService>();
        var roleRegistry = Substitute.For<LYBT.Desktop.Contracts.Roles.IRoleRegistry>();

        mock.LoggerFactory.Returns(loggerFactory);
        mock.EventAggregator.Returns(eventAggregator);
        mock.RegionManager.Returns(regionManager);
        mock.SessionManager.Returns(sessionManager);
        mock.UserNotificationService.Returns(userNotificationService);
        mock.CommonDialogService.Returns(commonDialogService);
        mock.RoleRegistry.Returns(roleRegistry);
        mock.UiThreadDispatcher.Returns(UiDispatcher);

        return mock;
    }
}
