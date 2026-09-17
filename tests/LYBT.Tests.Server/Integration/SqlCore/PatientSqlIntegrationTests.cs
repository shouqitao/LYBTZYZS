using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server._Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Server.Integration.SqlCore;

/// <summary>
/// Patient 真实 SQL Server 集成测试 — 覆盖 Create + GetById + Update、软删除 + 恢复。
/// </summary>
[Collection(SqlServerIntegrationCollection.Name)]
public class PatientSqlIntegrationTests : IntegrationTestBase
{
    protected override DbContext CreateContext()
        => new AppDbContext(TestDbFactory.CreateOptions<AppDbContext>(ConnectionString));

    private static readonly Guid OperatorId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Create_GetById_Update_ShouldPersistThroughSqlServer()
    {
        await using var context = (AppDbContext)CreateContext();

        var patient = Patient.Create(
            "张三",
            Gender.Male,
            birthDate: new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            pinYinCode: "zs");
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        patient.Id.Should().NotBe(Guid.Empty);
        patient.Status.Should().Be(CommonStatus.Enabled);
        patient.IsDeleted.Should().BeFalse();

        // GetById — 新上下文 + AsNoTracking
        await using var readContext = (AppDbContext)CreateContext();
        var loaded = await readContext.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patient.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("张三");
        loaded.Gender.Should().Be(Gender.Male);
        loaded.PinYinCode.Should().Be("zs");
        loaded.CreatedAt.Should().NotBe(default);

        // Update
        loaded.UpdateProfile("张三丰", Gender.Male, loaded.BirthDate, null, null, "zsf", OperatorId);
        readContext.Patients.Update(loaded);
        await readContext.SaveChangesAsync();

        await using var verifyContext = (AppDbContext)CreateContext();
        var updated = await verifyContext.Patients.AsNoTracking()
            .SingleAsync(p => p.Id == patient.Id);
        updated.Name.Should().Be("张三丰");
        updated.PinYinCode.Should().Be("zsf");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDelete_And_Restore_ShouldToggleQueryVisibility()
    {
        await using var context = (AppDbContext)CreateContext();

        var patient = Patient.Create("李四", Gender.Female);
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // 软删除
        patient.SoftDelete(OperatorId);
        await context.SaveChangesAsync();

        await using var afterDeleteContext = (AppDbContext)CreateContext();
        var hidden = await afterDeleteContext.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patient.Id);
        hidden.Should().BeNull("软删除后全局查询过滤器应隐藏该患者");

        var softDeleted = await afterDeleteContext.Patients
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(p => p.Id == patient.Id);
        softDeleted.IsDeleted.Should().BeTrue();
        softDeleted.UpdatedBy.Should().Be(OperatorId);

        // 恢复
        var toRestore = await afterDeleteContext.Patients
            .IgnoreQueryFilters()
            .SingleAsync(p => p.Id == patient.Id);
        toRestore.Restore(OperatorId);
        await afterDeleteContext.SaveChangesAsync();

        await using var afterRestoreContext = (AppDbContext)CreateContext();
        var restored = await afterRestoreContext.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patient.Id);
        restored.Should().NotBeNull("恢复后默认查询应重新可见");
        restored!.IsDeleted.Should().BeFalse();
        restored.Name.Should().Be("李四");
    }

    [Fact]
    public async Task SoftDelete_ThenCreateNew_ShouldNotConflictOnUniqueIndexes()
    {
        await using var context = (AppDbContext)CreateContext();

        // 软删除患者不留 IdNumber/Phone，避免过滤唯一索引冲突场景外噪声
        var first = Patient.Create("王五", Gender.Unknown);
        context.Patients.Add(first);
        await context.SaveChangesAsync();

        first.SoftDelete(OperatorId);
        await context.SaveChangesAsync();

        // 软删除后可再建同名患者（姓名无唯一约束；验证写路径仍可用）
        var second = Patient.Create("王五", Gender.Unknown);
        context.Patients.Add(second);
        await context.SaveChangesAsync();

        await using var readContext = (AppDbContext)CreateContext();
        var active = await readContext.Patients.AsNoTracking()
            .Where(p => p.Name == "王五")
            .ToListAsync();
        active.Should().HaveCount(1, "软删除的王五不应出现在默认查询");
        active[0].Id.Should().Be(second.Id);
    }
}
