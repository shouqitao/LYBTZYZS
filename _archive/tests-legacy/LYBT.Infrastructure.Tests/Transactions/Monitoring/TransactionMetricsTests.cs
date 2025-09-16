using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Transactions;
using LYBT.Infrastructure.Transactions.Monitoring;
using LYBT.Entities.Common;

namespace LYBT.Infrastructure.Tests.Transactions.Monitoring
{
    /// <summary>
    /// 事务指标监控系统单元测试
    /// 测试实时指标收集、统计分析、慢事务检测等功能
    /// </summary>
    public class TransactionMetricsTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<TransactionMetrics>> _loggerMock;
        private readonly TransactionMetrics _transactionMetrics;

        public TransactionMetricsTests()
        {
            // 创建内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<TransactionMetrics>>();

            _transactionMetrics = new TransactionMetrics(_loggerMock.Object, _dbContext, _memoryCache);
        }

        [Fact]
        public async Task RecordTransactionStartAsync_WithValidData_ShouldTrackActiveTransaction()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionName = "TestTransaction";
            var userId = Guid.NewGuid();

            // Act
            await _transactionMetrics.RecordTransactionStartAsync(transactionId, transactionName, userId);

            // Assert
            var snapshot = await _transactionMetrics.GetCurrentMetricsAsync();
            snapshot.ActiveTransactionCount.Should().Be(1);
        }

        [Fact]
        public async Task RecordTransactionCompleteAsync_WithSuccessfulTransaction_ShouldUpdateStatistics()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionName = "TestTransaction";
            var durationMs = 2500L;
            var stepCount = 3;

            // 先记录事务开始
            await _transactionMetrics.RecordTransactionStartAsync(transactionId, transactionName);

            // Act
            await _transactionMetrics.RecordTransactionCompleteAsync(transactionId, TransactionStatus.Completed, durationMs, stepCount);

            // Assert
            var snapshot = await _transactionMetrics.GetCurrentMetricsAsync();
            snapshot.ActiveTransactionCount.Should().Be(0); // 活跃事务应该减少
            snapshot.TotalCompletedToday.Should().Be(1);
            snapshot.TotalFailedToday.Should().Be(0);
            snapshot.AverageExecutionTimeMs.Should().Be(durationMs);
            snapshot.SlowestExecutionTimeMs.Should().Be(durationMs);
            snapshot.SuccessRate.Should().Be(100.0);
        }

        [Fact]
        public async Task RecordTransactionCompleteAsync_WithFailedTransaction_ShouldUpdateFailureStatistics()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionName = "FailedTransaction";
            var durationMs = 1500L;
            var stepCount = 2;

            await _transactionMetrics.RecordTransactionStartAsync(transactionId, transactionName);

            // Act
            await _transactionMetrics.RecordTransactionCompleteAsync(transactionId, TransactionStatus.Failed, durationMs, stepCount);

            // Assert
            var snapshot = await _transactionMetrics.GetCurrentMetricsAsync();
            snapshot.TotalCompletedToday.Should().Be(0);
            snapshot.TotalFailedToday.Should().Be(1);
            snapshot.SuccessRate.Should().Be(0.0);
        }

        [Fact]
        public async Task RecordStepExecutionAsync_WithSlowStep_ShouldCacheSlowStepInfo()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var stepName = "SlowStep";
            var stepOrder = 1;
            var durationMs = 3000L; // 3秒，超过慢步骤阈值

            await _transactionMetrics.RecordTransactionStartAsync(transactionId, "TestTransaction");

            // Act
            await _transactionMetrics.RecordStepExecutionAsync(transactionId, stepName, stepOrder, durationMs, TransactionStepStatus.Success);

            // Assert
            var cacheKey = $"slow_step_{transactionId}_{stepOrder}";
            var slowStepInfo = _memoryCache.Get<SlowStepInfo>(cacheKey);

            slowStepInfo.Should().NotBeNull();
            slowStepInfo!.StepName.Should().Be(stepName);
            slowStepInfo.DurationMs.Should().Be(durationMs);
            slowStepInfo.Order.Should().Be(stepOrder);
            slowStepInfo.Reason.Should().Be("很慢步骤");
        }

        [Fact]
        public async Task RecordCompensationAsync_WithValidData_ShouldUpdateCompensationStatistics()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionName = "CompensationTransaction";
            var compensatedSteps = 2;
            var totalCompensationTimeMs = 500L;

            await _transactionMetrics.RecordTransactionStartAsync(transactionId, transactionName);

            // Act
            await _transactionMetrics.RecordCompensationAsync(transactionId, compensatedSteps, totalCompensationTimeMs);

            // Assert - 验证补偿统计被正确记录（通过内存状态）
            // 这里主要验证方法执行不抛出异常，具体统计验证需要通过其他指标接口
            // 可以通过日志验证调用
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Compensation metrics recorded")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetCurrentMetricsAsync_WithMultipleTransactions_ShouldProvideAccurateSnapshot()
        {
            // Arrange
            var transactions = new[]
            {
                (Guid.NewGuid(), "Transaction1", 1000L, TransactionStatus.Completed),
                (Guid.NewGuid(), "Transaction2", 2000L, TransactionStatus.Completed),
                (Guid.NewGuid(), "Transaction3", 1500L, TransactionStatus.Failed)
            };

            foreach (var (transactionId, transactionName, durationMs, status) in transactions)
            {
                await _transactionMetrics.RecordTransactionStartAsync(transactionId, transactionName);
                await _transactionMetrics.RecordTransactionCompleteAsync(transactionId, status, durationMs, 2);
            }

            // Act
            var snapshot = await _transactionMetrics.GetCurrentMetricsAsync();

            // Assert
            snapshot.Should().NotBeNull();
            snapshot.ActiveTransactionCount.Should().Be(0);
            snapshot.TotalCompletedToday.Should().Be(2);
            snapshot.TotalFailedToday.Should().Be(1);
            snapshot.AverageExecutionTimeMs.Should().BeApproximately(1500.0, 0.1); // (1000+2000+1500)/3
            snapshot.SlowestExecutionTimeMs.Should().Be(2000);
            snapshot.SuccessRate.Should().BeApproximately(66.67, 0.1); // 2/3 * 100
        }

        [Fact]
        public async Task GetMetricsStatisticsAsync_WithDateRange_ShouldReturnPeriodStatistics()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddHours(-2);
            var endTime = DateTime.UtcNow;

            // 在数据库中创建测试事务日志
            var transactionLogs = new[]
            {
                new TransactionLog
                {
                    Id = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    TransactionName = "Transaction1",
                    Status = (int)TransactionStatus.Completed,
                    StartTime = startTime.AddMinutes(30),
                    DurationMs = 1500,
                    CreatedAt = DateTime.UtcNow
                },
                new TransactionLog
                {
                    Id = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    TransactionName = "Transaction2",
                    Status = (int)TransactionStatus.Failed,
                    StartTime = startTime.AddMinutes(60),
                    DurationMs = 2500,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.TransactionLogs.AddRange(transactionLogs);
            await _dbContext.SaveChangesAsync();

            // Act
            var statistics = await _transactionMetrics.GetMetricsStatisticsAsync(startTime, endTime);

            // Assert
            statistics.Should().NotBeNull();
            statistics.TotalTransactions.Should().Be(2);
            statistics.SuccessfulTransactions.Should().Be(1);
            statistics.FailedTransactions.Should().Be(1);
            statistics.CompensatedTransactions.Should().Be(0);
            statistics.AverageExecutionTimeMs.Should().Be(2000); // (1500+2500)/2
            statistics.MaxExecutionTimeMs.Should().Be(2500);
            
            statistics.TransactionTypeMetrics.Should().ContainKey("Transaction1");
            statistics.TransactionTypeMetrics.Should().ContainKey("Transaction2");
        }

        [Fact]
        public async Task GetSlowTransactionAlertsAsync_WithSlowTransactions_ShouldReturnAlertsWithSteps()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionName = "SlowTransaction";
            var thresholdMs = 3000L;
            var durationMs = 6000L; // 6秒，超过阈值

            // 记录事务和慢步骤
            await _transactionMetrics.RecordTransactionStartAsync(transactionId, transactionName);
            await _transactionMetrics.RecordStepExecutionAsync(transactionId, "SlowStep1", 1, 3000, TransactionStepStatus.Success);
            await _transactionMetrics.RecordStepExecutionAsync(transactionId, "SlowStep2", 2, 2500, TransactionStepStatus.Success);
            await _transactionMetrics.RecordTransactionCompleteAsync(transactionId, TransactionStatus.Completed, durationMs, 2);

            // Act
            var alerts = await _transactionMetrics.GetSlowTransactionAlertsAsync(thresholdMs);

            // Assert
            alerts.Should().NotBeNull();
            alerts.Should().HaveCount(1);
            
            var alert = alerts[0];
            alert.TransactionId.Should().Be(transactionId);
            alert.TransactionName.Should().Be(transactionName);
            alert.DurationMs.Should().Be(durationMs);
            alert.Status.Should().Be(TransactionStatus.Completed);
            alert.SlowReason.Should().Be("极慢事务 (>10s)"); // 6秒应该归类为极慢事务
            alert.SlowSteps.Should().HaveCount(2);
            alert.SlowSteps.Should().Contain(s => s.StepName == "SlowStep1" && s.DurationMs == 3000);
            alert.SlowSteps.Should().Contain(s => s.StepName == "SlowStep2" && s.DurationMs == 2500);
        }

        [Fact]
        public async Task GetErrorStatisticsAsync_WithErrorTransactions_ShouldReturnErrorAnalysis()
        {
            // Arrange
            var hoursBack = 2;
            var startTime = DateTime.UtcNow.AddHours(-hoursBack);

            // 在数据库中创建错误事务日志
            var errorTransactionLogs = new[]
            {
                new TransactionLog
                {
                    Id = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    TransactionName = "ErrorTransaction1",
                    Status = (int)TransactionStatus.Failed,
                    StartTime = startTime.AddMinutes(30),
                    Exception = "{\"Type\":\"InvalidOperationException\",\"Message\":\"数据验证失败\"}",
                    CreatedAt = DateTime.UtcNow
                },
                new TransactionLog
                {
                    Id = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    TransactionName = "ErrorTransaction2",
                    Status = (int)TransactionStatus.Failed,
                    StartTime = startTime.AddMinutes(60),
                    Exception = "{\"Type\":\"ArgumentException\",\"Message\":\"参数无效\"}",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _dbContext.TransactionLogs.AddRange(errorTransactionLogs);
            await _dbContext.SaveChangesAsync();

            // Act
            var errorStats = await _transactionMetrics.GetErrorStatisticsAsync(hoursBack);

            // Assert
            errorStats.Should().NotBeNull();
            errorStats.TotalErrors.Should().Be(2);
            errorStats.ErrorsByType.Should().ContainKey("InvalidOperationException");
            errorStats.ErrorsByType.Should().ContainKey("ArgumentException");
            errorStats.ErrorsByTransactionType.Should().ContainKey("ErrorTransaction1");
            errorStats.ErrorsByTransactionType.Should().ContainKey("ErrorTransaction2");
            errorStats.TopErrors.Should().HaveCountGreaterThan(0);
            errorStats.ErrorTrends.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ResetMetricsAsync_ShouldClearAllMetrics()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            await _transactionMetrics.RecordTransactionStartAsync(transactionId, "TestTransaction");
            await _transactionMetrics.RecordTransactionCompleteAsync(transactionId, TransactionStatus.Completed, 1000, 1);

            // 验证指标存在
            var snapshotBefore = await _transactionMetrics.GetCurrentMetricsAsync();
            snapshotBefore.TotalCompletedToday.Should().Be(1);

            // Act
            await _transactionMetrics.ResetMetricsAsync();

            // Assert
            var snapshotAfter = await _transactionMetrics.GetCurrentMetricsAsync();
            snapshotAfter.ActiveTransactionCount.Should().Be(0);
            snapshotAfter.TotalCompletedToday.Should().Be(0);
            snapshotAfter.TotalFailedToday.Should().Be(0);
            snapshotAfter.AverageExecutionTimeMs.Should().Be(0);
            snapshotAfter.SlowestExecutionTimeMs.Should().Be(0);
        }

        [Theory]
        [InlineData(TransactionStepStatus.Success, "成功")]
        [InlineData(TransactionStepStatus.Failed, "失败")]
        [InlineData(TransactionStepStatus.Compensated, "已补偿")]
        [InlineData(TransactionStepStatus.Skipped, "跳过")]
        public async Task RecordStepExecutionAsync_WithDifferentStatuses_ShouldHandleAllStatuses(
            TransactionStepStatus status, string description)
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var stepName = $"TestStep_{description}";
            var stepOrder = 1;
            var durationMs = 1000L;

            await _transactionMetrics.RecordTransactionStartAsync(transactionId, "TestTransaction");

            // Act & Assert
            var act = () => _transactionMetrics.RecordStepExecutionAsync(transactionId, stepName, stepOrder, durationMs, status);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GetCurrentMetricsAsync_ShouldIncludeSystemUptime()
        {
            // Act
            var snapshot = await _transactionMetrics.GetCurrentMetricsAsync();

            // Assert
            snapshot.Should().NotBeNull();
            snapshot.SystemStartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            snapshot.SnapshotTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task RecordTransactionStartAsync_WithMultipleConcurrentTransactions_ShouldHandleConcurrency()
        {
            // Arrange
            var transactionCount = 10;
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < transactionCount; i++)
            {
                var transactionId = Guid.NewGuid();
                var task = _transactionMetrics.RecordTransactionStartAsync(transactionId, $"ConcurrentTransaction_{i}");
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Assert
            var snapshot = await _transactionMetrics.GetCurrentMetricsAsync();
            snapshot.ActiveTransactionCount.Should().Be(transactionCount);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
            _memoryCache?.Dispose();
            _transactionMetrics?.Dispose();
        }
    }
}