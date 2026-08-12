using LYBT.Infrastructure.Data;
using LYBT.Module.Catalog.Infrastructure;
using LYBT.Module.Identity.Infrastructure;
using LYBT.Module.MedicalCases.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Tests.Server.Integration.Data;

/// <summary>
/// L2 集成层测试（P1 批次——方案 §五 L2）：EF 映射冒烟——
/// SQLite in-memory（真实 SQL 引擎 + 约束校验——社区校准 §7.1）建表 + 查询，
/// 拦截 InMemory 盲区（InMemory 不校验列映射——上线坑 #2 根源）。
/// 真实 SQL Server 变体（TestDatabase.IsConfigured 时）验证列映射与生产一致。
/// </summary>
public class DbContextMappingSmokeTests
{
    public static IEnumerable<object[]> DbContextCases()
    {
        yield return new object[] { "AppDbContext", (Func<DbContext>)(() => CreateAppDbContext()) };
        yield return new object[] { "IdentityDbContext", (Func<DbContext>)(() => CreateIdentityDbContext()) };
        yield return new object[] { "CatalogDbContext", (Func<DbContext>)(() => CreateCatalogDbContext()) };
        yield return new object[] { "MedicalCaseDbContext", (Func<DbContext>)(() => CreateMedicalCaseDbContext()) };
    }

    private static AppDbContext CreateAppDbContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static IdentityDbContext CreateIdentityDbContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new IdentityDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static CatalogDbContext CreateCatalogDbContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new CatalogDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static MedicalCaseDbContext CreateMedicalCaseDbContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<MedicalCaseDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new MedicalCaseDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Theory]
    [MemberData(nameof(DbContextCases))]
    public void EnsureCreated_AllDbContexts_BuildSchema(string name, Func<DbContext> create)
    {
        // SQLite 建表成功 = 无严重映射错误（列/表/关系定义有效）
        using var ctx = create();
        var tables = ctx.Database.SqlQueryRaw<string>(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name").ToList();
        tables.Should().NotBeEmpty($"{name} 应生成至少一个表");
    }

    [Fact]
    public void IdentityDbContext_LastLoginTime_ColumnMapping_IsValid()
    {
        // #2 核心守护：ApplicationUser.LastLoginTime → LastLoginAt 列（SQLite 建表验证映射）
        using var ctx = CreateIdentityDbContext();
        var entityType = ctx.Model.FindEntityType(typeof(LYBT.Entities.Users.ApplicationUser));
        var property = entityType!.FindProperty(nameof(LYBT.Entities.Users.ApplicationUser.LastLoginTime));
        property!.GetColumnName().Should().Be("LastLoginAt");
    }

    [Fact]
    public void SqlServer_MappingSmoke_WhenConfigured()
    {
        // 真实 SQL Server 变体（决策 1: 192.168.190.243 LYBTDB_Test——TEST_DB_CONNECTION 驱动）——
        // 未配置时静默通过（CI/本机无测试库）；配置后验证 EnsureCreated 真实建表（列映射与生产一致）
        if (!TestDatabase.IsConfigured)
            return; // 未配置 TEST_DB_CONNECTION——真实 SQL 验证跳过（标注：需用户设置环境变量）

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(TestDatabase.ConnectionString)
            .Options;
        using var ctx = new IdentityDbContext(options);
        ctx.Database.EnsureCreated();
        // 查询 Users 表——真实 SQL 下列映射错误会抛（LastLoginAt 等）
        var users = ctx.Set<LYBT.Entities.Users.ApplicationUser>().Take(1).ToList();
        users.Should().NotBeNull();
    }
}
