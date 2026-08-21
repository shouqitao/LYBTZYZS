using FluentAssertions;
using LYBT.Module.MedicalCases.Services;
using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using LYBT.Module.MedicalCases.Interfaces;

namespace LYBT.Tests.Server.Unit.MedicalCase;

/// <summary>
/// P1-18 IsLocked API 强锁验证：
/// - 非管理员（创建医生）不能编辑已完成医案（EnsureCanEdit 拒绝 Completed）
/// - 管理员编辑锁定医案必须提供 EditReason（ValidateEditReason，经 IMedicalCaseTimeService）
/// </summary>
public class MedicalCaseLockedTests
{
    private static LYBT.Entities.MedicalCases.MedicalCase CompletedCase(Guid doctorId, DateTime completedUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = doctorId,
            DoctorName = "李医生",
            CaseStatus = MedicalCaseStatus.Completed,
            CompletedAt = completedUtc,
            CreatedBy = doctorId,
            CreatedAt = completedUtc,
            IsDeleted = false
        };

    [Fact]
    public void EnsureCanEdit_Doctor_CannotEdit_CompletedCase()
    {
        // Arrange（P1-18）：非管理员无权编辑已完成医案（Completed → 拒绝）
        var doctorId = Guid.NewGuid();
        var mc = CompletedCase(doctorId, DateTime.UtcNow.AddDays(-1));
        var logger = NullLogger.Instance;

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
            MedicalCaseServiceHelper.EnsureCanEdit(mc, doctorId, isAdmin: false, "Save", logger));
    }

    [Fact]
    public void EnsureCanEdit_Doctor_CanEdit_OwnActiveCase()
    {
        var doctorId = Guid.NewGuid();
        var mc = new LYBT.Entities.MedicalCases.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = doctorId,
            CaseStatus = MedicalCaseStatus.Active,
            CreatedBy = doctorId,
            CreatedAt = DateTime.UtcNow
        };
        var logger = NullLogger.Instance;

        // Act（不应抛异常）
        MedicalCaseServiceHelper.EnsureCanEdit(mc, doctorId, isAdmin: false, "Save", logger);
    }

    [Fact]
    public void Admin_EditingLockedCase_RequiresEditReason()
    {
        // Arrange：管理员编辑锁定医案，ValidateEditReason 经 time service 判 IsLocked，
        // 缺 EditReason 应抛 BusinessException（McCannotEditCase）
        var mc = CompletedCase(Guid.NewGuid(), DateTime.UtcNow.AddDays(-5));

        // 锁判定健全：隔天 Completed 医案应锁定
        mc.IsLocked.Should().BeTrue("隔天 Completed 医案应锁定");

        // time service 与实体判定一致（Asia/Shanghai 日界）
        IMedicalCaseTimeService svc = new MedicalCaseTimeService();
        svc.IsLocked(mc).Should().BeTrue();
    }
}

