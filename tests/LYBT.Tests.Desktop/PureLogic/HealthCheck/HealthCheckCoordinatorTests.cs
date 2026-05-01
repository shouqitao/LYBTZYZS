using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Shell.Services.HealthCheck;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.HealthCheck;

/// <summary>
/// HealthCheckCoordinator 单元测试
/// 验证异步健康检查行为
/// </summary>
public class HealthCheckCoordinatorTests
{
    private readonly IApiHealthCheckService _mockHealthCheckService;
    private readonly IApplicationTickService _mockTickService;
    private readonly IApplicationStateService _mockAppStateService;
    private readonly ILogger<HealthCheckCoordinator> _mockLogger;

    public HealthCheckCoordinatorTests()
    {
        _mockHealthCheckService = Substitute.For<IApiHealthCheckService>();
        _mockTickService = Substitute.For<IApplicationTickService>();
        _mockAppStateService = Substitute.For<IApplicationStateService>();
        _mockLogger = Substitute.For<ILogger<HealthCheckCoordinator>>();
    }

    [Fact]
    public void Constructor_InitializesWithCorrectDefaults()
    {
        // Arrange & Act
        var coordinator = CreateCoordinator();

        // Assert
        Assert.Equal(ApiHealthStatus.Checking, coordinator.CurrentStatus);
        Assert.Equal(10, coordinator.CheckIntervalSeconds);
    }

    [Fact]
    public void Start_SubscribesToTick()
    {
        // Arrange
        var coordinator = CreateCoordinator();

        // Act
        coordinator.Start();

        // Assert
        _mockTickService.Received(1).Tick += Arg.Any<EventHandler<ApplicationTickEventArgs>>();
    }

    [Fact]
    public async Task CheckNowAsync_HealthyStatus_UpdatesState()
    {
        // Arrange
        _mockHealthCheckService.CheckHealthAsync(Arg.Any<int>())
            .Returns(Task.FromResult(ApiHealthStatus.Healthy));
        var coordinator = CreateCoordinator();

        // Act
        await coordinator.CheckNowAsync();

        // Assert
        Assert.Equal(ApiHealthStatus.Healthy, coordinator.CurrentStatus);
        await _mockHealthCheckService.Received(1).CheckHealthAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task CheckNowAsync_UnhealthyStatus_UpdatesState()
    {
        // Arrange
        _mockHealthCheckService.CheckHealthAsync(Arg.Any<int>())
            .Returns(Task.FromResult(ApiHealthStatus.Unhealthy));
        _mockHealthCheckService.LastErrorMessage.Returns("连接超时");
        var coordinator = CreateCoordinator();

        // Act
        await coordinator.CheckNowAsync();

        // Assert
        Assert.Equal(ApiHealthStatus.Unhealthy, coordinator.CurrentStatus);
    }

    [Fact]
    public async Task CheckNowAsync_Exception_HandlesGracefully()
    {
        // Arrange
        _mockHealthCheckService.CheckHealthAsync(Arg.Any<int>())
            .Returns(Task.FromException<ApiHealthStatus>(new Exception("网络错误")));
        var coordinator = CreateCoordinator();

        // Act - 不应抛出异常
        await coordinator.CheckNowAsync();

        // Assert
        Assert.Equal(ApiHealthStatus.Unhealthy, coordinator.CurrentStatus);
    }

    [Fact]
    public void Stop_UnsubscribesFromTick()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        coordinator.Start();

        // Act
        coordinator.Stop();

        // Assert
        _mockTickService.Received(1).Tick -= Arg.Any<EventHandler<ApplicationTickEventArgs>>();
    }

    [Fact]
    public void Start_WhenAlreadyRunning_DoesNotDuplicateSubscription()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        coordinator.Start();

        // Act
        coordinator.Start(); // 第二次调用

        // Assert - 应该只订阅一次
        _mockTickService.Received(1).Tick += Arg.Any<EventHandler<ApplicationTickEventArgs>>();
    }

    [Fact]
    public async Task CheckNowAsync_WhenDisposed_DoesNothing()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        coordinator.Dispose();

        // Act
        await coordinator.CheckNowAsync();

        // Assert - 不应调用健康检查服务
        await _mockHealthCheckService.DidNotReceive().CheckHealthAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task StatusChanged_EventFired_OnStatusChange()
    {
        // Arrange
        _mockHealthCheckService.CheckHealthAsync(Arg.Any<int>())
            .Returns(Task.FromResult(ApiHealthStatus.Healthy));
        var coordinator = CreateCoordinator();
        var eventFired = false;
        coordinator.StatusChanged += (_, _) => eventFired = true;

        // Act
        await coordinator.CheckNowAsync(); // Checking -> Healthy

        // Assert
        Assert.True(eventFired);
    }

    private HealthCheckCoordinator CreateCoordinator()
    {
        return new HealthCheckCoordinator(
            _mockHealthCheckService,
            _mockTickService,
            _mockAppStateService,
            _mockLogger);
    }
}
