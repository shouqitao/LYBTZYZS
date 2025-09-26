using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using LYBT.Infrastructure.Security;

namespace Infrastructure.UnitTests.Security;

public class KeyRotationBackgroundServiceTests
{
    private readonly Mock<IKeyManagementServiceFactory> _mockFactory;
    private readonly Mock<IKeyManagementService> _mockKeyManagementService;
    private readonly Mock<ILogger<KeyRotationBackgroundService>> _mockLogger;
    private readonly KeyRotationBackgroundService _service;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public KeyRotationBackgroundServiceTests()
    {
        _mockFactory = new Mock<IKeyManagementServiceFactory>();
        _mockKeyManagementService = new Mock<IKeyManagementService>();
        _mockLogger = new Mock<ILogger<KeyRotationBackgroundService>>();
        _cancellationTokenSource = new CancellationTokenSource();

        _mockFactory.Setup(x => x.CreateKeyManagementService())
            .Returns(_mockKeyManagementService.Object);

        _service = new KeyRotationBackgroundService(_mockFactory.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => new KeyRotationBackgroundService(_mockFactory.Object, _mockLogger.Object);
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new KeyRotationBackgroundService(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("keyManagementServiceFactory");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new KeyRotationBackgroundService(_mockFactory.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteAsync_WhenKeyRotationNotNeeded_ShouldNotRotateKey()
    {
        // Arrange
        _mockKeyManagementService.Setup(x => x.ShouldRotateKeyAsync())
            .ReturnsAsync(false);

        // 设置短暂的取消令牌，避免无限循环
        _cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var executeTask = _service.StartAsync(_cancellationTokenSource.Token);
        await Task.Delay(50); // 等待一个检查周期
        _cancellationTokenSource.Cancel();

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // 预期的取消异常，忽略
        }

        // Assert
        _mockFactory.Verify(x => x.CreateKeyManagementService(), Times.AtLeastOnce);
        _mockKeyManagementService.Verify(x => x.ShouldRotateKeyAsync(), Times.AtLeastOnce);
        _mockKeyManagementService.Verify(x => x.RotateJwtSecretAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenKeyRotationNeeded_ShouldRotateKey()
    {
        // Arrange
        var rotationCallCount = 0;
        _mockKeyManagementService.Setup(x => x.ShouldRotateKeyAsync())
            .ReturnsAsync(() => rotationCallCount == 0); // 第一次需要轮换，之后不需要

        _mockKeyManagementService.Setup(x => x.RotateJwtSecretAsync())
            .ReturnsAsync("new-rotated-secret")
            .Callback(() => rotationCallCount++);

        // 设置短暂的取消令牌
        _cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(150));

        // Act
        var executeTask = _service.StartAsync(_cancellationTokenSource.Token);
        await Task.Delay(100); // 等待执行
        _cancellationTokenSource.Cancel();

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // 预期的取消异常，忽略
        }

        // Assert
        _mockKeyManagementService.Verify(x => x.ShouldRotateKeyAsync(), Times.AtLeastOnce);
        _mockKeyManagementService.Verify(x => x.RotateJwtSecretAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenKeyManagementServiceThrows_ShouldLogErrorAndContinue()
    {
        // Arrange
        _mockKeyManagementService.Setup(x => x.ShouldRotateKeyAsync())
            .ThrowsAsync(new InvalidOperationException("测试异常"));

        // 设置短暂的取消令牌
        _cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var executeTask = _service.StartAsync(_cancellationTokenSource.Token);
        await Task.Delay(50);
        _cancellationTokenSource.Cancel();

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // 预期的取消异常，忽略
        }

        // Assert
        _mockKeyManagementService.Verify(x => x.ShouldRotateKeyAsync(), Times.AtLeastOnce);
        
        // 验证错误日志被记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("密钥轮换检查时发生错误")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFactoryReturnsNull_ShouldLogErrorAndContinue()
    {
        // Arrange
        _mockFactory.Setup(x => x.CreateKeyManagementService())
            .Returns((IKeyManagementService)null!);

        // 设置短暂的取消令牌
        _cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var executeTask = _service.StartAsync(_cancellationTokenSource.Token);
        await Task.Delay(50);
        _cancellationTokenSource.Cancel();

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // 预期的取消异常，忽略
        }

        // Assert
        _mockFactory.Verify(x => x.CreateKeyManagementService(), Times.AtLeastOnce);
        
        // 验证错误日志被记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("无法创建密钥管理服务实例")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccessfulRotation_ShouldLogSuccessMessage()
    {
        // Arrange
        _mockKeyManagementService.Setup(x => x.ShouldRotateKeyAsync())
            .ReturnsAsync(true);
        _mockKeyManagementService.Setup(x => x.RotateJwtSecretAsync())
            .ReturnsAsync("new-secret-key");

        // 设置短暂的取消令牌
        _cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var executeTask = _service.StartAsync(_cancellationTokenSource.Token);
        await Task.Delay(50);
        _cancellationTokenSource.Cancel();

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // 预期的取消异常，忽略
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JWT密钥轮换成功完成")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Service_ShouldImplementBackgroundService()
    {
        // Assert
        _service.Should().BeAssignableTo<BackgroundService>();
        _service.Should().BeAssignableTo<IHostedService>();
    }
}