using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.WebAPI.BackgroundServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services;

/// <summary>
/// SecurityAuditCleanupService 单元测试
/// Issue #1873 - 安全审计日志清理后台服务测试
/// </summary>
public class SecurityAuditCleanupServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _databaseName;
    private readonly SecurityAuditCleanupService _sut;

    public SecurityAuditCleanupServiceTests()
    {
        _databaseName = Guid.NewGuid().ToString();

        // 创建服务容器
        var services = new ServiceCollection();

        // 注册 AppDbContext（让DI容器管理生命周期）
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: _databaseName));

        services.AddLogging(builder => builder.AddConsole());

        _serviceProvider = services.BuildServiceProvider();

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = _serviceProvider.GetRequiredService<ILogger<SecurityAuditCleanupService>>();

        _sut = new SecurityAuditCleanupService(scopeFactory, logger);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    #region 清理逻辑测试

    [Fact]
    public async Task CleanupOldLogs_WithOldLogs_ShouldDeleteThem()
    {
        // Arrange - 添加测试数据
        Guid recentLogId;
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var oldLog1 = new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                EventType = "Login",
                CreatedAt = DateTime.UtcNow.AddDays(-31), // 31天前
                Success = true
            };

            var oldLog2 = new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                EventType = "Logout",
                CreatedAt = DateTime.UtcNow.AddDays(-35), // 35天前
                Success = true
            };

            var recentLog = new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                EventType = "Login",
                CreatedAt = DateTime.UtcNow.AddDays(-20), // 20天前（应保留）
                Success = true
            };

            recentLogId = recentLog.Id;

            await context.SecurityAuditLogs.AddRangeAsync(oldLog1, oldLog2, recentLog);
            await context.SaveChangesAsync();
        }

        // Act - 使用反射调用私有方法 CleanupOldLogsAsync
        var method = typeof(SecurityAuditCleanupService)
            .GetMethod("CleanupOldLogsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task)method!.Invoke(_sut, new object[] { CancellationToken.None })!;
        await task;

        // Assert
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var remainingLogs = await context.SecurityAuditLogs.ToListAsync();
            remainingLogs.Should().HaveCount(1);
            remainingLogs[0].Id.Should().Be(recentLogId);
        }
    }

    [Fact]
    public async Task CleanupOldLogs_WithNoOldLogs_ShouldNotDeleteAnything()
    {
        // Arrange - 添加测试数据（都是近期的）
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var recentLog1 = new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                EventType = "Login",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Success = true
            };

            var recentLog2 = new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                EventType = "Logout",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Success = true
            };

            await context.SecurityAuditLogs.AddRangeAsync(recentLog1, recentLog2);
            await context.SaveChangesAsync();
        }

        // Act
        var method = typeof(SecurityAuditCleanupService)
            .GetMethod("CleanupOldLogsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task)method!.Invoke(_sut, new object[] { CancellationToken.None })!;
        await task;

        // Assert
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var remainingLogs = await context.SecurityAuditLogs.ToListAsync();
            remainingLogs.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task CleanupOldLogs_WithExactly30DaysOld_ShouldNotDelete()
    {
        // Arrange - 添加刚好30天前的日志（边界条件）
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var boundaryLog = new SecurityAuditLog
            {
                Id = Guid.NewGuid(),
                EventType = "Login",
                CreatedAt = DateTime.UtcNow.AddDays(-30).AddMinutes(1), // 29天23小时59分钟（应保留）
                Success = true
            };

            await context.SecurityAuditLogs.AddAsync(boundaryLog);
            await context.SaveChangesAsync();
        }

        // Act
        var method = typeof(SecurityAuditCleanupService)
            .GetMethod("CleanupOldLogsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task)method!.Invoke(_sut, new object[] { CancellationToken.None })!;
        await task;

        // Assert
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var remainingLogs = await context.SecurityAuditLogs.ToListAsync();
            remainingLogs.Should().HaveCount(1);
        }
    }

    #endregion

    #region 后台服务启动/停止测试

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldStopGracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act - 启动服务后立即取消
        var executeTask = _sut.StartAsync(cts.Token);
        cts.Cancel();

        // 等待服务停止（最多5秒）
        var completedTask = await Task.WhenAny(executeTask, Task.Delay(5000));

        // Assert
        completedTask.Should().Be(executeTask, "服务应该在取消后立即停止");
        await _sut.StopAsync(CancellationToken.None);
    }


    #endregion
}
