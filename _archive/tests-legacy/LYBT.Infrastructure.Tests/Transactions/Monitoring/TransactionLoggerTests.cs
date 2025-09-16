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
    /// 事务日志系统单元测试
    /// 测试事务日志记录、查询、统计等功能
    /// </summary>
    public class TransactionLoggerTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<TransactionLogger>> _loggerMock;
        private readonly TransactionLogger _transactionLogger;

        public TransactionLoggerTests()
        {
            // 创建内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<TransactionLogger>>();

            _transactionLogger = new TransactionLogger(_dbContext, _memoryCache, _loggerMock.Object);
        }

        [Fact]
        public async Task LogTransactionStartAsync_WithValidData_ShouldCreateTransactionLog()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionName = "TestTransaction";
            var description = "测试事务日志开始";
            var userId = Guid.NewGuid();

            // Act
            await _transactionLogger.LogTransactionStartAsync(transactionId, transactionName, description, userId);

            // Assert
            var logEntry = await _dbContext.TransactionLogs
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            logEntry.Should().NotBeNull();
            logEntry!.TransactionId.Should().Be(transactionId);
            logEntry.TransactionName.Should().Be(transactionName);
            logEntry.Status.Should().Be((int)TransactionStatus.InProgress);
            logEntry.UserId.Should().Be(userId);
            logEntry.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            logEntry.EndTime.Should().BeNull();
        }

        [Fact]
        public async Task LogTransactionEndAsync_WithExistingTransaction_ShouldUpdateTransactionLog()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionName = "TestTransaction";
            var description = "测试事务";
            var userId = Guid.NewGuid();

            // 先创建开始日志
            await _transactionLogger.LogTransactionStartAsync(transactionId, transactionName, description, userId);

            var status = TransactionStatus.Completed;
            var duration = TimeSpan.FromSeconds(5);
            var message = "事务执行成功";

            // Act
            await _transactionLogger.LogTransactionEndAsync(transactionId, status, duration, message);

            // Assert
            var logEntry = await _dbContext.TransactionLogs
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            logEntry.Should().NotBeNull();
            logEntry!.Status.Should().Be((int)status);
            logEntry.DurationMs.Should().Be((long)duration.TotalMilliseconds);
            logEntry.EndTime.Should().NotBeNull();
            logEntry.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task LogStepStartAsync_WithValidData_ShouldCacheStepHistory()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var stepName = "TestStep";
            var stepOrder = 1;

            // Act
            await _transactionLogger.LogStepStartAsync(transactionId, stepName, stepOrder);

            // Assert
            // 步骤历史应该被缓存，而不是直接写入数据库
            var cacheKey = $"step_history_{transactionId}";
            var stepHistory = _memoryCache.Get<List<TransactionStepHistory>>(cacheKey);
            
            stepHistory.Should().NotBeNull();
            stepHistory.Should().HaveCount(1);
            stepHistory![0].StepName.Should().Be(stepName);
            stepHistory[0].Order.Should().Be(stepOrder);
            stepHistory[0].Status.Should().Be(TransactionStepStatus.InProgress);
        }

        [Fact]
        public async Task LogStepEndAsync_WithExistingStep_ShouldUpdateStepHistory()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var stepName = "TestStep";
            var stepOrder = 1;

            // 先记录步骤开始
            await _transactionLogger.LogStepStartAsync(transactionId, stepName, stepOrder);

            var status = TransactionStepStatus.Completed;
            var duration = TimeSpan.FromSeconds(2);
            var message = "步骤执行成功";

            // Act
            await _transactionLogger.LogStepEndAsync(transactionId, stepName, status, duration, message);

            // Assert
            var cacheKey = $"step_history_{transactionId}";
            var stepHistory = _memoryCache.Get<List<TransactionStepHistory>>(cacheKey);
            
            stepHistory.Should().NotBeNull();
            stepHistory.Should().HaveCount(1);
            stepHistory![0].Status.Should().Be(status);
            stepHistory[0].DurationMs.Should().Be((long)duration.TotalMilliseconds);
            stepHistory[0].EndTime.Should().NotBeNull();
            stepHistory[0].ResultMessage.Should().Be(message);
        }

        [Fact]
        public async Task LogCompensationAsync_WithValidData_ShouldRecordCompensationDetails()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var compensatedSteps = new List<string> { "Step1", "Step2" };
            var compensationDetails = new Dictionary<string, object>
            {
                ["TotalSteps"] = 2,
                ["Reason"] = "TransactionFailed"
            };

            // 先创建事务日志
            await _transactionLogger.LogTransactionStartAsync(transactionId, "TestTransaction", "测试补偿");

            // Act
            await _transactionLogger.LogCompensationAsync(transactionId, compensatedSteps, compensationDetails);

            // Assert
            var logEntry = await _dbContext.TransactionLogs
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            logEntry.Should().NotBeNull();
            
            // 验证上下文快照包含补偿信息
            logEntry!.ContextSnapshot.Should().NotBeNull();
            logEntry.ContextSnapshot.Should().Contain("Compensation");
        }

        [Fact]
        public async Task GetTransactionHistoryAsync_WithDateRange_ShouldReturnMatchingTransactions()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(1);

            // 创建测试数据
            var transactionIds = new[]
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            foreach (var transactionId in transactionIds)
            {
                await _transactionLogger.LogTransactionStartAsync(transactionId, "TestTransaction", "测试历史查询");
                await _transactionLogger.LogTransactionEndAsync(transactionId, TransactionStatus.Completed, TimeSpan.FromSeconds(1), "完成");
            }

            // Act
            var result = await _transactionLogger.GetTransactionHistoryAsync(startDate, endDate, pageIndex: 1, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(3);
            result.TotalCount.Should().Be(3);
            result.Items.Should().AllSatisfy(item => 
            {
                item.StartTime.Should().BeOnOrAfter(startDate);
                item.StartTime.Should().BeBefore(endDate);
            });
        }

        [Fact]
        public async Task GetTransactionByIdAsync_WithExistingId_ShouldReturnTransactionDetails()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionName = "TestTransaction";
            var description = "测试获取事务详情";

            // 创建事务和步骤历史
            await _transactionLogger.LogTransactionStartAsync(transactionId, transactionName, description);
            await _transactionLogger.LogStepStartAsync(transactionId, "Step1", 1);
            await _transactionLogger.LogStepEndAsync(transactionId, "Step1", TransactionStepStatus.Completed, TimeSpan.FromSeconds(1), "成功");
            await _transactionLogger.LogTransactionEndAsync(transactionId, TransactionStatus.Completed, TimeSpan.FromSeconds(5), "完成");

            // Act
            var result = await _transactionLogger.GetTransactionByIdAsync(transactionId);

            // Assert
            result.Should().NotBeNull();
            result!.TransactionId.Should().Be(transactionId);
            result.TransactionName.Should().Be(transactionName);
            result.Status.Should().Be(TransactionStatus.Completed);
            result.Steps.Should().HaveCount(1);
            result.Steps[0].StepName.Should().Be("Step1");
            result.Steps[0].Status.Should().Be(TransactionStepStatus.Completed);
        }

        [Fact]
        public async Task GetTransactionStatisticsAsync_WithValidDateRange_ShouldReturnAccurateStatistics()
        {
            // Arrange
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(1);

            // 创建不同状态的事务
            var successTransactionId = Guid.NewGuid();
            var failedTransactionId = Guid.NewGuid();

            await _transactionLogger.LogTransactionStartAsync(successTransactionId, "SuccessTransaction", "成功事务");
            await _transactionLogger.LogTransactionEndAsync(successTransactionId, TransactionStatus.Completed, TimeSpan.FromSeconds(2), "成功");

            await _transactionLogger.LogTransactionStartAsync(failedTransactionId, "FailedTransaction", "失败事务");
            await _transactionLogger.LogTransactionEndAsync(failedTransactionId, TransactionStatus.Failed, TimeSpan.FromSeconds(3), "失败");

            // Act
            var result = await _transactionLogger.GetTransactionStatisticsAsync(startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.TotalTransactions.Should().Be(2);
            result.CompletedTransactions.Should().Be(1);
            result.FailedTransactions.Should().Be(1);
            result.CompensatedTransactions.Should().Be(0);
            result.AverageExecutionTimeMs.Should().Be(2500); // (2000 + 3000) / 2
            result.TransactionsByType.Should().ContainKey("SuccessTransaction");
            result.TransactionsByType.Should().ContainKey("FailedTransaction");
        }

        [Fact]
        public async Task GetSlowTransactionsAsync_WithThreshold_ShouldReturnSlowTransactions()
        {
            // Arrange
            var thresholdMs = 3000; // 3秒
            var slowTransactionId = Guid.NewGuid();
            var fastTransactionId = Guid.NewGuid();

            // 创建慢事务
            await _transactionLogger.LogTransactionStartAsync(slowTransactionId, "SlowTransaction", "慢事务");
            await _transactionLogger.LogTransactionEndAsync(slowTransactionId, TransactionStatus.Completed, TimeSpan.FromSeconds(5), "慢但成功");

            // 创建快事务
            await _transactionLogger.LogTransactionStartAsync(fastTransactionId, "FastTransaction", "快事务");
            await _transactionLogger.LogTransactionEndAsync(fastTransactionId, TransactionStatus.Completed, TimeSpan.FromSeconds(1), "快速完成");

            // Act
            var result = await _transactionLogger.GetSlowTransactionsAsync(thresholdMs);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].TransactionId.Should().Be(slowTransactionId);
            result[0].DurationMs.Should().Be(5000);
        }

        [Theory]
        [InlineData(TransactionStatus.Completed, "成功完成")]
        [InlineData(TransactionStatus.Failed, "执行失败")]
        [InlineData(TransactionStatus.Compensated, "已补偿")]
        public async Task LogTransactionEndAsync_WithDifferentStatuses_ShouldHandleAllStatuses(TransactionStatus status, string message)
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            await _transactionLogger.LogTransactionStartAsync(transactionId, "TestTransaction", "测试不同状态");

            // Act
            await _transactionLogger.LogTransactionEndAsync(transactionId, status, TimeSpan.FromSeconds(1), message);

            // Assert
            var logEntry = await _dbContext.TransactionLogs
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            logEntry.Should().NotBeNull();
            logEntry!.Status.Should().Be((int)status);
        }

        [Fact]
        public async Task LogTransactionStartAsync_WithNullUserId_ShouldSucceed()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var transactionName = "TestTransaction";
            var description = "测试空用户ID";

            // Act
            await _transactionLogger.LogTransactionStartAsync(transactionId, transactionName, description, userId: null);

            // Assert
            var logEntry = await _dbContext.TransactionLogs
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            logEntry.Should().NotBeNull();
            logEntry!.UserId.Should().BeNull();
        }

        [Fact]
        public async Task LogStepEndAsync_WithException_ShouldRecordExceptionDetails()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var stepName = "FailedStep";
            var stepOrder = 1;
            var exception = new InvalidOperationException("测试异常");

            await _transactionLogger.LogStepStartAsync(transactionId, stepName, stepOrder);

            // Act
            await _transactionLogger.LogStepEndAsync(transactionId, stepName, TransactionStepStatus.Failed, 
                TimeSpan.FromSeconds(1), "执行失败", exception);

            // Assert
            var cacheKey = $"step_history_{transactionId}";
            var stepHistory = _memoryCache.Get<List<TransactionStepHistory>>(cacheKey);
            
            stepHistory.Should().NotBeNull();
            stepHistory![0].Status.Should().Be(TransactionStepStatus.Failed);
            stepHistory[0].ResultMessage.Should().Be("执行失败");
            stepHistory[0].ExceptionDetails.Should().NotBeNull();
            stepHistory[0].ExceptionDetails.Should().Contain("InvalidOperationException");
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
            _memoryCache?.Dispose();
        }
    }
}