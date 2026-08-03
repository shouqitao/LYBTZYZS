using FluentAssertions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Validators.BusinessRules;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// MedicalCaseBusinessRules 单元测试
/// 守护医案状态机：更新状态端点仅允许 Suspended↔Active，
/// Completed 必须走 CompleteAsync（US-MC-010 / BR-003）。
/// T4-B9: UpdateMedicalCaseStatusCommandHandler 曾绕过该校验直接赋值状态。
/// </summary>
public class MedicalCaseBusinessRulesTests
{
    [Theory]
    [InlineData(MedicalCaseStatus.Active, MedicalCaseStatus.Suspended, true)]
    [InlineData(MedicalCaseStatus.Suspended, MedicalCaseStatus.Active, true)]
    [InlineData(MedicalCaseStatus.Active, MedicalCaseStatus.Completed, false)]
    [InlineData(MedicalCaseStatus.Suspended, MedicalCaseStatus.Completed, false)]
    [InlineData(MedicalCaseStatus.Completed, MedicalCaseStatus.Active, false)]
    [InlineData(MedicalCaseStatus.Completed, MedicalCaseStatus.Suspended, false)]
    [InlineData(MedicalCaseStatus.Completed, MedicalCaseStatus.Completed, false)]
    [InlineData(MedicalCaseStatus.Active, MedicalCaseStatus.Active, false)]
    [InlineData(MedicalCaseStatus.Suspended, MedicalCaseStatus.Suspended, false)]
    public void IsValidStatusTransition_OnlyAllowsSuspendedActiveBidirectional(
        MedicalCaseStatus from, MedicalCaseStatus to, bool expected)
    {
        MedicalCaseBusinessRules.IsValidStatusTransition(from, to).Should().Be(expected);
    }

    [Fact]
    public void CanCreateNewCase_RejectsWhenActiveOrSuspendedExists()
    {
        MedicalCaseBusinessRules.CanCreateNewCase(new[] { MedicalCaseStatus.Completed }).Should().BeTrue();
        MedicalCaseBusinessRules.CanCreateNewCase(new[] { MedicalCaseStatus.Active }).Should().BeFalse();
        MedicalCaseBusinessRules.CanCreateNewCase(new[] { MedicalCaseStatus.Suspended }).Should().BeFalse();
    }

    [Fact]
    public void HasActiveCase_HasSuspendedCase_DetectSingleActiveConstraint()
    {
        MedicalCaseBusinessRules.HasActiveCase(new[] { MedicalCaseStatus.Active }).Should().BeTrue();
        MedicalCaseBusinessRules.HasActiveCase(new[] { MedicalCaseStatus.Completed }).Should().BeFalse();
        MedicalCaseBusinessRules.HasSuspendedCase(new[] { MedicalCaseStatus.Suspended }).Should().BeTrue();
        MedicalCaseBusinessRules.HasSuspendedCase(new[] { MedicalCaseStatus.Completed }).Should().BeFalse();
    }
}
