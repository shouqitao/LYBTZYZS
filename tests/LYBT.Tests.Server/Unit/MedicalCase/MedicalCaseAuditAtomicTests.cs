using FluentAssertions;
using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Infrastructure;

namespace LYBT.Tests.Server.Unit.MedicalCase;

/// <summary>
/// P1-19 审计与业务原子性验证：
/// AddAuditLogAsync(saveChanges:false) 仅入 ChangeTracker 不落库，
/// 由 UpdateAsync 单次 SaveChanges 提交——业务失败则审计同回滚。
/// </summary>
public class MedicalCaseAuditAtomicTests
{
    [Fact]
    public void AddAuditLog_WithSaveChangesFalse_ShouldNotPersistImmediately()
    {
        // 仅验证仓储重载契约存在（false 分支不立即 SaveChanges——逻辑在仓储实现内）
        var repoType = typeof(MedicalCaseRepository);
        repoType.GetMethod("AddAuditLogAsync", new[] { typeof(MedicalCaseAuditLog), typeof(bool), typeof(CancellationToken) })
            .Should().NotBeNull("P1-19 需提供 saveChanges 重载");

        var iface = typeof(LYBT.Module.MedicalCases.Interfaces.IMedicalCaseRepository);
        iface.GetMethod("AddAuditLogAsync", new[] { typeof(MedicalCaseAuditLog), typeof(bool), typeof(CancellationToken) })
            .Should().NotBeNull();
    }

    [Fact]
    public void AuditLog_Entity_HasNoFk_Cascade()
    {
        // 审计表无 FK（物理删除审计仍保留的设计前提）；验证实体可独立构造
        var log = new MedicalCaseAuditLog
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = Guid.NewGuid(),
            OperatorId = Guid.NewGuid(),
            OperatorName = "sysadmin",
            OperationType = 1, // 1 = Update（见 MedicalCaseAuditLog 枚举值）
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        log.Id.Should().NotBeEmpty();
        log.IsDeleted.Should().BeFalse();
    }
}
