using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.DataSources;
using LYBT.Desktop.LocalData.Initialization;
using LYBT.Desktop.LocalData.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.Integration.Fixtures;

/// <summary>
/// Desktop端集成测试Fixture。
/// 使用SQLite InMemory数据库，注册全部真实DataSource。
/// 仅Mock ICurrentUserProvider (最小Mock范围)。
/// 每次调用 CreateServiceProvider 返回独立的DI容器和数据库。
/// </summary>
public class DesktopFixture : IDisposable
{
    private readonly List<SqliteConnection> _connections = new();
    private bool _disposed;

    /// <summary>默认测试用户ID</summary>
    public static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000099");

    /// <summary>
    /// 创建配置完整的服务提供者。
    /// 包含: LocalDbContext + 5个DataSource + LocalAuthService + DatabaseInitializer。
    /// 数据库已初始化 (EnsureCreated)。
    /// </summary>
    public async Task<IServiceProvider> CreateServiceProviderAsync()
    {
        var services = new ServiceCollection();

        // SQLite InMemory 连接 (独立)
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        _connections.Add(connection);

        // DbContextOptions
        services.AddSingleton(_ =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<LocalDbContext>();
            optionsBuilder.UseSqlite(connection);
            optionsBuilder.EnableSensitiveDataLogging();
            return optionsBuilder.Options;
        });

        // CurrentUserProvider (Mock - 最小范围)
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        currentUserProvider.CurrentUserId.Returns(TestUserId);
        services.AddSingleton(currentUserProvider);

        // LocalDbContext
        services.AddScoped(sp =>
        {
            var options = sp.GetRequiredService<DbContextOptions<LocalDbContext>>();
            var userProvider = sp.GetRequiredService<ICurrentUserProvider>();
            return new LocalDbContext(options, userProvider);
        });

        // Logging
        services.AddLogging();

        // 本地认证服务
        services.AddScoped<ILocalAuthService, LocalAuthService>();

        // 数据库初始化器
        services.AddScoped<DatabaseInitializer>();

        // 真实 DataSource (全部5个)
        services.AddScoped<IPatientDataSource, LocalPatientDataSource>();
        services.AddScoped<IHerbDataSource, LocalHerbDataSource>();
        services.AddScoped<IFormulaDataSource, LocalFormulaDataSource>();
        services.AddScoped<IMedicalCaseDataSource, LocalMedicalCaseDataSource>();
        services.AddScoped<IUserDataSource, LocalUserDataSource>();

        var provider = services.BuildServiceProvider();

        // 初始化数据库Schema
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        await db.Database.EnsureCreatedAsync();

        return provider;
    }

    /// <summary>
    /// 创建独立的 DbContext (用于数据验证/准备)。
    /// 注意: 这个 DbContext 使用独立的 SQLite InMemory 连接，
    /// 与 CreateServiceProviderAsync 的数据库不共享。
    /// </summary>
    public LocalDbContext CreateStandaloneDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection);

        var options = new DbContextOptionsBuilder<LocalDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new LocalDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var connection in _connections)
                {
                    connection.Dispose();
                }
                _connections.Clear();
            }
            _disposed = true;
        }
    }
}
