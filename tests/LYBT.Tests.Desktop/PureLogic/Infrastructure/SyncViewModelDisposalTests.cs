using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Sync.Services;
using LYBT.Desktop.Sync.ViewModels;
using NSubstitute;
using Prism.Services.Dialogs;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure;

public class SyncViewModelDisposalTests
{
    private readonly IViewModelServices _viewModelServices;
    private readonly ISyncService _syncService;
    private readonly IDialogService _dialogService;
    private readonly IApiHealthCheckService _healthCheckService;
    private readonly SyncItemViewModelFactory _itemFactory;

    public SyncViewModelDisposalTests()
    {
        _viewModelServices = Substitute.For<IViewModelServices>();
        _syncService = Substitute.For<ISyncService>();
        _dialogService = Substitute.For<IDialogService>();
        _healthCheckService = Substitute.For<IApiHealthCheckService>();
        _itemFactory = Substitute.For<SyncItemViewModelFactory>();
    }

    private SyncViewModel CreateSut()
    {
        return new SyncViewModel(
            _viewModelServices,
            _syncService,
            _dialogService,
            _healthCheckService,
            _itemFactory);
    }

    [Fact]
    public void Dispose_MultipleCallsAreSafe()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Dispose();
        sut.Dispose();
        sut.Dispose();

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_AfterInitialization_ReleasesResources()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        _ = sut.PageTitle;
        sut.Dispose();

        // Assert
        _itemFactory.Received(1).SetSelectionChangedCallback(null!);
        sut.Should().NotBeNull();
    }
}
