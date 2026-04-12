using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Services;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Desktop.Users.ViewModels.Handlers;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Users;

/// <summary>
/// UserMasterDetailViewModel 简化单元测试
/// 验证用户管理模块的Master-Detail视图模型基本行为
/// </summary>
public class UserMasterDetailViewModelTests
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<UserListDto, UserDetailModel> _masterDetailServices;
    private readonly IUserService _commandHandler;
    private readonly IUserPasswordHandler _passwordHandler;
    private readonly IUserStatusHandler _statusHandler;
    private readonly IUserImportExportHandler _importExportHandler;
    private readonly IDesktopCacheManager _cacheManager;
    private readonly UserEditorViewModel _userEditor;
    private readonly ILoggerFactory _loggerFactory;

    // MasterDetailServices 组件
    private readonly IListViewServices<UserListDto> _listViewServices;
    private readonly IDetailEditorService<UserDetailModel> _detailEditor;
    private readonly IDialogManager _dialogManager;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ILoadingStateManager _loadingState;
    private readonly IPaginationService _pagination;
    private readonly ISearchService _search;
    private readonly ISelectionService<UserListDto> _selection;
    private readonly IErrorHandler _errorHandler;
    private readonly IAsyncExecutor _asyncExecutor;

    public UserMasterDetailViewModelTests()
    {
        // Arrange - 创建所有 mock
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        // 创建 MasterDetailServices 组件 mocks
        _listViewServices = Substitute.For<IListViewServices<UserListDto>>();
        _detailEditor = Substitute.For<IDetailEditorService<UserDetailModel>>();
        _dialogManager = Substitute.For<IDialogManager>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();
        _loadingState = Substitute.For<ILoadingStateManager>();
        _pagination = Substitute.For<IPaginationService>();
        _search = Substitute.For<ISearchService>();
        _selection = Substitute.For<ISelectionService<UserListDto>>();
        _errorHandler = Substitute.For<IErrorHandler>();
        _asyncExecutor = Substitute.For<IAsyncExecutor>();

        // 设置 ListViewServices 返回子服务
        _listViewServices.Loading.Returns(_loadingState);
        _listViewServices.Pagination.Returns(_pagination);
        _listViewServices.Search.Returns(_search);
        _listViewServices.Selection.Returns(_selection);
        _listViewServices.ErrorHandler.Returns(_errorHandler);
        _listViewServices.AsyncExecutor.Returns(_asyncExecutor);

        // 设置 ExecuteWithLoadingAsync 实际执行传入的函数
        _loadingState.ExecuteWithLoadingAsync(Arg.Any<Func<Task>>(), Arg.Any<string?>(), Arg.Any<bool>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>()());

        // 创建 MasterDetailServices mock
        _masterDetailServices = Substitute.For<IMasterDetailServices<UserListDto, UserDetailModel>>();
        _masterDetailServices.List.Returns(_listViewServices);
        _masterDetailServices.DetailEditor.Returns(_detailEditor);
        _masterDetailServices.Dialog.Returns(_dialogManager);
        _masterDetailServices.Navigation.Returns(_navigationCoordinator);
        _masterDetailServices.Loading.Returns(_loadingState);
        _masterDetailServices.Pagination.Returns(_pagination);
        _masterDetailServices.Search.Returns(_search);
        _masterDetailServices.Selection.Returns(_selection);
        _masterDetailServices.ErrorHandler.Returns(_errorHandler);
        _masterDetailServices.AsyncExecutor.Returns(_asyncExecutor);

        // 创建 ViewModelServices mock
        _viewModelServices = Substitute.For<IViewModelServices>();
        _viewModelServices.LoggerFactory.Returns(_loggerFactory);

        // 创建所有 Handler mocks
        _commandHandler = Substitute.For<IUserService>();
        _passwordHandler = Substitute.For<IUserPasswordHandler>();
        _statusHandler = Substitute.For<IUserStatusHandler>();
        _importExportHandler = Substitute.For<IUserImportExportHandler>();
        _cacheManager = Substitute.For<IDesktopCacheManager>();
        _userEditor = new UserEditorViewModel(_cacheManager);
    }

    private UserMasterDetailViewModel CreateSut()
    {
        return new UserMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _passwordHandler,
            _statusHandler,
            _importExportHandler,
            _cacheManager,
            _userEditor);
    }

    #region 构造函数和初始化

    [Fact]
    public void Constructor_InitializesPageTitle()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.PageTitle.Should().Be("用户管理");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCommandHandlerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new UserMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            null!,
            _passwordHandler,
            _statusHandler,
            _importExportHandler,
            _cacheManager,
            _userEditor);

        act.Should().Throw<ArgumentNullException>().WithParameterName("commandHandler");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPasswordHandlerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new UserMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            null!,
            _statusHandler,
            _importExportHandler,
            _cacheManager,
            _userEditor);

        act.Should().Throw<ArgumentNullException>().WithParameterName("passwordHandler");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenStatusHandlerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new UserMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _passwordHandler,
            null!,
            _importExportHandler,
            _cacheManager,
            _userEditor);

        act.Should().Throw<ArgumentNullException>().WithParameterName("statusHandler");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenImportExportHandlerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new UserMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _passwordHandler,
            _statusHandler,
            null!,
            _cacheManager,
            _userEditor);

        act.Should().Throw<ArgumentNullException>().WithParameterName("importExportHandler");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCacheManagerIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new UserMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _passwordHandler,
            _statusHandler,
            _importExportHandler,
            null!,
            _userEditor);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cacheManager");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenUserEditorIsNull()
    {
        // Arrange & Act & Assert
        Action act = () => new UserMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _commandHandler,
            _passwordHandler,
            _statusHandler,
            _importExportHandler,
            _cacheManager,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("userEditor");
    }

    [Fact]
    public void EntityDisplayName_ReturnsCorrectValue()
    {
        // Act
        var sut = CreateSut();

        // Assert - 通过 DetailTitle 间接验证 EntityDisplayName
        sut.DetailTitle.Should().Be("用户详情");
    }

    [Fact]
    public void RoleOptions_ContainsAllRoleValues()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.RoleOptions.Should().HaveCount(4);
        sut.RoleOptions.Should().Contain(UserRole.Receptionist);
        sut.RoleOptions.Should().Contain(UserRole.Doctor);
        sut.RoleOptions.Should().Contain(UserRole.Admin);
        sut.RoleOptions.Should().Contain(UserRole.SuperAdmin);
    }

    [Fact]
    public void StatusOptions_ContainsEnabledAndDisabled()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.StatusOptions.Should().Contain(CommonStatus.Enabled);
        sut.StatusOptions.Should().Contain(CommonStatus.Disabled);
    }

    [Fact]
    public void SelectedRoleFilter_DefaultValueIsNull()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.SelectedRoleFilter.Should().BeNull();
    }

    [Fact]
    public void SelectedStatusFilter_DefaultValueIsNull()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.SelectedStatusFilter.Should().BeNull();
    }

    [Fact]
    public void ShowInactiveUsers_DefaultValueIsFalse()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.ShowInactiveUsers.Should().BeFalse();
    }

    #endregion

    #region 属性变更测试

    [Fact]
    public void SelectedRoleFilter_SetValue_TriggersPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChangedCalled = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(UserMasterDetailViewModel.SelectedRoleFilter))
                propertyChangedCalled = true;
        };

        // Act
        sut.SelectedRoleFilter = UserRole.Doctor;

        // Assert
        propertyChangedCalled.Should().BeTrue();
        sut.SelectedRoleFilter.Should().Be(UserRole.Doctor);
    }

    [Fact]
    public void SelectedStatusFilter_SetValue_TriggersPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChangedCalled = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(UserMasterDetailViewModel.SelectedStatusFilter))
                propertyChangedCalled = true;
        };

        // Act
        sut.SelectedStatusFilter = CommonStatus.Disabled;

        // Assert
        propertyChangedCalled.Should().BeTrue();
        sut.SelectedStatusFilter.Should().Be(CommonStatus.Disabled);
    }

    [Fact]
    public void ShowInactiveUsers_SetValue_TriggersPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChangedCalled = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(UserMasterDetailViewModel.ShowInactiveUsers))
                propertyChangedCalled = true;
        };

        // Act
        sut.ShowInactiveUsers = true;

        // Assert
        propertyChangedCalled.Should().BeTrue();
        sut.ShowInactiveUsers.Should().BeTrue();
    }

    #endregion

    #region 辅助方法

    private static UserListDto CreateUserListDto(Guid? id = null, string userName = "testuser")
    {
        return new UserListDto
        {
            Id = id ?? Guid.NewGuid(),
            UserName = userName,
            RealName = "测试用户",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
        };
    }

    private static UserDetailDto CreateUserDetailDto(Guid? id = null, string userName = "testuser")
    {
        return new UserDetailDto
        {
            Id = id ?? Guid.NewGuid(),
            UserName = userName,
            RealName = "测试用户",
            PinYinCode = "CSYH",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
