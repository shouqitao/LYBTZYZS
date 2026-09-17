using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Server._Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected string ConnectionString { get; private set; } = string.Empty;

    public virtual async Task InitializeAsync()
    {
        ConnectionString = TestDbFactory.GetTestConnectionString();
        // 确保数据库存在
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await RespawnCheckpoint.ResetAsync(ConnectionString);
    }

    protected abstract DbContext CreateContext();
}
