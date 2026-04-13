using FluentAssertions;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Clinical.ViewModels;
using LYBT.Desktop.Clinical.ViewModels.Workspace;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LYBT.Tests.Desktop.PureLogic.Clinical;

/// <summary>
/// PatientSelectionViewModel 单元测试
/// 验证两页分离重构后患者选择页承载 CardReader/PendingQueue 子VM的预期行为
/// TDD RED 阶段 - 这些测试在实现前必须失败
/// </summary>
public class PatientSelectionViewModelTests
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IPatientApi _patientApi;
    private readonly IMedicalCaseApi _medicalCaseApi;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEventAggregator _eventAggregator;
    private readonly IRegionManager _regionManager;
    private readonly ISessionManager _sessionManager;
    private readonly ICommonDialogService _commonDialogService;
    private readonly ICardReaderService _cardReaderService;
    private readonly IPatientCardReaderIntegration _patientIntegration;
    private readonly IPendingQueueManager _pendingQueueManager;

    public PatientSelectionViewModelTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        _eventAggregator = Substitute.For<IEventAggregator>();
        _regionManager = Substitute.For<IRegionManager>();
        _sessionManager = Substitute.For<ISessionManager>();
        _commonDialogService = Substitute.For<ICommonDialogService>();

        _viewModelServices = Substitute.For<IViewModelServices>();
        _viewModelServices.LoggerFactory.Returns(_loggerFactory);
        _viewModelServices.EventAggregator.Returns(_eventAggregator);
        _viewModelServices.RegionManager.Returns(_regionManager);
        _viewModelServices.SessionManager.Returns(_sessionManager);
        _viewModelServices.CommonDialogService.Returns(_commonDialogService);

        _patientApi = Substitute.For<IPatientApi>();
        _medicalCaseApi = Substitute.For<IMedicalCaseApi>();
        _medicalCaseService = Substitute.For<IMedicalCaseService>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();
        _cardReaderService = Substitute.For<ICardReaderService>();
        _patientIntegration = Substitute.For<IPatientCardReaderIntegration>();
        _pendingQueueManager = Substitute.For<IPendingQueueManager>();
        _pendingQueueManager.PendingQueue.Returns(new ObservableCollection<PendingMedicalCaseDto>());
    }

    private PatientSelectionViewModel CreateSut() => new(
        _viewModelServices,
        _patientApi,
        _medicalCaseApi,
        _medicalCaseService,
        _navigationCoordinator,
        _cardReaderService,
        _patientIntegration,
        _pendingQueueManager);

    private static object? GetProperty(object obj, string name)
        => obj.GetType().GetProperty(name)?.GetValue(obj);

    [Fact]
    public void Constructor_CreatesCardReaderChildVm_NotNull()
    {
        var sut = CreateSut();

        var cardReader = GetProperty(sut, "CardReader");
        cardReader.Should().NotBeNull("CardReader child VM must be initialized in constructor");
    }

    [Fact]
    public void Constructor_CreatesPendingQueueChildVm_NotNull()
    {
        var sut = CreateSut();

        var pendingQueue = GetProperty(sut, "PendingQueue");
        pendingQueue.Should().NotBeNull("PendingQueue child VM must be initialized in constructor");
    }

    [Fact]
    public void CardReader_IsCardReaderViewModel_CorrectType()
    {
        var sut = CreateSut();

        var cardReader = GetProperty(sut, "CardReader");
        cardReader.Should().BeOfType<CardReaderViewModel>();
    }

    [Fact]
    public void PendingQueue_IsPendingQueueViewModel_CorrectType()
    {
        var sut = CreateSut();

        var pendingQueue = GetProperty(sut, "PendingQueue");
        pendingQueue.Should().BeOfType<PendingQueueViewModel>();
    }

    [Fact]
    public void PatientSelectionViewModel_ImplementsIWorkspaceHost()
    {
        var sut = CreateSut();

        sut.Should().BeAssignableTo<IWorkspaceHost>(
            "PatientSelectionViewModel must implement IWorkspaceHost to act as host for child VMs");
    }

    [Fact]
    public void IWorkspaceHost_SetBusy_DoesNotThrow()
    {
        var sut = CreateSut();
        var host = sut as IWorkspaceHost;
        host.Should().NotBeNull("PatientSelectionViewModel must implement IWorkspaceHost");

        var act = () => host!.SetBusy(true, "加载中...");
        act.Should().NotThrow();
    }

    [Fact]
    public async Task IWorkspaceHost_ShowErrorAsync_DoesNotThrow()
    {
        var sut = CreateSut();
        var host = sut as IWorkspaceHost;
        host.Should().NotBeNull("PatientSelectionViewModel must implement IWorkspaceHost");

        var act = async () => await host!.ShowErrorAsync("测试错误消息");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task IWorkspaceHost_ShowConfirmAsync_ReturnsBoolean()
    {
        _commonDialogService.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        var sut = CreateSut();
        var host = sut as IWorkspaceHost;
        host.Should().NotBeNull("PatientSelectionViewModel must implement IWorkspaceHost");

        var result = await host!.ShowConfirmAsync("确认操作？", "提示");

        result.Should().BeTrue();
    }

    [Fact]
    public void PendingQueue_HasNoPendingCases_TrueWhenEmpty()
    {
        var sut = CreateSut();

        var pendingQueue = GetProperty(sut, "PendingQueue") as PendingQueueViewModel;
        pendingQueue.Should().NotBeNull("PendingQueue property must exist");
        pendingQueue!.HasNoPendingCases.Should().BeTrue("pending queue is empty on construction");
    }

    [Fact]
    public void ExistingPatients_CollectionIsEmpty_OnConstruction()
    {
        var sut = CreateSut();

        sut.Patients.Should().BeEmpty("no patients loaded before OnNavigatedTo");
    }

    [Fact]
    public void ExistingHasSelection_IsFalse_WhenNoPatientSelected()
    {
        var sut = CreateSut();

        sut.HasSelection.Should().BeFalse();
    }
}
