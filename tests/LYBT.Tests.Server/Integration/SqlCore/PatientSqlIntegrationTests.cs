using System.Security.Claims;
using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Infrastructure;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server._Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Tests.Server.Integration.SqlCore;

/// <summary>
/// Patient 真实 SQL Server 集成测试 — 覆盖 Create + GetById + Update、软删除 + 恢复。
/// </summary>
[Collection(SqlServerIntegrationCollection.Name)]
public class PatientSqlIntegrationTests : IntegrationTestBase
{
    protected override DbContext CreateContext()
        => new AppDbContext(TestDbFactory.CreateOptions<AppDbContext>(ConnectionString), CreateOperatorAccessor());

    private static readonly Guid OperatorId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    /// <summary>
    /// 带操作者声明的 HttpContext 访问器：审计自动化（<c>DbContextAuditExtensions.SetAuditFields</c>）
    /// 以「环境上下文操作者」为准强制写 UpdatedBy（防伪冒），无 HttpContext 时归属 System 用户
    /// （AppDbContext.GetCurrentUserId 的 P1-3 兜底）。本测试要验证「软删除/恢复由当前操作者留痕」，
    /// 故显式提供声明为 <see cref="OperatorId"/> 的上下文。
    /// </summary>
    private static IHttpContextAccessor CreateOperatorAccessor()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, OperatorId.ToString())
            ], authenticationType: "Test"))
        };

        return new HttpContextAccessor { HttpContext = httpContext };
    }

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

    // ------------------------------------------------------------------
    // R-6 手机号 HMAC 盲索引（2026-09-23 修复：加密列 LIKE 致患者搜索 500）
    // ------------------------------------------------------------------

    private PatientRepository CreateRepository(PatientsDbContext context)
        => new(context, NullLogger<PatientRepository>.Instance);

    private PatientsDbContext CreatePatientsContext()
        => new(TestDbFactory.CreateOptions<PatientsDbContext>(ConnectionString), CreateOperatorAccessor());

    [Fact]
    public async Task KeywordSearch_ByName_DoesNotThrow_AndReturnsMatch()
    {
        // 回归守卫：原实现 keyword 谓词含 `p.PhoneNumber.Contains(kw)`（加密列），EF 生成
        // `LIKE @p ESCAPE N'<本次加密后的密文>'`——密文含非法转义字符时 SQL Server 抛
        // 「invalid escape character … LIKE predicate」→ 患者搜索 500（US-PAT-001 状态取证）。
        await using var context = CreatePatientsContext();
        var repository = CreateRepository(context);

        var zhang = Patient.Create("张三", Gender.Male, phoneNumber: "13800001234", createdBy: OperatorId);
        await repository.AddAsync(zhang);
        var li = Patient.Create("李四", Gender.Female, phoneNumber: "13900005678", createdBy: OperatorId);
        await repository.AddAsync(li);

        var byName = await repository.GetPagedAsync(1, 20, "张", null);

        byName.TotalCount.Should().Be(1);
        byName.Items.Should().ContainSingle(p => p.Id == zhang.Id);
    }

    [Fact]
    public async Task KeywordSearch_ByFullPhone_MatchesViaBlindIndex_AndFragmentDoesNotThrow()
    {
        await using var context = CreatePatientsContext();
        var repository = CreateRepository(context);

        var zhang = Patient.Create("张三", Gender.Male, phoneNumber: "13800001234", createdBy: OperatorId);
        await repository.AddAsync(zhang);

        // 完整手机号 → HMAC 盲索引精确匹配
        var byPhone = await repository.GetPagedAsync(1, 20, "13800001234", null);
        byPhone.TotalCount.Should().Be(1);
        byPhone.Items.Should().ContainSingle(p => p.Id == zhang.Id);

        // 片段：加密列无法片段匹配 → 不命中、不报错（口径见 US-PAT-001：按姓名/电话/拼音首字母筛选）
        var byFragment = await repository.GetPagedAsync(1, 20, "1234", null);
        byFragment.Items.Should().BeEmpty();
        byFragment.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ExistsByPhoneAsync_DetectsDuplicate_AndExcludesSelf()
    {
        // 回归守卫：原实现比较加密列 `p.PhoneNumber == phoneNumber`，随机 nonce 致密文永不相等
        // → 电话查重恒 false（重复患者静默放行）。改走 HMAC 盲索引后必须能检出。
        await using var context = CreatePatientsContext();
        var repository = CreateRepository(context);

        var first = Patient.Create("张三", Gender.Male, phoneNumber: "13800001234", createdBy: OperatorId);
        await repository.AddAsync(first);

        (await repository.ExistsByPhoneAsync("13800001234")).Should().BeTrue();
        (await repository.ExistsByPhoneAsync("13800001234", excludeId: first.Id)).Should().BeFalse();
        (await repository.ExistsByPhoneAsync("13900000000")).Should().BeFalse();
    }

    [Fact]
    public async Task Repository_WritesPhoneSearchHash_OnCreateAndUpdate()
    {
        await using var context = CreatePatientsContext();
        var repository = CreateRepository(context);

        var patient = Patient.Create("张三", Gender.Male, phoneNumber: "13800001234", createdBy: OperatorId);
        await repository.AddAsync(patient);

        var createdHash = patient.PhoneSearchHash;
        createdHash.Should().NotBeNullOrWhiteSpace();
        createdHash.Should().NotBe("13800001234", "盲索引必须是 HMAC 摘要而非明文");

        // 更新手机号 → 摘要随之变化；清空手机号 → 摘要清空
        patient.UpdateProfile("张三", Gender.Male, patient.BirthDate, "13911112222", null, null, OperatorId);
        await repository.UpdateAsync(patient);
        patient.PhoneSearchHash.Should().NotBe(createdHash);

        (await repository.ExistsByPhoneAsync("13911112222")).Should().BeTrue();
        (await repository.ExistsByPhoneAsync("13800001234")).Should().BeFalse("旧号码摘要应随更新失效");

        patient.UpdateProfile("张三", Gender.Male, patient.BirthDate, null, null, null, OperatorId);
        await repository.UpdateAsync(patient);
        patient.PhoneSearchHash.Should().BeNull();
    }
}
