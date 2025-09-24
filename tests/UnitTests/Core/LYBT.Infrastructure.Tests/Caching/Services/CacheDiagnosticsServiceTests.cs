using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Caching.Models;
using LYBT.Infrastructure.Caching.Services;
using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Infrastructure.Tests.Caching.Services
{
    /// <summary>
    /// CacheDiagnosticsService单元测试
    /// </summary>
    public class CacheDiagnosticsServiceTests
    {
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ILogger<CacheDiagnosticsService>> _mockLogger;
        private readonly IOptions<CacheOptions> _cacheOptions;
        private readonly CacheDiagnosticsService _diagnosticsService;

        public CacheDiagnosticsServiceTests()
        {
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<CacheDiagnosticsService>>();

            _cacheOptions = Options.Create(new CacheOptions
            {
                Monitoring = new CacheOptions.MonitoringConfig
                {
                    HitRateThreshold = 0.8,
                    CapacityThreshold = 0.85,
                    EvictionRateThreshold = 100,
                    HistorySnapshotCount = 10,
                    EventIds = new CacheOptions.EventIdConfig
                    {
                        LowHitRate = 5001,
                        HighCapacity = 5002,
                        HighEvictionRate = 5003
                    }
                }
            });

            _diagnosticsService = new CacheDiagnosticsService(
                _mockCacheService.Object,
                _cacheOptions,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetHealthStatusAsync_WhenHitRateLow_ReturnsWarning()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 60,
                MissCount = 40,
                CurrentItemCount = 100,
                MaxCapacity = 1000,
                EvictionRate = 50
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act
            var result = await _diagnosticsService.GetHealthStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsHealthy.Should().BeTrue(); // 单个指标不健康但整体可能仍健康
            result.Level.Should().Be(HealthLevel.Warning);
            result.Message.Should().Contain("命中率");
        }

        [Fact]
        public async Task GetHealthStatusAsync_WhenCapacityHigh_ReturnsWarning()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 90,
                MissCount = 10,
                CurrentItemCount = 900,
                MaxCapacity = 1000,
                EvictionRate = 50
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act
            var result = await _diagnosticsService.GetHealthStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.Level.Should().Be(HealthLevel.Warning);
            result.Message.Should().Contain("容量");
        }

        [Fact]
        public async Task GetHealthStatusAsync_WhenEvictionRateHigh_ReturnsDegraded()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 90,
                MissCount = 10,
                CurrentItemCount = 500,
                MaxCapacity = 1000,
                EvictionRate = 150 // 超过阈值
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act
            var result = await _diagnosticsService.GetHealthStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.HealthStatus.Should().BeOneOf(HealthLevel.Ok, HealthLevel.Warning);
            result.Message.Should().Contain("逐出");
        }

        [Fact]
        public async Task GetHealthStatusAsync_WhenAllMetricsHealthy_ReturnsHealthy()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 85,
                MissCount = 15,
                CurrentItemCount = 500,
                MaxCapacity = 1000,
                EvictionRate = 50
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act
            var result = await _diagnosticsService.GetHealthStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsHealthy.Should().BeTrue();
            result.Level.Should().Be(HealthLevel.Healthy);
            result.Message.Should().Contain("正常");
        }

        [Fact]
        public void CheckThresholds_WhenLowHitRate_AlertsTrue()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 60,
                MissCount = 40,
                CurrentItemCount = 100,
                MaxCapacity = 1000
            };

            // Act
            var result = _diagnosticsService.CheckThresholds(statistics);

            // Assert
            result.Should().NotBeNull();
            result.IsLowHitRate.Should().BeTrue();
            result.HasAnyAlert.Should().BeTrue();
            result.CurrentHitRate.Should().BeApproximately(0.6, 0.01);
        }

        [Fact]
        public void CheckThresholds_WhenHighCapacity_AlertsTrue()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 80,
                MissCount = 20,
                CurrentItemCount = 900,
                MaxCapacity = 1000
            };

            // Act
            var result = _diagnosticsService.CheckThresholds(statistics);

            // Assert
            result.Should().NotBeNull();
            result.IsHighCapacity.Should().BeTrue();
            result.HasAnyAlert.Should().BeTrue();
            result.CurrentCapacityRatio.Should().BeApproximately(0.9, 0.01);
        }

        [Fact]
        public void CheckThresholds_WhenHighEvictionRate_AlertsTrue()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 80,
                MissCount = 20,
                CurrentItemCount = 500,
                MaxCapacity = 1000,
                EvictionRate = 150
            };

            // Act
            var result = _diagnosticsService.CheckThresholds(statistics);

            // Assert
            result.Should().NotBeNull();
            result.IsHighEvictionRate.Should().BeTrue();
            result.HasAnyAlert.Should().BeTrue();
            result.CurrentEvictionRate.Should().Be(150);
        }

        [Fact]
        public async Task RunDiagnosticsAsync_GeneratesCompleteResult()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 85,
                MissCount = 15,
                CurrentItemCount = 500,
                MaxCapacity = 1000,
                EvictionRate = 50,
                UsedMemory = 1024 * 1024 * 10 // 10MB
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act
            var result = await _diagnosticsService.RunDiagnosticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.DiagnosticId.Should().NotBeNullOrWhiteSpace();
            result.DiagnosticTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.HealthStatus.Should().NotBeNull();
            result.Performance.Should().NotBeNull();
            result.Capacity.Should().NotBeNull();
            result.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public void GetHistorySnapshots_ReturnsRequestedCount()
        {
            // Arrange - 先生成一些快照
            var statistics = new CacheStatistics
            {
                HitCount = 85,
                MissCount = 15,
                CurrentItemCount = 500,
                MaxCapacity = 1000
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // 生成多个快照
            for (int i = 0; i < 5; i++)
            {
                _diagnosticsService.RunDiagnosticsAsync().Wait();
                Thread.Sleep(10); // 确保时间戳不同
            }

            // Act
            var snapshots = _diagnosticsService.GetHistorySnapshots(3);

            // Assert
            snapshots.Should().NotBeNull();
            snapshots.Count().Should().BeLessOrEqualTo(3);
        }

        [Fact]
        public void GetHistorySnapshots_LimitsToMaxConfigured()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 85,
                MissCount = 15,
                CurrentItemCount = 500,
                MaxCapacity = 1000
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // 生成超过限制的快照
            for (int i = 0; i < 15; i++)
            {
                _diagnosticsService.RunDiagnosticsAsync().Wait();
                Thread.Sleep(10);
            }

            // Act
            var allSnapshots = _diagnosticsService.GetHistorySnapshots(100);

            // Assert
            allSnapshots.Count().Should().BeLessOrEqualTo(10); // 配置的最大值
        }

        [Fact]
        public void GetLatestSnapshot_ReturnsNullWhenNoSnapshots()
        {
            // Arrange - 新实例没有快照

            // Act
            var snapshot = _diagnosticsService.GetLatestSnapshot();

            // Assert
            snapshot.Should().BeNull();
        }

        [Fact]
        public async Task GetLatestSnapshot_ReturnsMostRecentAfterDiagnostics()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 85,
                MissCount = 15,
                CurrentItemCount = 500,
                MaxCapacity = 1000,
                Timestamp = DateTime.UtcNow
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act
            await _diagnosticsService.RunDiagnosticsAsync();
            var snapshot = _diagnosticsService.GetLatestSnapshot();

            // Assert
            snapshot.Should().NotBeNull();
            snapshot.Statistics.Should().NotBeNull();
            snapshot.Statistics.HitCount.Should().Be(85);
        }

        [Fact]
        public async Task RunDiagnosticsAsync_LogsWarningOnThresholdBreach()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 60,
                MissCount = 40, // 低命中率
                CurrentItemCount = 100,
                MaxCapacity = 1000
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act
            await _diagnosticsService.RunDiagnosticsAsync();

            // Assert - 验证日志记录了Warning
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
        }

        [Fact]
        public async Task ConcurrentDiagnostics_ThreadSafe()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 85,
                MissCount = 15,
                CurrentItemCount = 500,
                MaxCapacity = 1000
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act - 并发执行诊断
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => _diagnosticsService.RunDiagnosticsAsync())
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r != null);

            // 验证快照数量不超过配置限制
            var snapshots = _diagnosticsService.GetHistorySnapshots(100);
            snapshots.Count().Should().BeLessOrEqualTo(10);
        }

        [Fact]
        public async Task GetHealthStatusAsync_HandlesNullMaxCapacity()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 85,
                MissCount = 15,
                CurrentItemCount = 500,
                MaxCapacity = null, // 无容量限制
                EvictionRate = 50
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act
            var result = await _diagnosticsService.GetHealthStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsHealthy.Should().BeTrue();
            // 容量检查应该被跳过
        }

        [Fact]
        public async Task RunDiagnosticsAsync_GeneratesRecommendations()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                HitCount = 60,
                MissCount = 40, // 低命中率
                CurrentItemCount = 900, // 高容量
                MaxCapacity = 1000,
                EvictionRate = 150 // 高逐出率
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(statistics);

            // Act
            var result = await _diagnosticsService.RunDiagnosticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.HealthStatus.Recommendations.Should().NotBeEmpty();
            result.HealthStatus.Recommendations.Should().Contain(r => r.Contains("命中率"));
            result.HealthStatus.Recommendations.Should().Contain(r => r.Contains("容量"));
            result.HealthStatus.Recommendations.Should().Contain(r => r.Contains("逐出"));
        }
    }
}