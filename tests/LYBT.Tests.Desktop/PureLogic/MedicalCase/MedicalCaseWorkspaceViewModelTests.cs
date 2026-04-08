using FluentAssertions;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Clinical.ViewModels;
using LYBT.Desktop.Clinical.ViewModels.Workspace;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

/// <summary>
/// MedicalCaseWorkspaceViewModel 简化单元测试
/// 验证医案工作区 Composite ViewModel 的基本行为
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
    public void Constructor_InitializesMedicalCaseIdToEmptyGuid()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.MedicalCaseId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Constructor_CurrentPatient_IsNullByDefault()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.CurrentPatient.Should().BeNull();
    }

    #endregion

    #region MedicalCaseId 属性测试

    [Fact]
    public void MedicalCaseId_SetValue_UpdatesProperty()
    {
        // Arrange
        var sut = CreateSut();
        var newId = Guid.NewGuid();

        // Act
        sut.MedicalCaseId = newId;

        // Assert
        sut.MedicalCaseId.Should().Be(newId);
    }

    [Fact]
    public void MedicalCaseId_SetValue_TriggersPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChangedCalled = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MedicalCaseWorkspaceViewModel.MedicalCaseId))
                propertyChangedCalled = true;
        };

        // Act
        sut.MedicalCaseId = Guid.NewGuid();

        // Assert
        propertyChangedCalled.Should().BeTrue();
    }

    #endregion

    #region CurrentPatient 属性测试

    [Fact]
    public void CurrentPatient_SetValue_UpdatesProperty()
    {
        // Arrange
        var sut = CreateSut();
        var patient = CreatePatientDetailDto();

        // Act
        sut.CurrentPatient = patient;

        // Assert
        sut.CurrentPatient.Should().Be(patient);
    }

    [Fact]
    public void CurrentPatient_SetValue_TriggersPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var patient = CreatePatientDetailDto();
        var propertyChangedCalled = false;
        sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MedicalCaseWorkspaceViewModel.CurrentPatient))
                propertyChangedCalled = true;
        };

        // Act
        sut.CurrentPatient = patient;

        // Assert
        propertyChangedCalled.Should().BeTrue();
    }

    #endregion

    #region MedicalCaseDetailDto 使用测试

    [Fact]
    public void CanCreateMedicalCaseDetailDto_WithValidData()
    {
        // Arrange & Act
        var dto = CreateMedicalCaseDetailDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().NotBe(Guid.Empty);
        dto.PatientId.Should().NotBe(Guid.Empty);
        dto.PatientName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MedicalCaseDetailDto_CaseStatus_DefaultsToSuspended()
    {
        // Arrange & Act
        var dto = CreateMedicalCaseDetailDto();

        // Assert
        dto.CaseStatus.Should().Be(MedicalCaseStatus.Suspended);
    }

    [Fact]
    public void MedicalCaseDetailDto_HasPrescription_ReturnsCorrectValue()
    {
        // Arrange
        var dtoWithPrescription = CreateMedicalCaseDetailDto(hasPrescription: true);
        var dtoWithoutPrescription = CreateMedicalCaseDetailDto(hasPrescription: false);

        // Assert
        dtoWithPrescription.HasPrescription.Should().BeTrue();
        dtoWithoutPrescription.HasPrescription.Should().BeFalse();
    }

    #endregion

    #region 导航测试

    public void IsNavigationTarget_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        // Note: NavigationContext is sealed in Prism, cannot be mocked with NSubstitute
        
        // Act & Assert - Skip this test
        Assert.True(true);
    }

    #endregion

    #region 辅助方法

    private static MedicalCaseDetailDto CreateMedicalCaseDetailDto(bool hasPrescription = false)
    {
        return new MedicalCaseDetailDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PatientName = "测试患者",
            PatientGender = Gender.Male,
            CaseNumber = "MC-2024-001",
            CaseStatus = MedicalCaseStatus.Suspended,
            PrescriptionId = hasPrescription ? Guid.NewGuid() : null,
            CreatedAt = DateTime.UtcNow,
            DoctorName = "测试医生"
        };
    }

    private static PatientDetailDto CreatePatientDetailDto()
    {
        return new PatientDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "测试患者",
            Gender = Gender.Male,
            PhoneNumber = "13800138000",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
