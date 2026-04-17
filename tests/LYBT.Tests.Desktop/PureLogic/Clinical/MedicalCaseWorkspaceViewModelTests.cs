using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Clinical.ViewModels;
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
using LYBT.Desktop.Infrastructure.Services.Toast;

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
    private readonly IToastService _toastService;
    private readonly PrescriptionPrintHandler _printHandler;
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
        _toastService = Substitute.For<IToastService>();
        
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
        
        _dialogService = Substitute.For<IDialogService>();
    }

    private MedicalCaseWorkspaceViewModel CreateSut()
    {
        return new MedicalCaseWorkspaceViewModel(
            _viewModelServices,
            _medicalCaseService,
            _navigationCoordinator,
            _activeConsultationService,
            _toastService,
            _printHandler,
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

    #region Phase 1.2: CurrentStep Auto-Advance Logic

    [Fact]
    public void CurrentStep_Initially_IsStep1()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        sut.CurrentStep.Should().Be(1);
    }

    [Fact]
    public void CurrentStep_AdvancesToStep2_WhenPresentIllnessHasContent()
    {
        // Arrange
        var sut = CreateSut();
        sut.ConsultationEditor.Consultation.PresentIllness = "患者主诉内容";

        // Act
        sut.ConsultationEditor.Consultation.PropertyChanged +=
            (s, e) => { if (e.PropertyName == "PresentIllness") sut.UpdateState(); };

        // Manually trigger the update
        sut.UpdateState();

        // Assert
        sut.CurrentStep.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void CurrentStep_AdvancesToStep3_WhenDiagnosisComplete()
    {
        // Arrange
        var sut = CreateSut();
        sut.ConsultationEditor.Consultation.PresentIllness = "患者主诉内容";
        sut.ConsultationEditor.Consultation.TcmDiagnosis = "脾胃虚弱证";

        // Act
        sut.UpdateState();

        // Assert
        sut.CurrentStep.Should().BeGreaterThanOrEqualTo(3);
    }

    #endregion

    #region Phase 1.4: CompletenessCheck

    [Fact]
    public void Completeness_Initially_IsNotComplete()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        sut.Completeness.Should().NotBeNull();
        sut.Completeness.DiagnosisComplete.Should().BeFalse();
        sut.Completeness.CanCompleteCase.Should().BeFalse();
    }

    [Fact]
    public void Completeness_DiagnosisComplete_UpdatesWhenDiagnosisFilled()
    {
        // Arrange
        var sut = CreateSut();
        sut.ConsultationEditor.Consultation.TcmDiagnosis = "脾胃虚弱证";

        // Act
        sut.UpdateState();

        // Assert
        sut.Completeness.DiagnosisComplete.Should().BeTrue();
    }

    [Fact]
    public void Completeness_PrescriptionContentComplete_WhenPrescriptionHasItems()
    {
        // Arrange
        var sut = CreateSut();
        sut.IsPrescriptionEnabled = true;
        sut.ConsultationEditor.Consultation.TcmDiagnosis = "脾胃虚弱证";

        // Add prescription items
        var prescriptionItem = new PrescriptionItemDto
        {
            HerbId = Guid.NewGuid(),
            HerbName = "测试药材",
            Dosage = 10m,
            Unit = "g"
        };
        sut.PrescriptionEditor.Prescription.Items.Add(prescriptionItem);

        // Act
        sut.UpdateState();

        // Assert
        sut.Completeness.PrescriptionContentComplete.Should().BeTrue();
        sut.Completeness.PrescriptionItemCount.Should().Be(1);
    }

    [Fact]
    public void Completeness_CanCompleteCase_WhenAllCriteriaMet()
    {
        // Arrange
        var sut = CreateSut();
        sut.ConsultationEditor.Consultation.TcmDiagnosis = "脾胃虚弱证";
        sut.IsPrescriptionEnabled = true;

        var prescriptionItem = new PrescriptionItemDto
        {
            HerbId = Guid.NewGuid(),
            HerbName = "测试药材",
            Dosage = 10m,
            Unit = "g"
        };
        sut.PrescriptionEditor.Prescription.Items.Add(prescriptionItem);
        sut.PrescriptionEditor.Prescription.DosageCount = 7;

        // Act
        sut.UpdateState();

        // Assert
        sut.Completeness.CanCompleteCase.Should().BeTrue();
        sut.State.CanComplete.Should().BeTrue();
    }

    [Fact]
    public void Completeness_CanCompleteCase_WithoutPrescription_WhenDiagnosisComplete()
    {
        // Arrange
        var sut = CreateSut();
        sut.ConsultationEditor.Consultation.TcmDiagnosis = "脾胃虚弱证";
        sut.IsPrescriptionEnabled = false;

        // Act
        sut.UpdateState();

        // Assert
        sut.Completeness.CanCompleteCase.Should().BeTrue();
    }

    #endregion

    #region Phase 1.3: ConsultationItem Properties

    [Fact]
    public void ConsultationItem_IsPresentIllnessValid_ReturnsFalse_WhenEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.ConsultationEditor.Consultation.IsPresentIllnessValid.Should().BeFalse();
    }

    [Fact]
    public void ConsultationItem_IsPresentIllnessValid_ReturnsTrue_When5OrMoreChars()
    {
        // Arrange
        var sut = CreateSut();
        sut.ConsultationEditor.Consultation.PresentIllness = "测试内容超过五个字";

        // Act & Assert
        sut.ConsultationEditor.Consultation.IsPresentIllnessValid.Should().BeTrue();
    }

    [Fact]
    public void ConsultationItem_IsDiagnosisComplete_ReturnsTrue_WhenHasDiagnosis()
    {
        // Arrange
        var sut = CreateSut();
        sut.ConsultationEditor.Consultation.TcmDiagnosis = "脾胃虚弱证";

        // Act & Assert
        sut.ConsultationEditor.Consultation.IsDiagnosisComplete.Should().BeTrue();
    }

    #endregion

    #region Phase 1.3: PrescriptionItem Properties

    [Fact]
    public void PrescriptionItem_HasItems_ReturnsTrue_WhenHasItems()
    {
        // Arrange
        var sut = CreateSut();
        var prescriptionItem = new PrescriptionItemDto
        {
            HerbId = Guid.NewGuid(),
            HerbName = "测试药材",
            Dosage = 10m,
            Unit = "g"
        };
        sut.PrescriptionEditor.Prescription.Items.Add(prescriptionItem);

        // Act & Assert
        sut.PrescriptionEditor.Prescription.HasItems.Should().BeTrue();
        sut.PrescriptionEditor.Prescription.ItemCount.Should().Be(1);
    }

    #endregion
}
