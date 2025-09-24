using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Caching.Models;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.WebAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.WebAPI.Tests.Services
{
    /// <summary>
    /// CacheHealthBackgroundService单元测试
    /// </summary>
    public class CacheHealthBackgroundServiceTests
    {
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ICacheDiagnosticsService> _mockDiagnosticsService;
        private readonly Mock<ILogger<CacheHealthBackgroundService>> _mockLogger;
        private readonly Mock<IHostApplicationLifetime> _mockLifetime;
        private readonly IOptions<CacheOptions> _cacheOptions;

        public CacheHealthBackgroundServiceTests()
        {
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockDiagnosticsService = new Mock<ICacheDiagnosticsService>();
            _mockLogger = new Mock<ILogger<CacheHealthBackgroundService>>();
            _mockLifetime = new Mock<IHostApplicationLifetime>();

            // 设置依赖注入链
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(ICacheDiagnosticsService)))
                .Returns(_mockDiagnosticsService.Object);

            _cacheOptions = Options.Create(new CacheOptions
            {
                Monitoring = new CacheOptions.MonitoringConfig
                {
                    Enabled = true,
                    SamplingIntervalSeconds = 1, // 设置短间隔便于测试
                    HitRateThreshold = 0.8,
                    CapacityThreshold = 0.85,
                    EvictionRateThreshold = 100,
                    EventIds = new CacheOptions.EventIdConfig
                    {
                        LowHitRate = 5001,
                        HighCapacity = 5002,
                        HighEvictionRate = 5003
                    }
                }
            });
        }

        [Fact]
        public async Task StartAsync_WhenMonitoringEnabled_StartsTimer()
        {
            // Arrange
            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                _cacheOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            // Act
            await service.StartAsync(CancellationToken.None);

            // Assert - 等待一段时间后验证诊断服务被调用
            await Task.Delay(1500); // 等待超过采样间隔

            _mockDiagnosticsService.Verify(
                x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce
            );

            // Cleanup
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StartAsync_WhenMonitoringDisabled_DoesNotStartTimer()
        {
            // Arrange
            var disabledOptions = Options.Create(new CacheOptions
            {
                Monitoring = new CacheOptions.MonitoringConfig
                {
                    Enabled = false
                }
            });

            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                disabledOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(500);

            // Assert - 诊断服务不应被调用
            _mockDiagnosticsService.Verify(
                x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );

            // Cleanup
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task DoWork_WhenLowHitRate_LogsWarning()
        {
            // Arrange
            var healthStatus = new CacheHealthStatus
            {
                IsHealthy = false,
                Level = HealthLevel.Warning,
                Message = "缓存命中率过低"
            };

            var snapshot = new CacheHealthSnapshot
            {
                SnapshotId = Guid.NewGuid().ToString(),
                SnapshotTime = DateTime.UtcNow,
                HealthLevel = HealthLevel.Warning,
                Statistics = new CacheStatistics
                {
                    HitCount = 60,
                    MissCount = 40
                },
                ThresholdCheck = new ThresholdCheckResult
                {
                    IsLowHitRate = true,
                    CurrentHitRate = 0.6,
                    HitRateThreshold = 0.8,
                    HasAnyAlert = true
                }
            };

            _mockDiagnosticsService.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthStatus);
            _mockDiagnosticsService.Setup(x => x.GetLatestSnapshot())
                .Returns(snapshot);

            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                _cacheOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(1500);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.Is<EventId>(e => e.Id == 5001), // LowHitRate EventId
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );

            // Cleanup
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task DoWork_WhenHighCapacity_LogsWarning()
        {
            // Arrange
            var healthStatus = new CacheHealthStatus
            {
                IsHealthy = false,
                Level = HealthLevel.Warning,
                Message = "缓存容量接近上限"
            };

            var snapshot = new CacheHealthSnapshot
            {
                SnapshotId = Guid.NewGuid().ToString(),
                SnapshotTime = DateTime.UtcNow,
                HealthLevel = HealthLevel.Warning,
                Statistics = new CacheStatistics
                {
                    CurrentItemCount = 900,
                    MaxCapacity = 1000
                },
                ThresholdCheck = new ThresholdCheckResult
                {
                    IsHighCapacity = true,
                    CurrentCapacityRatio = 0.9,
                    CapacityThreshold = 0.85,
                    HasAnyAlert = true
                }
            };

            _mockDiagnosticsService.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthStatus);
            _mockDiagnosticsService.Setup(x => x.GetLatestSnapshot())
                .Returns(snapshot);

            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                _cacheOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(1500);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.Is<EventId>(e => e.Id == 5002), // HighCapacity EventId
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );

            // Cleanup
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task DoWork_WhenHighEvictionRate_LogsWarning()
        {
            // Arrange
            var healthStatus = new CacheHealthStatus
            {
                IsHealthy = false,
                Level = HealthLevel.Degraded,
                Message = "缓存逐出率过高"
            };

            var snapshot = new CacheHealthSnapshot
            {
                SnapshotId = Guid.NewGuid().ToString(),
                SnapshotTime = DateTime.UtcNow,
                HealthLevel = HealthLevel.Degraded,
                Statistics = new CacheStatistics
                {
                    EvictionRate = 150
                },
                ThresholdCheck = new ThresholdCheckResult
                {
                    IsHighEvictionRate = true,
                    CurrentEvictionRate = 150,
                    EvictionRateThreshold = 100,
                    HasAnyAlert = true
                }
            };

            _mockDiagnosticsService.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthStatus);
            _mockDiagnosticsService.Setup(x => x.GetLatestSnapshot())
                .Returns(snapshot);

            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                _cacheOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(1500);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.Is<EventId>(e => e.Id == 5003), // HighEvictionRate EventId
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );

            // Cleanup
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task DoWork_WhenHealthy_LogsDebug()
        {
            // Arrange
            var healthStatus = new CacheHealthStatus
            {
                IsHealthy = true,
                Level = HealthLevel.Healthy,
                Message = "缓存运行正常"
            };

            var snapshot = new CacheHealthSnapshot
            {
                SnapshotId = Guid.NewGuid().ToString(),
                SnapshotTime = DateTime.UtcNow,
                HealthLevel = HealthLevel.Healthy,
                Statistics = new CacheStatistics
                {
                    HitCount = 85,
                    MissCount = 15,
                    CurrentItemCount = 500,
                    MaxCapacity = 1000,
                    EvictionRate = 50
                },
                ThresholdCheck = new ThresholdCheckResult
                {
                    HasAnyAlert = false
                }
            };

            _mockDiagnosticsService.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthStatus);
            _mockDiagnosticsService.Setup(x => x.GetLatestSnapshot())
                .Returns(snapshot);

            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                _cacheOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(1500);

            // Assert - 健康状态只记录Debug级别
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );

            // Cleanup
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task DoWork_WhenExceptionThrown_LogsErrorAndContinues()
        {
            // Arrange
            _mockDiagnosticsService.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("模拟错误"));

            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                _cacheOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(2500); // 等待多个采样周期

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );

            // 验证服务继续运行（多次调用）
            _mockDiagnosticsService.Verify(
                x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()),
                Times.AtLeast(2)
            );

            // Cleanup
            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StopAsync_StopsTimerGracefully()
        {
            // Arrange
            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                _cacheOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(500);
            await service.StopAsync(CancellationToken.None);

            // 停止后等待
            await Task.Delay(1500);

            // Assert - 停止后不应再调用诊断服务
            var initialCalls = _mockDiagnosticsService.Invocations.Count;
            await Task.Delay(1500);
            var afterStopCalls = _mockDiagnosticsService.Invocations.Count;

            afterStopCalls.Should().Be(initialCalls);
        }

        [Fact]
        public async Task ExecuteAsync_RespectsStoppingToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                _cacheOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            // Act
            await service.StartAsync(cts.Token);
            await Task.Delay(500);
            cts.Cancel(); // 取消token
            await service.StopAsync(CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("停止")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        [Fact]
        public async Task DoWork_UsesCorrectSamplingInterval()
        {
            // Arrange
            var customOptions = Options.Create(new CacheOptions
            {
                Monitoring = new CacheOptions.MonitoringConfig
                {
                    Enabled = true,
                    SamplingIntervalSeconds = 2 // 2秒间隔
                }
            });

            var service = new CacheHealthBackgroundService(
                _mockScopeFactory.Object,
                customOptions,
                _mockLogger.Object,
                _mockLifetime.Object
            );

            _mockDiagnosticsService.Setup(x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CacheHealthStatus { IsHealthy = true, Level = HealthLevel.Healthy });

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(5000); // 等待5秒

            // Assert - 应该调用2-3次（5秒 / 2秒间隔）
            _mockDiagnosticsService.Verify(
                x => x.GetHealthStatusAsync(It.IsAny<CancellationToken>()),
                Times.Between(2, 3, Moq.Range.Inclusive)
            );

            // Cleanup
            await service.StopAsync(CancellationToken.None);
        }
    }
}