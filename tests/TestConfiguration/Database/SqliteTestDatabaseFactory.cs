using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Common.Database;

/// <summary>
/// SQLite InMemory测试数据库工厂
/// 提供比EF Core InMemoryDatabase更可靠的测试隔离
///
/// 优势:
/// 1. 真正的SQL语法支持
/// 2. 外键约束验证
/// 3. 完整的事务支持
/// 4. 每个测试独立的数据库实例(通过独立连接实现)
/// 5. 更接近SQL Server的行为
/// </summary>
public static class SqliteTestDatabaseFactory
{
    /// <summary>
    /// 创建SQLite InMemory连接
    /// 注意: 保持连接打开状态，关闭连接会删除内存数据库
    /// </summary>
    public static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// 配置DbContext使用SQLite InMemory数据库
    /// </summary>
    /// <typeparam name="TContext">DbContext类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="connection">SQLite连接(可选,不传则自动创建)</param>
    /// <returns>SQLite连接(需要在测试结束后Dispose)</returns>
    public static SqliteConnection AddSqliteInMemoryContext<TContext>(
        this IServiceCollection services,
        SqliteConnection? connection = null)
        where TContext : DbContext
    {
        connection ??= CreateConnection();

        services.AddDbContext<TContext>(options =>
        {
            options.UseSqlite(connection);
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        return connection;
    }

    /// <summary>
    /// 创建DbContextOptions用于SQLite InMemory测试
    /// </summary>
    /// <typeparam name="TContext">DbContext类型</typeparam>
    /// <param name="connection">SQLite连接</param>
    /// <returns>配置好的DbContextOptions</returns>
    public static DbContextOptions<TContext> CreateOptions<TContext>(SqliteConnection connection)
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;
    }

    /// <summary>
    /// 创建并初始化SQLite InMemory DbContext
    /// </summary>
    /// <typeparam name="TContext">DbContext类型</typeparam>
    /// <param name="contextFactory">DbContext工厂方法</param>
    /// <returns>元组(连接, 上下文) - 两者都需要在测试结束后Dispose</returns>
    public static (SqliteConnection Connection, TContext Context) CreateContext<TContext>(
        Func<DbContextOptions<TContext>, TContext> contextFactory)
        where TContext : DbContext
    {
        var connection = CreateConnection();
        var options = CreateOptions<TContext>(connection);
        var context = contextFactory(options);

        // 确保数据库Schema已创建
        context.Database.EnsureCreated();

        return (connection, context);
    }
}

/// <summary>
/// SQLite测试上下文包装器
/// 自动管理连接和上下文的生命周期
/// </summary>
/// <typeparam name="TContext">DbContext类型</typeparam>
public sealed class SqliteTestContext<TContext> : IDisposable, IAsyncDisposable
    where TContext : DbContext
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    /// <summary>
    /// 获取DbContext实例
    /// </summary>
    public TContext Context { get; }

    /// <summary>
    /// 创建SQLite测试上下文
    /// </summary>
    /// <param name="contextFactory">DbContext工厂方法</param>
    public SqliteTestContext(Func<DbContextOptions<TContext>, TContext> contextFactory)
    {
        _connection = SqliteTestDatabaseFactory.CreateConnection();
        var options = SqliteTestDatabaseFactory.CreateOptions<TContext>(_connection);
        Context = contextFactory(options);
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// 重置数据库(删除所有数据并重新创建Schema)
    /// </summary>
    public async Task ResetAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.Database.EnsureCreatedAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await Context.DisposeAsync();
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
    }
}
