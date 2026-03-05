using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase.Workspace;

public class ConsultationEditorPureTests
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly IWorkspaceHost _host;
    private readonly ILoggerFactory _loggerFactory;

    public ConsultationEditorPureTests()
    {
        _context = Substitute.For<IMedicalCaseWorkspaceContext>();
        _context.MedicalCaseId.Returns(Guid.NewGuid());
        _host = Substitute.For<IWorkspaceHost>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
    }

    private ConsultationEditorViewModel CreateSut() => new(_context, _host, _loggerFactory);

    [Fact]
    public void InitializeForNewCase_sets_patient_fields_and_resets_diagnosis()
    {
        var sut = CreateSut();
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        sut.InitializeForNewCase("Zhang San", patientId, userId);

        sut.Consultation.PatientName.Should().Be("Zhang San");
        sut.Consultation.PatientId.Should().Be(patientId);
        sut.Consultation.UserId.Should().Be(userId);
        sut.Consultation.MedicalCaseId.Should().Be(_context.MedicalCaseId);
        sut.Consultation.PresentIllness.Should().BeNull();
        sut.Consultation.TcmDiagnosis.Should().BeNull();
    }

    [Fact]
    public void InitializeFromDto_maps_all_fields()
    {
        var sut = CreateSut();
        var dto = new ConsultationDetailDto
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            PresentIllness = "headache",
            TongueDiagnosis = "red tongue",
            PulseDiagnosis = "rapid pulse",
            TcmDiagnosis = "wind-heat"
        };

        sut.InitializeFromDto(dto);

        sut.Consultation.Id.Should().Be(dto.Id);
        sut.Consultation.PresentIllness.Should().Be("headache");
        sut.Consultation.TongueDiagnosis.Should().Be("red tongue");
        sut.Consultation.PulseDiagnosis.Should().Be("rapid pulse");
        sut.Consultation.TcmDiagnosis.Should().Be("wind-heat");
    }

    [Fact]
    public void Validate_fails_when_TcmDiagnosis_empty()
    {
        var sut = CreateSut();
        sut.InitializeForNewCase("Test", Guid.NewGuid(), Guid.NewGuid());

        sut.Validate().Should().BeFalse();
        sut.ValidationMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_succeeds_when_TcmDiagnosis_filled()
    {
        var sut = CreateSut();
        sut.Consultation.TcmDiagnosis = "wind-heat";

        sut.Validate().Should().BeTrue();
        sut.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void Reset_clears_diagnosis_fields()
    {
        var sut = CreateSut();
        sut.Consultation.PresentIllness = "headache";
        sut.Consultation.TongueDiagnosis = "red";
        sut.Consultation.PulseDiagnosis = "rapid";
        sut.Consultation.TcmDiagnosis = "wind-heat";

        sut.Reset();

        sut.Consultation.PresentIllness.Should().BeNull();
        sut.Consultation.TongueDiagnosis.Should().BeNull();
        sut.Consultation.PulseDiagnosis.Should().BeNull();
        sut.Consultation.TcmDiagnosis.Should().BeNull();
    }

    [Fact]
    public void GetConsultationData_returns_dto_from_mapper()
    {
        var sut = CreateSut();
        sut.Consultation.TcmDiagnosis = "wind-heat";
        sut.Consultation.PresentIllness = "headache";

        var data = sut.GetConsultationData();

        data.Should().NotBeNull();
        data!.TcmDiagnosis.Should().Be("wind-heat");
        data.PresentIllness.Should().Be("headache");
    }
}
