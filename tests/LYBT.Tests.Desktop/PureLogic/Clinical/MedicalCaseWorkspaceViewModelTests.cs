using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Clinical.ViewModels;
using LYBT.Desktop.Clinical.ViewModels.Workspace;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using LYBT.Desktop.Infrastructure.Events;

namespace LYBT.Tests.Desktop.PureLogic.Clinical;

/// <summary>
/// MedicalCaseWorkspaceViewModel 单元测试
/// 验证医案工作区 Composite ViewModel 的初始化和基本行为
/// </summary>
public class MedicalCaseWorkspaceViewModelTests
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IPendingQueueManager _pendingQueueManager;
    private readonly PrescriptionPrintHandler _printHandler;
    private readonly ICardReaderService _cardReaderService;
    private readonly IPatientCardReaderIntegration _patientCardReaderIntegration;
    private readonly IDialogService? _dialogService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEventAggregator _eventAggregator;
    private readonly IRegionManager _regionManager;
    private readonly ISessionManager _sessionManager;
    private readonly ICommonDialogService _commonDialogService;
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IClinicSettingsService _clinicSettingsService;
    private readonly IPrintService<PrescriptionPrintModel> _printService;

    public MedicalCaseWorkspaceViewModelTests()
    {
        // Arrange - 创建所有 mock
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        _eventAggregator = Substitute.For<IEventAggregator>();
        // Mock GetEvent to return PubSubEvent mocks that don't require SynchronizationContext
        var consultationEvent = Substitute.For<CaseEvents.ConsultationCompletedEvent>();
        consultationEvent.Subscribe(Arg.Any<Action<CaseConsultationCompletedPayload>>(), Arg.Any<ThreadOption>())
            .Returns(new SubscriptionToken(_ => { }));
        var prescriptionEvent = Substitute.For<CaseEvents.PrescriptionCompletedEvent>();
        prescriptionEvent.Subscribe(Arg.Any<Action<CasePrescriptionCompletedPayload>>(), Arg.Any<ThreadOption>())
            .Returns(new SubscriptionToken(_ => { }));
        _eventAggregator.GetEvent<CaseEvents.ConsultationCompletedEvent>().Returns(consultationEvent);
        _eventAggregator.GetEvent<CaseEvents.PrescriptionCompletedEvent>().Returns(prescriptionEvent);
        _regionManager = Substitute.For<IRegionManager>();
        _sessionManager = Substitute.For<ISessionManager>();
        _commonDialogService = Substitute.For<ICommonDialogService>();

        _viewModelServices = Substitute.For<IViewModelServices>();
        _viewModelServices.LoggerFactory.Returns(_loggerFactory);
        _viewModelServices.EventAggregator.Returns(_eventAggregator);
        _viewModelServices.RegionManager.Returns(_regionManager);
        _viewModelServices.SessionManager.Returns(_sessionManager);
        _viewModelServices.CommonDialogService.Returns(_commonDialogService);

        _medicalCaseService = Substitute.For<IMedicalCaseService>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();
        _activeConsultationService = Substitute.For<IActiveConsultationService>();
        _pendingQueueManager = Substitute.For<IPendingQueueManager>();
        
        // Mock dependencies for PrescriptionPrintHandler
        _medicalCaseRepository = Substitute.For<IMedicalCaseRepository>();
        _clinicSettingsService = Substitute.For<IClinicSettingsService>();
        _printService = Substitute.For<IPrintService<PrescriptionPrintModel>>();
        
        // Create PrescriptionPrintHandler with mocked dependencies
        _printHandler = new PrescriptionPrintHandler(
            _medicalCaseService,
            _medicalCaseRepository,
            _sessionManager,
            _clinicSettingsService,
            _loggerFactory,
            _printService);
        
        _cardReaderService = Substitute.For<ICardReaderService>();
        _patientCardReaderIntegration = Substitute.For<IPatientCardReaderIntegration>();
        _dialogService = Substitute.For<IDialogService>();
    }

    private MedicalCaseWorkspaceViewModel CreateSut()
    {
        return new MedicalCaseWorkspaceViewModel(
            _viewModelServices,
            _medicalCaseService,
            _navigationCoordinator,
            _activeConsultationService,
            _pendingQueueManager,
            _printHandler,
            _cardReaderService,
            _patientCardReaderIntegration,
            _dialogService);
    }

    #region 构造函数和初始化

    [Fact]
    public void Constructor_InitializesDefaultState()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.State.Should().NotBeNull();
        sut.MedicalCaseId.Should().Be(Guid.Empty);
        sut.CurrentPatient.Should().BeNull();
        sut.Remark.Should().BeEmpty();
        sut.EditReason.Should().BeEmpty();
        sut.IsPrescriptionEnabled.Should().BeFalse();
        sut.NeedsPrescription.Should().BeTrue();
        sut.AllHerbs.Should().NotBeNull();
        sut.AllHerbs.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_InitializesChildViewModels()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.ConsultationEditor.Should().NotBeNull();
        sut.PrescriptionEditor.Should().NotBeNull();
        sut.Commands.Should().NotBeNull();
        sut.PendingQueue.Should().NotBeNull();
        sut.CardReader.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.BackCommand.Should().NotBeNull();
        sut.ViewPatientHistoryCommand.Should().NotBeNull();
        sut.SaveChangesCommand.Should().NotBeNull();
    }

    #endregion

    #region 属性变更

    [Fact]
    public void MedicalCaseId_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChangedRaised = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MedicalCaseWorkspaceViewModel.MedicalCaseId))
                propertyChangedRaised = true;
        };

        // Act
        sut.MedicalCaseId = Guid.NewGuid();

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void CurrentPatient_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChangedRaised = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MedicalCaseWorkspaceViewModel.CurrentPatient))
                propertyChangedRaised = true;
        };

        // Act
        sut.CurrentPatient = new PatientDetailDto { Id = Guid.NewGuid(), Name = "Test Patient" };

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void Remark_SetValue_UpdatesCachedMedicalCase()
    {
        // Arrange
        var sut = CreateSut();
        var cachedCase = new MedicalCaseDetailDto { Id = Guid.NewGuid() };
        _medicalCaseService.CachedMedicalCase.Returns(cachedCase);

        // Act
        sut.Remark = "Test remark";

        // Assert
        cachedCase.Remark.Should().Be("Test remark");
    }

    [Fact]
    public void IsPrescriptionEnabled_SetTrue_UpdatesValidationEnabled()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.IsPrescriptionEnabled = true;

        // Assert
        sut.PrescriptionEditor.Prescription.ValidationEnabled.Should().BeTrue();
    }

    #endregion

    #region 导航

    [Fact]
    public void IsNavigationTarget_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.IsNavigationTarget(null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
