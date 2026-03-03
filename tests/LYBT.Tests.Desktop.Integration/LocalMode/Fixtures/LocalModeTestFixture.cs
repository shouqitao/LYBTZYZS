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

namespace LYBT.Tests.Desktop.Integration.LocalMode.Fixtures;

/// <summary>
/// 本地模式集成测试夹具
/// 提供完整的 DI 容器配置，模拟真实的本地模式运行环境
/// OpenSpec: implement-local-mode Phase 5.2
/// </summary>
public class LocalModeTestFixture : IDisposable
{
    private readonly List<SqliteConnection> _connections = new();
    private bool _disposed;

    /// <summary>
    /// 创建配置完整的服务提供者
    /// 包含: LocalDbContext, DataSources, Services
    /// </summary>
    public IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // 创建独立的 SQLite 连接（InMemory）
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection);

        // 注册 DbContextOptions
        services.AddSingleton(_ =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<LocalDbContext>();
            optionsBuilder.UseSqlite(connection);
            optionsBuilder.EnableSensitiveDataLogging();
            return optionsBuilder.Options;
        });

        // 注册 CurrentUserProvider（Mock）
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        currentUserProvider.CurrentUserId.Returns(Guid.NewGuid());
        services.AddSingleton(currentUserProvider);

        // 注册 LocalDbContext
        services.AddScoped(sp =>
        {
            var options = sp.GetRequiredService<DbContextOptions<LocalDbContext>>();
            var userProvider = sp.GetRequiredService<ICurrentUserProvider>();
            return new LocalDbContext(options, userProvider);
        });

        // 注册 Logging
        services.AddLogging();

        // 注册 LocalAuthService
        services.AddScoped<ILocalAuthService, LocalAuthService>();

        // 注册 DatabaseInitializer
        services.AddScoped<DatabaseInitializer>();

        // 注册 Local DataSources
        services.AddScoped<IPatientDataSource, LocalPatientDataSource>();
        services.AddScoped<IHerbDataSource, LocalHerbDataSource>();
        services.AddScoped<IFormulaDataSource, LocalFormulaDataSource>();
        services.AddScoped<IMedicalCaseDataSource, LocalMedicalCaseDataSource>();
        services.AddScoped<IUserDataSource, LocalUserDataSource>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 创建独立的 DbContext（用于数据准备）
    /// </summary>
    public LocalDbContext CreateDbContext()
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
