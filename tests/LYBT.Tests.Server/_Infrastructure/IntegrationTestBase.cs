using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Server._Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected string ConnectionString { get; private set; } = string.Empty;

    public virtual async Task InitializeAsync()
    {
        ConnectionString = TestDbFactory.GetTestConnectionString();

        // 确保数据库存在且 schema 与当前模型一致。
        // 必须先删后建：共享测试库（默认 LocalDB `LYBT_Test`，或 TEST_CONNECTION_STRING）在首次创建后
        // 会长期保留，`EnsureCreatedAsync` 对已存在的库**不会**补列/补索引——模型新增列（如 R-6
        // Patient.PhoneSearchHash）在测试库中缺失，测试会以「Invalid column name」失败并掩盖真实回归。
        // 类级集合 `SqlServerIntegrationCollection` 已串行化，无并发建库风险；数据在测试间仍由 Respawn 重置。
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await RespawnCheckpoint.ResetAsync(ConnectionString);
    }

    protected abstract DbContext CreateContext();
}
