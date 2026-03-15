using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Initialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Startup;

public class DatabaseInitializerTests
{
    private readonly ILogger<DatabaseInitializer> _logger;
    private int _contextCreateCount;

    public DatabaseInitializerTests()
    {
        _logger = Substitute.For<ILogger<DatabaseInitializer>>();
        _contextCreateCount = 0;
    }

    private Func<LocalDbContext> CreateContextFactory()
    {
        return () =>
        {
            _contextCreateCount++;
            // 使用 SQL Server LocalDB 进行测试
            var options = new DbContextOptionsBuilder<LocalDbContext>()
                .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=LYBTZYZS_Test;Trusted_Connection=True;")
                .Options;

            var currentUserProvider = Substitute.For<ICurrentUserProvider>();
            currentUserProvider.CurrentUserId.Returns(Guid.NewGuid());

            return new LocalDbContext(options, currentUserProvider);
        };
    }

    [Fact]
    public async Task EnsureInitializedAsync_FirstCall_CreatesDatabase()
    {
        // Arrange
        var factory = CreateContextFactory();
        var initializer = new DatabaseInitializer(factory, _logger);

        // Act
        await initializer.EnsureInitializedAsync();

        // Assert
        Assert.Equal(1, _contextCreateCount);
    }

    [Fact]
    public async Task EnsureInitializedAsync_SecondCall_DoesNotRecreateDatabase()
    {
        // Arrange
        var factory = CreateContextFactory();
        var initializer = new DatabaseInitializer(factory, _logger);

        // Act
        await initializer.EnsureInitializedAsync();
        await initializer.EnsureInitializedAsync();

        // Assert
        Assert.Equal(1, _contextCreateCount); // 只创建一次
    }

    [Fact]
    public async Task EnsureInitializedAsync_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        var factory = CreateContextFactory();
        var initializer = new DatabaseInitializer(factory, _logger);

        // Act
        var tasks = new[]
        {
            Task.Run(() => initializer.EnsureInitializedAsync()),
            Task.Run(() => initializer.EnsureInitializedAsync()),
            Task.Run(() => initializer.EnsureInitializedAsync())
        };

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, _contextCreateCount); // 即使有并发调用，也只创建一次
    }

    [Fact]
    public async Task CanConnectAsync_ReturnsTrue_WhenDatabaseExists()
    {
        // Arrange
        var factory = CreateContextFactory();
        var initializer = new DatabaseInitializer(factory, _logger);
        await initializer.EnsureInitializedAsync();

        // Act
        var canConnect = await initializer.CanConnectAsync();

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task CanConnectAsync_ReturnsFalse_WhenFactoryThrows()
    {
        // Arrange
        var failingFactory = new Func<LocalDbContext>(() => throw new Exception("Connection failed"));
        var initializer = new DatabaseInitializer(failingFactory, _logger);

        // Act
        var canConnect = await initializer.CanConnectAsync();

        // Assert
        Assert.False(canConnect);
    }
}
