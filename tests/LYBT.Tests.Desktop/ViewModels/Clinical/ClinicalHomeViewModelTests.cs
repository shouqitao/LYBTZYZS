using LYBT.Desktop.Clinical.ViewModels;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Tests.Desktop.ViewModels.Clinical;

/// <summary>
/// ClinicalHomeViewModel 基础覆盖测试
/// A4-07: Desktop.Clinical 零覆盖补齐
/// </summary>
public class ClinicalHomeViewModelTests
{
    private readonly IViewModelServices _services;
    private readonly IAuthenticationService _authService;
    private readonly IDialogService _dialogService;
    private readonly INavigationCoordinator _navigationCoordinator;

    public ClinicalHomeViewModelTests()
    {
        // 构建 IViewModelServices mock
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _services = Substitute.For<IViewModelServices>();
        _services.LoggerFactory.Returns(loggerFactory);
        _services.EventAggregator.Returns(Substitute.For<IEventAggregator>());
        _services.RegionManager.Returns(Substitute.For<IRegionManager>());
        _services.SessionManager.Returns(Substitute.For<ISessionManager>());
        _services.UserNotificationService.Returns(Substitute.For<IUserNotificationService>());
        _services.CommonDialogService.Returns(Substitute.For<ICommonDialogService>());
        _services.RoleRegistry.Returns(Substitute.For<IRoleRegistry>());

        _authService = Substitute.For<IAuthenticationService>();
        _dialogService = Substitute.For<IDialogService>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();

        // 默认返回一个用户
        _authService.GetCurrentUserAsync().Returns(Task.FromResult<UserDetailDto?>(
            new UserDetailDto { UserName = "doctor1", RealName = "张医生" }));
    }

    private ClinicalHomeViewModel CreateViewModel()
    {
        return new ClinicalHomeViewModel(_services, _authService, _dialogService, _navigationCoordinator);
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.Should().NotBeNull();
        vm.Should().BeAssignableTo<ClinicalHomeViewModel>();
    }

    [Fact]
    public void Constructor_WithNullAuthService_ShouldThrow()
    {
        // Act & Assert
        var act = () => new ClinicalHomeViewModel(_services, null!, _dialogService, _navigationCoordinator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("authService");
    }

    [Fact]
    public void Constructor_WithNullDialogService_ShouldThrow()
    {
        // Act & Assert
        var act = () => new ClinicalHomeViewModel(_services, _authService, null!, _navigationCoordinator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dialogService");
    }

    [Fact]
    public void Constructor_WithNullNavigationCoordinator_ShouldThrow()
    {
        // Act & Assert
        var act = () => new ClinicalHomeViewModel(_services, _authService, _dialogService, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationCoordinator");
    }

    #endregion

    #region 初始状态测试

    [Fact]
    public void InitialState_StatisticsDefaultToZero()
    {
        // Arrange & Act
        var vm = CreateViewModel();

        // Assert
        vm.TodayConsultationCount.Should().Be(0);
        vm.PendingCaseCount.Should().Be(0);
    }

    #endregion

    #region INavigationAware 测试

    [Fact]
    public void IsNavigationTarget_ShouldReturnTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        var context = new NavigationContext(
            Substitute.For<IRegionNavigationService>(),
            new Uri("ClinicalHomeView", UriKind.Relative));

        // Act
        var result = vm.IsNavigationTarget(context);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
