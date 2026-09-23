using System.Collections.ObjectModel;
using System.ComponentModel;
using FluentAssertions;
using LYBT.Desktop.Controls.Models;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Shell;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Login;
using LYBT.Desktop.Shell.ViewModels;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// Shell 三控件（Header/SideNav/Footer）ViewModel 绑定契约测试。
/// <para>回归背景（P1）：三控件曾因 <c>AutoWireViewModel</c> 约定名不匹配而静默拿不到 DataContext，
/// 导致侧栏导航列表/深色开关、底栏状态/时间全部失效。此处以真实实例验证状态来源与绑定面：
/// 侧栏展开态/宽度 = ISidebarStateManager（SSOT，宿主与侧栏共用）；主题 = IThemeService（SSOT）；
/// 状态栏文本随真实健康状态变化（不得硬编码）。</para>
/// </summary>
public class ShellViewModelBindingTests
{
    private readonly SidebarStateManager _sidebar = new();
    private readonly IThemeService _theme = Substitute.For<IThemeService, INotifyPropertyChanged>();
    private readonly IStatusBarManager _statusBar = Substitute.For<IStatusBarManager, INotifyPropertyChanged>();
    private readonly ILoginStateManager _loginState = Substitute.For<ILoginStateManager>();
    private readonly INavigationManager _navigationManager = Substitute.For<INavigationManager>();
    private readonly IShellServices _shell = Substitute.For<IShellServices>();
    private readonly IActiveConsultationService _activeConsultation = Substitute.For<IActiveConsultationService>();
    private readonly IDialogManager _dialogManager = Substitute.For<IDialogManager>();
    private readonly IShellLogoutService _logoutService = Substitute.For<IShellLogoutService>();
    private readonly IMenuManager _menu = Substitute.For<IMenuManager>();
    private readonly IApplicationTickService _applicationTickService = Substitute.For<IApplicationTickService>();
    private readonly ISessionTimeoutMonitor _sessionTimeoutMonitor = Substitute.For<ISessionTimeoutMonitor>();
    private readonly Prism.Commands.DelegateCommand _navigateBackCommand = new(() => { });

    public ShellViewModelBindingTests()
    {
        _navigationManager.NavigationItems.Returns(new ObservableCollection<NavigationItem>
        {
            new() { Title = "患者管理", Group = "业务", IconKind = "AccountSearch" },
            new() { Title = "系统设置", Group = "管理", IconKind = "AccountCog" },
        });

        _menu.NavigateBackCommand.Returns(_navigateBackCommand);
        _shell.Menu.Returns(_menu);
        _shell.Sidebar.Returns(_sidebar);
        _shell.Theme.Returns(_theme);
        _shell.StatusBar.Returns(_statusBar);
        _shell.LoginState.Returns(_loginState);
        _shell.Navigation.Returns(_navigationManager);
        _shell.ActiveConsultation.Returns(_activeConsultation);
        _shell.Logout.Returns(_logoutService);
        // 真实 ShellDialogHelper（其方法非 virtual 无法被 NSubstitute 拦截）→ 底层 IDialogManager 可控
        _shell.Dialogs.Returns(new ShellDialogHelper(
            _dialogManager,
            null,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<ShellDialogHelper>>()));
    }

    private SideNavViewModel CreateSideNav() => new(_shell, _navigationManager);

    private MainWindowViewModel CreateHost()
    {
        // ShellEventCoordinator ctor 以 ThreadOption.UIThread 订阅 → 构造期需存在 SynchronizationContext
        // （Prism 要求 EventAggregator 在 UI 线程构造）。仅在构造窗口内临时提供，随后还原，避免污染测试线程。
        var previousContext = SynchronizationContext.Current;
        if (previousContext is null)
        {
            SynchronizationContext.SetSynchronizationContext(
                new System.Windows.Threading.DispatcherSynchronizationContext(
                    System.Windows.Threading.Dispatcher.CurrentDispatcher));
        }

        try
        {
            // 先构造真实协调器再 Returns（NSubstitute 禁止在 Returns 参数内触发其他 substitute 调用）
            var events = new ShellEventCoordinator(
                Substitute.For<IShellEventServices>(),
                new Prism.Events.EventAggregator(),
                Substitute.For<IApiClient>(),
                Substitute.For<Prism.Services.Dialogs.IDialogService>(),
                Substitute.For<IFirstRunStateService>(),
                _applicationTickService,
                _sessionTimeoutMonitor,
                Substitute.For<Microsoft.Extensions.Logging.ILogger<ShellEventCoordinator>>());
            _shell.Events.Returns(events);

            var services = Substitute.For<IViewModelServices>();
            services.LoggerFactory.Returns(Substitute.For<Microsoft.Extensions.Logging.ILoggerFactory>());
            return new MainWindowViewModel(
                services,
                _shell,
                Substitute.For<INavigationCoordinator>(),
                _navigationManager);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    [Fact]
    public void CtrlM_ToggleCommand_And_SideNavHamburger_ShareSingleSidebarState()
    {
        using var sideNav = CreateSideNav();
        using var host = CreateHost();
        var hostChanges = new List<string>();
        host.PropertyChanged += (_, e) => hostChanges.Add(e.PropertyName ?? string.Empty);

        // Ctrl+M（宿主命令）→ 侧栏汉堡按钮经同一 SSOT 观察（P1 回归：此前两侧各持一份状态）
        host.ToggleSidebarCommand.Execute(null);
        host.IsSidebarExpanded.Should().BeTrue();
        sideNav.IsSidebarExpanded.Should().BeTrue("宿主 Ctrl+M 与侧栏汉堡必须同源");
        sideNav.SidebarWidth.Should().Be(ShellConstants.SidebarExpandedWidth);
        sideNav.SidebarWidth.Should().Be(ShellConstants.SidebarExpandedWidth);
        hostChanges.Should().Contain(nameof(MainWindowViewModel.SidebarWidth), "宽度变化需通知绑定刷新");

        // 侧栏汉堡（双向绑定 IsSidebarExpanded）→ 宿主同样观察
        sideNav.IsSidebarExpanded = false;
        host.IsSidebarExpanded.Should().BeFalse();
        sideNav.SidebarWidth.Should().Be(ShellConstants.SidebarCollapsedWidth);
        sideNav.SidebarWidth.Should().Be(ShellConstants.SidebarCollapsedWidth);
    }

    [Fact]
    public void SideNav_IsNavTextVisible_DerivesFromSharedState_AndNotifies()
    {
        using var sideNav = CreateSideNav();
        var changes = new List<string>();
        sideNav.PropertyChanged += (_, e) => changes.Add(e.PropertyName ?? string.Empty);

        sideNav.IsNavTextVisible.Should().BeFalse();
        _sidebar.Toggle();

        sideNav.IsNavTextVisible.Should().BeTrue();
        changes.Should().Contain(nameof(SideNavViewModel.IsNavTextVisible));
        changes.Should().Contain(nameof(SideNavViewModel.SidebarWidth));
    }

    [Fact]
    public void SideNav_IsDarkMode_ProxiesThemeService_WithoutLocalState()
    {
        using var sideNav = CreateSideNav();
        _theme.IsDarkMode.Returns(false);

        sideNav.IsDarkMode.Should().BeFalse();
        sideNav.IsDarkMode = true;

        _theme.Received(1).ApplyTheme(true);

        // 主题状态由 ThemeService 广播 → 代理属性跟随（不因本地字段而漂移）
        _theme.IsDarkMode.Returns(true);
        ((INotifyPropertyChanged)_theme).PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _theme,
            new PropertyChangedEventArgs(nameof(IThemeService.IsDarkMode)));
        sideNav.IsDarkMode.Should().BeTrue();
    }

    [Fact]
    public void Footer_ApiStatusText_ReflectsHealthStatus_InsteadOfHardcodedText()
    {
        _statusBar.ApiStatus.Returns(ApiHealthStatus.Checking);
        using var footer = new FooterViewModel(_shell);
        footer.ApiStatusText.Should().Be("API 检测中…");

        _statusBar.ApiStatus.Returns(ApiHealthStatus.Healthy);
        footer.ApiStatusText.Should().Be("API 已连接");

        _statusBar.ApiStatus.Returns(ApiHealthStatus.Unhealthy);
        footer.ApiStatusText.Should().Be("API 未连接");
        footer.ApiStatusIcon.Should().Be(_statusBar.ApiStatusIcon);
    }

    [Fact]
    public void Footer_Notifies_When_StatusBarReportsChange()
    {
        _statusBar.ApiStatus.Returns(ApiHealthStatus.Healthy);
        using var footer = new FooterViewModel(_shell);
        var changes = new List<string>();
        footer.PropertyChanged += (_, e) => changes.Add(e.PropertyName ?? string.Empty);

        _statusBar.ApiStatus.Returns(ApiHealthStatus.Healthy);
        ((INotifyPropertyChanged)_statusBar).PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
            _statusBar,
            new PropertyChangedEventArgs(nameof(IStatusBarManager.ApiStatus)));

        changes.Should().Contain(nameof(FooterViewModel.ApiStatusText));
        changes.Should().Contain(nameof(FooterViewModel.ApiStatusIcon));
    }

    [Fact]
    public void Header_ProxiesLoginState_AndNotifiesOnLoginStateChanged()
    {
        _loginState.CurrentUserDisplayName.Returns("陈医生");
        _loginState.CurrentUserRoleDisplay.Returns("医生");
        using var header = new HeaderViewModel(_shell);
        var changes = new List<string>();
        header.PropertyChanged += (_, e) => changes.Add(e.PropertyName ?? string.Empty);

        header.CurrentUserDisplayName.Should().Be("陈医生");
        header.CurrentUserRoleDisplay.Should().Be("医生");
        header.EditProfileCommand.Should().BeSameAs(_shell.Menu.EditProfileCommand);
        header.NavigateBackCommand.Should().BeSameAs(_shell.Menu.NavigateBackCommand);

        _loginState.LoginStateChanged += Raise.Event<EventHandler>(_loginState, EventArgs.Empty);
        changes.Should().Contain(nameof(HeaderViewModel.CurrentUserDisplayName));
        changes.Should().Contain(nameof(HeaderViewModel.CurrentUserRoleDisplay));
    }

    [Fact]
    public void SideNav_GroupedNavigationItems_GroupsByGroupingProperty()
    {
        using var sideNav = CreateSideNav();

        var view = sideNav.GroupedNavigationItems;

        view.Should().NotBeNull();
        view!.GroupDescriptions.Should().HaveCount(1);
        view.GroupDescriptions[0].Should().BeOfType<System.Windows.Data.PropertyGroupDescription>()
            .Which.PropertyName.Should().Be(nameof(NavigationItem.Group), "XAML GroupStyle 头绑定 {Binding Name} 依赖该分组");
        view.Cast<object>().Should().HaveCount(2);
    }

    [Fact]
    public void SidebarStateManager_Toggle_FlipsStateAndNotifies()
    {
        var changes = new List<string>();
        _sidebar.PropertyChanged += (_, e) => changes.Add(e.PropertyName ?? string.Empty);

        _sidebar.Toggle();

        _sidebar.IsSidebarExpanded.Should().BeTrue();
        _sidebar.IsNavTextVisible.Should().BeTrue();
        _sidebar.SidebarWidth.Should().Be(ShellConstants.SidebarExpandedWidth);
        changes.Should().Contain(nameof(ISidebarStateManager.SidebarWidth));
        changes.Should().Contain(nameof(ISidebarStateManager.IsNavTextVisible));
    }

    #region 登出守卫（既有审查 #34：侧栏登出曾绕过活跃医案离开守卫）

    private ShellLogoutService CreateLogoutService() => new(
        _activeConsultation,
        _loginState,
        _shell.Dialogs,
        Substitute.For<Microsoft.Extensions.Logging.ILogger<ShellLogoutService>>());

    [Fact]
    public async Task ShellLogout_ActiveConsultation_CannotLeave_KeepsSession()
    {
        _activeConsultation.HasActiveConsultation.Returns(true);
        _activeConsultation.RequestLeaveAsync().Returns(new LeaveConsultationResult { CanLeave = false });

        var outcome = await CreateLogoutService().RequestLogoutAsync();

        outcome.Should().Be(LogoutOutcome.Cancelled);
        await _loginState.DidNotReceive().PerformLogoutAsync();
    }

    [Fact]
    public async Task ShellLogout_ActiveConsultation_CanLeave_PerformsLogout()
    {
        _activeConsultation.HasActiveConsultation.Returns(true);
        _activeConsultation.RequestLeaveAsync().Returns(new LeaveConsultationResult { CanLeave = true });

        var outcome = await CreateLogoutService().RequestLogoutAsync();

        outcome.Should().Be(LogoutOutcome.LoggedOut);
        await _loginState.Received(1).PerformLogoutAsync();
    }

    [Fact]
    public async Task ShellLogout_NoActiveConsultation_RequiresConfirmation()
    {
        _activeConsultation.HasActiveConsultation.Returns(false);
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns(false);

        var cancelled = await CreateLogoutService().RequestLogoutAsync();
        cancelled.Should().Be(LogoutOutcome.Cancelled);
        await _loginState.DidNotReceive().PerformLogoutAsync();

        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns(true);
        var confirmed = await CreateLogoutService().RequestLogoutAsync();
        confirmed.Should().Be(LogoutOutcome.LoggedOut);
        await _loginState.Received(1).PerformLogoutAsync();
    }

    [Fact]
    public async Task ShellLogout_Failure_ReturnsFailed_WithoutThrowing()
    {
        _activeConsultation.HasActiveConsultation.Returns(false);
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns(true);
        _loginState.PerformLogoutAsync().Returns(Task.FromException(new InvalidOperationException("boom")));

        var outcome = await CreateLogoutService().RequestLogoutAsync();

        outcome.Should().Be(LogoutOutcome.Failed);
    }

    [Fact]
    public async Task SideNav_LogoutCommand_UsesSharedGuardedFlow()
    {
        _logoutService.RequestLogoutAsync().Returns(LogoutOutcome.LoggedOut);
        using var sideNav = CreateSideNav();

        await sideNav.LogoutCommand.ExecuteAsync(null);

        await _logoutService.Received(1).RequestLogoutAsync();
        // 侧栏不得直调 PerformLogoutAsync（若直调则绕过活跃医案离开守卫——既有审查 #34）
        await _loginState.DidNotReceive().PerformLogoutAsync();
    }

    #endregion
}
