using FluentAssertions;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Entities.MedicalCases;

/// <summary>
/// MedicalCase entity tests - computed properties, navigation, and business rules.
/// Trivial property getter/setter tests removed (test restructuring 2026-03-05).
/// </summary>
public class MedicalCaseModelTests
{
    #region IsLocked Computed Property

    [Fact]
    public void IsLocked_ShouldReturnFalse_WhenActive()
    {
        var mc = new MedicalCase { CaseStatus = MedicalCaseStatus.Active };
        mc.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void IsLocked_ShouldReturnFalse_WhenSuspended()
    {
        var mc = new MedicalCase { CaseStatus = MedicalCaseStatus.Suspended };
        mc.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void IsLocked_ShouldReturnFalse_WhenCompletedToday()
    {
        var mc = new MedicalCase
        {
            CaseStatus = MedicalCaseStatus.Completed,
            CompletedAt = DateTime.Today.AddHours(1)
        };
        mc.IsLocked.Should().BeFalse("当天完成的医案不应被锁定");
    }

    [Fact]
    public void IsLocked_ShouldReturnTrue_WhenCompletedBeforeToday()
    {
        var mc = new MedicalCase
        {
            CaseStatus = MedicalCaseStatus.Completed,
            CompletedAt = DateTime.Today.AddDays(-1)
        };
        mc.IsLocked.Should().BeTrue("非当天完成的医案应该被锁定");
    }

    #endregion

    #region IsActive / IsCompleted Computed Properties

    [Theory]
    [InlineData(MedicalCaseStatus.Suspended, true)]
    [InlineData(MedicalCaseStatus.Active, true)]
    [InlineData(MedicalCaseStatus.Completed, false)]
    public void IsActive_ShouldReturnCorrectValue(MedicalCaseStatus status, bool expected)
    {
        var mc = new MedicalCase { CaseStatus = status };
        mc.IsActive.Should().Be(expected);
    }

    [Theory]
    [InlineData(MedicalCaseStatus.Active, false)]
    [InlineData(MedicalCaseStatus.Suspended, false)]
    [InlineData(MedicalCaseStatus.Completed, true)]
    public void IsCompleted_ShouldReturnCorrectValue(MedicalCaseStatus status, bool expected)
    {
        var mc = new MedicalCase { CaseStatus = status };
        mc.IsCompleted.Should().Be(expected);
    }

    #endregion

    #region Navigation Properties (Architectural Constraints)

    [Fact]
    public void Consultation_ShouldUseSharedPrimaryKey()
    {
        var mc = new MedicalCase { Id = Guid.NewGuid() };
        var consultation = new Consultation { Id = mc.Id };
        mc.Consultation = consultation;

        mc.Consultation.Id.Should().Be(mc.Id, "Consultation使用共享主键");
    }

    [Fact]
    public void Prescription_ShouldBeOptionalWithForeignKey()
    {
        var mc = new MedicalCase { Id = Guid.NewGuid() };
        var prescription = new Prescription { MedicalCaseId = mc.Id };
        mc.Prescription = prescription;

        mc.Prescription.MedicalCaseId.Should().Be(mc.Id);
    }

    #endregion

    #region NeedsPrescription Three-State

    [Fact]
    public void NeedsPrescription_ShouldSupportThreeStates()
    {
        var mc = new MedicalCase();
        mc.NeedsPrescription.Should().BeNull("默认未标记");

        mc.NeedsPrescription = true;
        mc.NeedsPrescription.Should().BeTrue();

        mc.NeedsPrescription = false;
        mc.NeedsPrescription.Should().BeFalse();
    }

    #endregion
}
