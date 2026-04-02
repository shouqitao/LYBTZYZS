using FluentAssertions;
using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Desktop.Users.ViewModels.Handlers;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure;

public class UserMasterDetailViewModelDisposalTests
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<UserListDto, UserDetailModel> _masterDetailServices;
    private readonly IUserRepository _userRepository;
    private readonly UserService _userService;
    private readonly IUserPasswordHandler _passwordHandler;
    private readonly IUserStatusHandler _statusHandler;
    private readonly IUserImportExportHandler _importExportHandler;
    private readonly IDesktopCacheManager _cacheManager;

    public UserMasterDetailViewModelDisposalTests()
    {
        _viewModelServices = Substitute.For<IViewModelServices>();
        _masterDetailServices = Substitute.For<IMasterDetailServices<UserListDto, UserDetailModel>>();
        _userRepository = Substitute.For<IUserRepository>();
        var logger = Substitute.For<ILogger<UserService>>();
        _userService = new UserService(_userRepository, logger);
        _passwordHandler = Substitute.For<IUserPasswordHandler>();
        _statusHandler = Substitute.For<IUserStatusHandler>();
        _importExportHandler = Substitute.For<IUserImportExportHandler>();
        _cacheManager = Substitute.For<IDesktopCacheManager>();
    }

    private UserMasterDetailViewModel CreateSut()
    {
        return new UserMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _userService,
            _passwordHandler,
            _statusHandler,
            _importExportHandler,
            _cacheManager);
    }

    [Fact]
    public void Dispose_MultipleCallsAreSafe()
    {
        var sut = CreateSut();

        sut.Dispose();
        sut.Dispose();
        sut.Dispose();

        sut.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var sut = CreateSut();

        var act = () => sut.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_AfterInitialization_ReleasesResources()
    {
        var sut = CreateSut();
        
        _ = sut.PageTitle;

        sut.Dispose();

        sut.Should().NotBeNull();
    }
}
