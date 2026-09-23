using FluentAssertions;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Server;

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
        // IsLocked 按「诊所本地日期」比较（ClinicTime，默认 Asia/Shanghai）。
        // 原用 DateTime.UtcNow.Date.AddHours(1)：当 UTC ≥ 16:00 时该时刻的诊所日期已跨到次日，
        // 于是「当天完成」被算成「昨天完成」→ 测试在下午/晚间必失败（时间相关假红）。
        // 改为取当前 UTC 时刻本身——与实现同一时钟，任何时刻都必然同属一个诊所日期。
        var mc = new MedicalCase
        {
            CaseStatus = MedicalCaseStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };
        mc.IsLocked.Should().BeFalse("当天完成的医案不应被锁定");
    }

    [Fact]
    public void IsLocked_ShouldReturnTrue_WhenCompletedBeforeToday()
    {
        var mc = new MedicalCase
        {
            CaseStatus = MedicalCaseStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddDays(-2)
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
