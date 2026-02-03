using LYBT.Desktop.LocalData.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Tests.TestFixtures;

/// <summary>
/// LocalDbContext 测试夹具 - 提供 SQLite InMemory 数据库支持
/// 每次调用 CreateContext() 创建独立的数据库连接，确保测试隔离
/// </summary>
public class LocalDbContextFixture : IDisposable
{
    private readonly List<SqliteConnection> _connections = new();
    private bool _disposed;

    /// <summary>
    /// 创建新的 DbContext 实例（独立数据库连接）
    /// </summary>
    public LocalDbContext CreateContext()
    {
        // 每次创建新连接，确保测试隔离
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

    /// <summary>
    /// 创建 Mock 的 ILogger
    /// </summary>
    public static ILogger<T> CreateLogger<T>()
    {
        return Substitute.For<ILogger<T>>();
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
