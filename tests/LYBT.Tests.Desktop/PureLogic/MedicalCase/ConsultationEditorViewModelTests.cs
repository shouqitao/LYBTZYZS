using System;
using System.Collections.Generic;
using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

public class ConsultationEditorViewModelTests : UserJourneyTestBase
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly IWorkspaceHost _host;
    private readonly ILoggerFactory _loggerFactory;

    public ConsultationEditorViewModelTests(UserJourneyFixture fixture) : base(fixture)
    {
        _context = Substitute.For<IMedicalCaseWorkspaceContext>();
        _context.MedicalCaseId.Returns(Guid.NewGuid());

        _host = Substitute.For<IWorkspaceHost>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
    }

    private ConsultationEditorViewModel CreateSut() => new(_context, _host, _loggerFactory);

    [Fact]
    public void Constructor_InitializesEmptyConsultation()
    {
        var sut = CreateSut();

        sut.Consultation.Should().NotBeNull();
        sut.Consultation.PresentIllness.Should().BeNull();
        sut.Consultation.TongueDiagnosis.Should().BeNull();
        sut.Consultation.PulseDiagnosis.Should().BeNull();
        sut.Consultation.TcmDiagnosis.Should().BeNull();
        sut.Consultation.IsDiagnosisComplete.Should().BeFalse();
    }

    [Fact]
    public void InitializeFromDto_MapsAllFields()
    {
        var sut = CreateSut();
        var dto = new ConsultationDetailDto
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PatientName = "张三",
            PresentIllness = "头痛三天",
            TongueDiagnosis = "舌红苔黄",
            PulseDiagnosis = "脉弦数",
            TcmDiagnosis = "肝阳上亢证",
        };

        sut.InitializeFromDto(dto);

        sut.Consultation.PresentIllness.Should().Be("头痛三天");
        sut.Consultation.TongueDiagnosis.Should().Be("舌红苔黄");
        sut.Consultation.PulseDiagnosis.Should().Be("脉弦数");
        sut.Consultation.TcmDiagnosis.Should().Be("肝阳上亢证");
        sut.Consultation.PatientName.Should().Be("张三");
        sut.Consultation.MedicalCaseId.Should().Be(dto.MedicalCaseId);
        sut.Consultation.IsDiagnosisComplete.Should().BeTrue();
    }

    [Fact]
    public void InitializeForNewCase_SetsPatientInfo_AndMedicalCaseId()
    {
        var sut = CreateSut();
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var medicalCaseId = _context.MedicalCaseId;

        sut.InitializeForNewCase("李四", patientId, userId);

        sut.Consultation.PatientName.Should().Be("李四");
        sut.Consultation.PatientId.Should().Be(patientId);
        sut.Consultation.UserId.Should().Be(userId);
        sut.Consultation.MedicalCaseId.Should().Be(medicalCaseId);
        sut.Consultation.IsDiagnosisComplete.Should().BeFalse();
    }

    [Fact]
    public void Reset_ClearsAllFields()
    {
        var sut = CreateSut();
        sut.Consultation.PresentIllness = "头痛";
        sut.Consultation.TongueDiagnosis = "舌红";
        sut.Consultation.PulseDiagnosis = "脉弦";
        sut.Consultation.TcmDiagnosis = "肝阳上亢";

        sut.Reset();

        sut.Consultation.PresentIllness.Should().BeNull();
        sut.Consultation.TongueDiagnosis.Should().BeNull();
        sut.Consultation.PulseDiagnosis.Should().BeNull();
        sut.Consultation.TcmDiagnosis.Should().BeNull();
        sut.Consultation.IsDiagnosisComplete.Should().BeFalse();
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenTcmDiagnosisIsEmpty()
    {
        var sut = CreateSut();
        sut.Consultation.PresentIllness = "头痛";
        sut.Consultation.TcmDiagnosis = string.Empty;

        sut.Validate().Should().BeFalse();
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenTcmDiagnosisIsProvided()
    {
        var sut = CreateSut();
        sut.Consultation.PresentIllness = "头痛";
        sut.Consultation.TcmDiagnosis = "肝阳上亢证";

        sut.Validate().Should().BeTrue();
        sut.Consultation.IsDiagnosisComplete.Should().BeTrue();
    }

    [Fact]
    public void GetConsultationData_MapsAllFields()
    {
        var sut = CreateSut();
        sut.Consultation.PresentIllness = "头痛";
        sut.Consultation.TongueDiagnosis = "舌红苔黄";
        sut.Consultation.PulseDiagnosis = "脉弦数";
        sut.Consultation.TcmDiagnosis = "肝阳上亢证";
        sut.Consultation.PatientName = "张三";

        var result = sut.GetConsultationData();

        result.Should().NotBeNull();
        result!.PresentIllness.Should().Be("头痛");
        result.TongueDiagnosis.Should().Be("舌红苔黄");
        result.PulseDiagnosis.Should().Be("脉弦数");
        result.TcmDiagnosis.Should().Be("肝阳上亢证");
    }

    [Fact]
    public void Consultation_SetProperty_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var notifications = new List<string?>();
        sut.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        var newItem = new ConsultationItem { PresentIllness = "新病史" };
        sut.Consultation = newItem;

        notifications.Should().Contain(nameof(ConsultationEditorViewModel.Consultation));
        sut.Consultation.Should().Be(newItem);
    }
}
