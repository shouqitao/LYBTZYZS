using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.Infrastructure;

/// <summary>
/// User Journey 测试夹具
/// 管理测试生命周期：SQL Server LocalDB 数据库初始化和清理
/// </summary>
public class UserJourneyFixture : IAsyncLifetime, IDisposable
{
    private ServiceProvider? _serviceProvider;
    private bool _isInitialized;
    private string _connectionString = null!;

    /// <summary>
    /// 获取 ServiceProvider 实例
    /// </summary>
    public IServiceProvider ServiceProvider =>
        _serviceProvider ?? throw new InvalidOperationException("Fixture not initialized");

    /// <summary>
    /// 获取 LocalDbContext 实例
    /// </summary>
    public LocalDbContext DbContext =>
        ServiceProvider.GetRequiredService<LocalDbContext>();

    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    public string ConnectionString => _connectionString;

    /// <summary>
    /// 异步初始化测试夹具
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        // 初始化 WPF 环境
        WpfTestHelper.InitializeWpf();

        // 使用 SQL Server LocalDB（与生产环境一致），每个夹具实例使用独立数据库
        var dbName = $"LYBTZYZS_UserJourneyTests_{Guid.NewGuid():N}";
        _connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True";

        // 创建 ServiceProvider
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // 创建数据库架构
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        await context.Database.EnsureCreatedAsync();

        _isInitialized = true;
    }

    /// <summary>
    /// 异步清理测试夹具
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
        {
            // 删除测试数据库
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            await context.Database.EnsureDeletedAsync();

            await _serviceProvider.DisposeAsync();
            _serviceProvider = null;
        }

        _isInitialized = false;
    }

    /// <summary>
    /// 同步清理测试夹具
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 创建新的 ServiceProvider 作用域
    /// 用于需要隔离的测试场景
    /// </summary>
    public IServiceScope CreateScope() =>
        ServiceProvider.CreateScope();

    /// <summary>
    /// 重置数据库状态
    /// 删除所有数据但保留表结构
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocalDbContext>();

        // 按依赖关系顺序删除数据
        context.PrescriptionItems.RemoveRange(context.PrescriptionItems);
        context.Prescriptions.RemoveRange(context.Prescriptions);
        context.Consultations.RemoveRange(context.Consultations);
        context.MedicalCasePrintLogs.RemoveRange(context.MedicalCasePrintLogs);
        context.MedicalCases.RemoveRange(context.MedicalCases);
        context.FormulaHerbItems.RemoveRange(context.FormulaHerbItems);
        context.Formulas.RemoveRange(context.Formulas);
        context.Herbs.RemoveRange(context.Herbs);
        context.Patients.RemoveRange(context.Patients);
        context.Users.RemoveRange(context.Users);

        await context.SaveChangesAsync();

        // 重置测试数据工厂计数器
        TestDataFactory.ResetCounters();
    }

    /// <summary>
    /// 配置 DI 服务
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // 日志
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // 当前用户 Provider (mock)
        var currentUserProvider = Substitute.For<ICurrentUserProvider>();
        currentUserProvider.CurrentUserId.Returns(Guid.NewGuid());
        services.AddSingleton(currentUserProvider);

        // LocalDbContext (SQL Server LocalDB，与生产环境一致)
        services.AddDbContext<LocalDbContext>(options =>
        {
            options.UseSqlServer(_connectionString);
        }, ServiceLifetime.Scoped);

        // 注册 Repository 和 Service 的 mock/fake 实现
        // 子类可以覆盖此方法添加更多服务
    }
}

/// <summary>
/// 测试集合定义，共享 UserJourneyFixture
/// </summary>
[CollectionDefinition("UserJourney")]
public class UserJourneyCollection : ICollectionFixture<UserJourneyFixture>
{
    // 此集合用于共享 UserJourneyFixture 实例
    // 标记为 [Collection("UserJourney")] 的测试类将共享同一个夹具实例
}
