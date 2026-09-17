using FluentAssertions;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server._Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Server.Integration.SqlCore;

/// <summary>
/// MedicalCase 真实 SQL Server 集成测试 — 覆盖 CRUD、Complete 状态流转、软删除查询过滤。
/// 使用 AppDbContext（MedicalCase 对 Patient/User 有 FK，需同一上下文）。
/// </summary>
[Collection(SqlServerIntegrationCollection.Name)]
public class MedicalCaseSqlIntegrationTests : IntegrationTestBase
{
    protected override DbContext CreateContext()
        => new AppDbContext(TestDbFactory.CreateOptions<AppDbContext>(ConnectionString));

    private static async Task<(Patient Patient, ApplicationUser Doctor)> SeedPatientAndDoctorAsync(AppDbContext context)
    {
        var patient = Patient.Create($"患者_{Guid.NewGuid():N}"[..12], Gender.Male);
        var doctor = ApplicationUser.Create(
            $"doc_{Guid.NewGuid():N}"[..12],
            "李医生",
            UserRole.Doctor);

        context.Patients.Add(patient);
        context.Users.Add(doctor);
        await context.SaveChangesAsync();
        return (patient, doctor);
    }

    private static MedicalCase CreateCase(Patient patient, ApplicationUser doctor)
    {
        return new MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            PatientName = patient.Name,
            UserId = doctor.Id,
            DoctorName = doctor.RealName,
            CaseStatus = MedicalCaseStatus.Active,
            NeedsPrescription = false
        };
    }

    [Fact]
    public async Task CreateAndGetByIdAndUpdate_ShouldPersistThroughSqlServer()
    {
        await using var context = (AppDbContext)CreateContext();
        var (patient, doctor) = await SeedPatientAndDoctorAsync(context);

        var entity = CreateCase(patient, doctor);
        context.MedicalCases.Add(entity);
        await context.SaveChangesAsync();

        // GetById — 新上下文 + AsNoTracking，走真实 SELECT
        await using var readContext = (AppDbContext)CreateContext();
        var loaded = await readContext.MedicalCases.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == entity.Id);

        loaded.Should().NotBeNull();
        loaded!.PatientId.Should().Be(patient.Id);
        loaded.UserId.Should().Be(doctor.Id);
        loaded.CaseStatus.Should().Be(MedicalCaseStatus.Active);
        loaded.IsDeleted.Should().BeFalse();
        loaded.CreatedAt.Should().NotBe(default);

        // Update
        loaded.DoctorName = "王医生";
        loaded.NeedsPrescription = true;
        readContext.MedicalCases.Update(loaded);
        await readContext.SaveChangesAsync();

        await using var verifyContext = (AppDbContext)CreateContext();
        var updated = await verifyContext.MedicalCases.AsNoTracking()
            .SingleAsync(m => m.Id == entity.Id);
        updated.DoctorName.Should().Be("王医生");
        updated.NeedsPrescription.Should().BeTrue();
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Complete_ShouldTransitionToCompletedAndSetCompletedAt()
    {
        await using var context = (AppDbContext)CreateContext();
        var (patient, doctor) = await SeedPatientAndDoctorAsync(context);

        var entity = CreateCase(patient, doctor);
        context.MedicalCases.Add(entity);
        await context.SaveChangesAsync();

        entity.Complete();
        await context.SaveChangesAsync();

        await using var readContext = (AppDbContext)CreateContext();
        var loaded = await readContext.MedicalCases.AsNoTracking()
            .SingleAsync(m => m.Id == entity.Id);

        loaded.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        loaded.CompletedAt.Should().NotBeNull();
        loaded.IsCompleted.Should().BeTrue();
        loaded.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDelete_ShouldHideFromDefaultQuery_AndVisibleWithIgnoreQueryFilters()
    {
        await using var context = (AppDbContext)CreateContext();
        var (patient, doctor) = await SeedPatientAndDoctorAsync(context);

        var entity = CreateCase(patient, doctor);
        context.MedicalCases.Add(entity);
        await context.SaveChangesAsync();

        entity.SoftDelete();
        await context.SaveChangesAsync();

        // 默认查询过滤软删除
        await using var readContext = (AppDbContext)CreateContext();
        var filtered = await readContext.MedicalCases.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == entity.Id);
        filtered.Should().BeNull("软删除后全局查询过滤器应隐藏该医案");

        // IgnoreQueryFilters 可见
        var withFilterIgnored = await readContext.MedicalCases
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == entity.Id);
        withFilterIgnored.Should().NotBeNull();
        withFilterIgnored!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldRemoveRowFromDatabase()
    {
        await using var context = (AppDbContext)CreateContext();
        var (patient, doctor) = await SeedPatientAndDoctorAsync(context);

        var entity = CreateCase(patient, doctor);
        context.MedicalCases.Add(entity);
        await context.SaveChangesAsync();

        context.MedicalCases.Remove(entity);
        await context.SaveChangesAsync();

        await using var readContext = (AppDbContext)CreateContext();
        var gone = await readContext.MedicalCases
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == entity.Id);
        gone.Should().BeNull("物理删除后行应不存在");
    }
}
