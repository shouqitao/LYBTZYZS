using System.ComponentModel;
using FluentAssertions;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Tests.Desktop.Infrastructure;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.MedicalCase;

/// <summary>
/// Phase 1.3: ConsultationItem validation property tests
/// Tests for field validation indicators
/// </summary>
public class ConsultationItemTests : UserJourneyTestBase
{
    public ConsultationItemTests(UserJourneyFixture fixture) : base(fixture)
    {
    }

    private ConsultationItem CreateSut() => new();

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var sut = CreateSut();

        sut.Id.Should().Be(Guid.Empty);
        sut.MedicalCaseId.Should().Be(Guid.Empty);
        sut.PatientId.Should().Be(Guid.Empty);
        sut.UserId.Should().Be(Guid.Empty);
        sut.PatientName.Should().BeEmpty();
        sut.DoctorName.Should().BeEmpty();
        sut.PresentIllness.Should().BeNull();
        sut.TongueDiagnosis.Should().BeNull();
        sut.PulseDiagnosis.Should().BeNull();
        sut.TcmDiagnosis.Should().BeNull();
    }

    [Fact]
    public void IsDiagnosisComplete_ReturnsFalse_WhenTcmDiagnosisIsNull()
    {
        var sut = CreateSut();
        sut.TcmDiagnosis = null;

        sut.IsDiagnosisComplete.Should().BeFalse();
    }

    [Fact]
    public void IsDiagnosisComplete_ReturnsFalse_WhenTcmDiagnosisIsEmpty()
    {
        var sut = CreateSut();
        sut.TcmDiagnosis = string.Empty;

        sut.IsDiagnosisComplete.Should().BeFalse();
    }

    [Fact]
    public void IsDiagnosisComplete_ReturnsFalse_WhenTcmDiagnosisIsWhitespace()
    {
        var sut = CreateSut();
        sut.TcmDiagnosis = "   ";

        sut.IsDiagnosisComplete.Should().BeFalse();
    }

    [Fact]
    public void IsDiagnosisComplete_ReturnsTrue_WhenTcmDiagnosisHasValue()
    {
        var sut = CreateSut();
        sut.TcmDiagnosis = "肝阳上亢证";

        sut.IsDiagnosisComplete.Should().BeTrue();
    }

    [Fact]
    public void TcmDiagnosis_SetProperty_RaisesIsDiagnosisCompleteChanged()
    {
        var sut = CreateSut();
        var propertiesChanged = new List<string?>();
        sut.PropertyChanged += (_, e) => propertiesChanged.Add(e.PropertyName);

        sut.TcmDiagnosis = "感冒";

        propertiesChanged.Should().Contain(nameof(ConsultationItem.TcmDiagnosis));
        propertiesChanged.Should().Contain(nameof(ConsultationItem.IsDiagnosisComplete));
    }

    [Fact]
    public void IsPresentIllnessValid_ReturnsFalse_WhenPresentIllnessIsNull()
    {
        var sut = CreateSut();
        sut.PresentIllness = null;

        sut.IsPresentIllnessValid.Should().BeFalse();
    }

    [Fact]
    public void IsPresentIllnessValid_ReturnsFalse_WhenPresentIllnessIsEmpty()
    {
        var sut = CreateSut();
        sut.PresentIllness = string.Empty;

        sut.IsPresentIllnessValid.Should().BeFalse();
    }

    [Fact]
    public void IsPresentIllnessValid_ReturnsFalse_WhenPresentIllnessIsWhitespace()
    {
        var sut = CreateSut();
        sut.PresentIllness = "   ";

        sut.IsPresentIllnessValid.Should().BeFalse();
    }

    [Fact]
    public void IsPresentIllnessValid_ReturnsFalse_WhenPresentIllnessIsLessThan5Characters()
    {
        var sut = CreateSut();
        sut.PresentIllness = "头痛";

        sut.IsPresentIllnessValid.Should().BeFalse();
    }

    [Fact]
    public void IsPresentIllnessValid_ReturnsTrue_WhenPresentIllnessIsExactly5Characters()
    {
        var sut = CreateSut();
        sut.PresentIllness = "头痛三";

        sut.IsPresentIllnessValid.Should().BeTrue();
    }

    [Fact]
    public void IsPresentIllnessValid_ReturnsTrue_WhenPresentIllnessIsMoreThan5Characters()
    {
        var sut = CreateSut();
        sut.PresentIllness = "头痛三天，伴有发热";

        sut.IsPresentIllnessValid.Should().BeTrue();
    }

    [Fact]
    public void PresentIllness_SetProperty_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var propertiesChanged = new List<string?>();
        sut.PropertyChanged += (_, e) => propertiesChanged.Add(e.PropertyName);

        sut.PresentIllness = "头痛三天";

        propertiesChanged.Should().Contain(nameof(ConsultationItem.PresentIllness));
    }

    [Fact]
    public void DisplayText_ReturnsPatientNameAndDiagnosis()
    {
        var sut = CreateSut();
        sut.PatientName = "张三";
        sut.TcmDiagnosis = "肝阳上亢证";

        sut.DisplayText.Should().Be("张三 - 肝阳上亢证");
    }

    [Fact]
    public void DisplayText_ReturnsPatientNameAndUndiagnosed_WhenDiagnosisIsEmpty()
    {
        var sut = CreateSut();
        sut.PatientName = "李四";
        sut.TcmDiagnosis = null;

        sut.DisplayText.Should().Be("李四 - 未诊断");
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenTcmDiagnosisIsEmpty()
    {
        var sut = CreateSut();
        sut.TcmDiagnosis = null;

        var result = sut.Validate();

        result.Should().BeFalse();
        sut.ValidationMessage.Should().Be("请填写中医诊断");
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenTcmDiagnosisHasValue()
    {
        var sut = CreateSut();
        sut.TcmDiagnosis = "感冒";

        var result = sut.Validate();

        result.Should().BeTrue();
        sut.ValidationMessage.Should().BeEmpty();
    }

    [Fact]
    public void Validate_SetsValidationMessage_AndRaisesErrorsChanged()
    {
        var sut = CreateSut();
        sut.TcmDiagnosis = null;
        var errorsChangedEvents = new List<DataErrorsChangedEventArgs?>();
        ((INotifyDataErrorInfo)sut).ErrorsChanged += (_, e) => errorsChangedEvents.Add(e);

        sut.Validate();

        errorsChangedEvents.Should().HaveCount(1);
        errorsChangedEvents[0]!.PropertyName.Should().Be(nameof(ConsultationItem.TcmDiagnosis));
    }

    [Fact]
    public void Reset_ClearsAllFields_ExceptIds()
    {
        var sut = CreateSut();
        sut.Id = Guid.NewGuid();
        sut.MedicalCaseId = Guid.NewGuid();
        sut.PatientId = Guid.NewGuid();
        sut.UserId = Guid.NewGuid();
        sut.PresentIllness = "头痛";
        sut.TongueDiagnosis = "舌红";
        sut.PulseDiagnosis = "脉弦";
        sut.TcmDiagnosis = "肝阳上亢";

        sut.Reset();

        sut.PresentIllness.Should().BeNull();
        sut.TongueDiagnosis.Should().BeNull();
        sut.PulseDiagnosis.Should().BeNull();
        sut.TcmDiagnosis.Should().BeNull();
        sut.Id.Should().NotBe(Guid.Empty);
        sut.MedicalCaseId.Should().NotBe(Guid.Empty);
        sut.PatientId.Should().NotBe(Guid.Empty);
        sut.UserId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GetConsultationData_ReturnsValidDto()
    {
        var sut = CreateSut();
        sut.Id = Guid.NewGuid();
        sut.MedicalCaseId = Guid.NewGuid();
        sut.PatientId = Guid.NewGuid();
        sut.UserId = Guid.NewGuid();
        sut.PatientName = "张三";
        sut.PresentIllness = "头痛三天";
        sut.TongueDiagnosis = "舌红苔黄";
        sut.PulseDiagnosis = "脉弦数";
        sut.TcmDiagnosis = "肝阳上亢证";

        var dto = sut.GetConsultationData();

        dto.Should().NotBeNull();
        dto!.PresentIllness.Should().Be("头痛三天");
        dto.TongueDiagnosis.Should().Be("舌红苔黄");
        dto.PulseDiagnosis.Should().Be("脉弦数");
        dto.TcmDiagnosis.Should().Be("肝阳上亢证");
    }

    [Fact]
    public void GetPrescriptionData_ReturnsNull()
    {
        var sut = CreateSut();

        var dto = sut.GetPrescriptionData();

        dto.Should().BeNull();
    }
}
