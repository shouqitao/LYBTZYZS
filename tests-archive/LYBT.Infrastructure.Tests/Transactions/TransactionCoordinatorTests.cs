using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using FluentAssertions;
using LYBT.Infrastructure.Transactions;
using LYBT.Infrastructure.Transactions.Steps;
using LYBT.Infrastructure.Transactions.Monitoring;

namespace LYBT.Infrastructure.Tests.Transactions
{
    /// <summary>
    /// 事务协调器单元测试
    /// 测试事务协调器的核心功能：事务执行、补偿、重试、并行处理等
    /// </summary>
    public class TransactionCoordinatorTests
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<ITransactionLogger> _transactionLoggerMock;
        private readonly Mock<ILogger<TransactionCoordinator<TestTransactionContext>>> _loggerMock;
        private readonly TransactionCoordinator<TestTransactionContext> _coordinator;

        public TransactionCoordinatorTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _transactionLoggerMock = new Mock<ITransactionLogger>();
            _loggerMock = new Mock<ILogger<TransactionCoordinator<TestTransactionContext>>>();

            _coordinator = new TransactionCoordinator<TestTransactionContext>(
                _serviceProviderMock.Object,
                _transactionLoggerMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulSteps_ShouldReturnCompletedResult()
        {
            // Arrange
            var context = new TestTransactionContext { TestData = "Initial" };
            var step1 = new TestSuccessfulStep("Step1", 1);
            var step2 = new TestSuccessfulStep("Step2", 2);

            var definition = new TransactionDefinition<TestTransactionContext>
            {
                Name = "TestTransaction",
                Description = "测试事务",
                Steps = new List<ITransactionStep<TestTransactionContext>> { step1, step2 },
                Timeout = TimeSpan.FromMinutes(5),
                MaxRetryCount = 1,
                EnableAutoCompensation = true,
                EnableParallelExecution = false
            };

            // Act
            var result = await _coordinator.ExecuteAsync(definition, context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TransactionStatus.Completed);
            result.Context.Should().Be(context);
            result.Message.Should().NotBeEmpty();
            result.EndTime.Should().NotBeNull();
            result.EndTime.Should().BeAfter(result.StartTime);
            result.ExecutedSteps.Should().HaveCount(2);

            // 验证步骤执行顺序
            result.ExecutedSteps[0].StepName.Should().Be("Step1");
            result.ExecutedSteps[1].StepName.Should().Be("Step2");
        }

        [Fact]
        public async Task ExecuteAsync_WithFailedStep_ShouldReturnFailedResult()
        {
            // Arrange
            var context = new TestTransactionContext { TestData = "Initial" };
            var step1 = new TestSuccessfulStep("Step1", 1);
            var step2 = new TestFailedStep("Step2", 2, "测试失败");

            var definition = new TransactionDefinition<TestTransactionContext>
            {
                Name = "TestTransaction",
                Description = "测试事务失败",
                Steps = new List<ITransactionStep<TestTransactionContext>> { step1, step2 },
                Timeout = TimeSpan.FromMinutes(5),
                MaxRetryCount = 0,
                EnableAutoCompensation = false,
                EnableParallelExecution = false
            };

            // Act
            var result = await _coordinator.ExecuteAsync(definition, context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TransactionStatus.Failed);
            result.Message.Should().Contain("测试失败");
            result.ExecutedSteps.Should().HaveCount(2);
            result.ExecutedSteps[1].Status.Should().Be(TransactionStepStatus.Failed);
        }

        [Fact]
        public async Task ExecuteAsync_WithFailedStepAndCompensation_ShouldCompensateSuccessfulSteps()
        {
            // Arrange
            var context = new TestTransactionContext { TestData = "Initial" };
            var step1 = new TestCompensatableStep("Step1", 1);
            var step2 = new TestFailedStep("Step2", 2, "测试失败");

            var definition = new TransactionDefinition<TestTransactionContext>
            {
                Name = "TestTransaction",
                Description = "测试事务补偿",
                Steps = new List<ITransactionStep<TestTransactionContext>> { step1, step2 },
                Timeout = TimeSpan.FromMinutes(5),
                MaxRetryCount = 0,
                EnableAutoCompensation = true,
                EnableParallelExecution = false
            };

            // Act
            var result = await _coordinator.ExecuteAsync(definition, context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TransactionStatus.Compensated);
            result.CompensatedSteps.Should().HaveCount(1);
            result.CompensatedSteps[0].StepName.Should().Be("Step1");

            // 验证补偿步骤被调用
            var compensatableStep = (TestCompensatableStep)step1;
            compensatableStep.CompensationCalled.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_WithRetryableStep_ShouldRetryFailedSteps()
        {
            // Arrange
            var context = new TestTransactionContext { TestData = "Initial" };
            var step1 = new TestRetryableStep("Step1", 1, failCount: 2);

            var definition = new TransactionDefinition<TestTransactionContext>
            {
                Name = "TestTransaction",
                Description = "测试重试",
                Steps = new List<ITransactionStep<TestTransactionContext>> { step1 },
                Timeout = TimeSpan.FromMinutes(5),
                MaxRetryCount = 3,
                EnableAutoCompensation = false,
                EnableParallelExecution = false
            };

            // Act
            var result = await _coordinator.ExecuteAsync(definition, context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TransactionStatus.Completed);
            
            // 验证重试次数
            var retryableStep = (TestRetryableStep)step1;
            retryableStep.ExecutionCount.Should().Be(3); // 初次执行 + 2次重试
        }

        [Fact]
        public async Task ExecuteAsync_WithTimeout_ShouldTimeoutAndCompensate()
        {
            // Arrange
            var context = new TestTransactionContext { TestData = "Initial" };
            var step1 = new TestLongRunningStep("Step1", 1, TimeSpan.FromSeconds(5));

            var definition = new TransactionDefinition<TestTransactionContext>
            {
                Name = "TestTransaction",
                Description = "测试超时",
                Steps = new List<ITransactionStep<TestTransactionContext>> { step1 },
                Timeout = TimeSpan.FromMilliseconds(100), // 非常短的超时
                MaxRetryCount = 0,
                EnableAutoCompensation = false,
                EnableParallelExecution = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _coordinator.ExecuteAsync(definition, context, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellationToken_ShouldCancelGracefully()
        {
            // Arrange
            var context = new TestTransactionContext { TestData = "Initial" };
            var step1 = new TestLongRunningStep("Step1", 1, TimeSpan.FromSeconds(5));

            var definition = new TransactionDefinition<TestTransactionContext>
            {
                Name = "TestTransaction",
                Description = "测试取消",
                Steps = new List<ITransactionStep<TestTransactionContext>> { step1 },
                Timeout = TimeSpan.FromMinutes(5),
                MaxRetryCount = 0,
                EnableAutoCompensation = false,
                EnableParallelExecution = false
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _coordinator.ExecuteAsync(definition, context, cancellationTokenSource.Token));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExecuteAsync_WithValidContext_ShouldLogTransactionEvents(bool enableParallel)
        {
            // Arrange
            var context = new TestTransactionContext { TestData = "Initial" };
            var step1 = new TestSuccessfulStep("Step1", 1);

            var definition = new TransactionDefinition<TestTransactionContext>
            {
                Name = "TestTransaction",
                Description = "测试日志记录",
                Steps = new List<ITransactionStep<TestTransactionContext>> { step1 },
                Timeout = TimeSpan.FromMinutes(5),
                MaxRetryCount = 0,
                EnableAutoCompensation = false,
                EnableParallelExecution = enableParallel
            };

            // Act
            var result = await _coordinator.ExecuteAsync(definition, context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TransactionStatus.Completed);

            // 验证日志记录调用
            _transactionLoggerMock.Verify(
                x => x.LogTransactionStartAsync(
                    result.TransactionId,
                    "TestTransaction",
                    "测试日志记录",
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _transactionLoggerMock.Verify(
                x => x.LogTransactionEndAsync(
                    result.TransactionId,
                    TransactionStatus.Completed,
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptySteps_ShouldReturnCompletedResult()
        {
            // Arrange
            var context = new TestTransactionContext { TestData = "Initial" };

            var definition = new TransactionDefinition<TestTransactionContext>
            {
                Name = "EmptyTransaction",
                Description = "空事务",
                Steps = new List<ITransactionStep<TestTransactionContext>>(),
                Timeout = TimeSpan.FromMinutes(5),
                MaxRetryCount = 0,
                EnableAutoCompensation = false,
                EnableParallelExecution = false
            };

            // Act
            var result = await _coordinator.ExecuteAsync(definition, context, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TransactionStatus.Completed);
            result.ExecutedSteps.Should().BeEmpty();
            result.CompensatedSteps.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteAsync_WithNullDefinition_ShouldThrowArgumentNullException()
        {
            // Arrange
            var context = new TestTransactionContext { TestData = "Initial" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _coordinator.ExecuteAsync(null, context, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_WithNullContext_ShouldThrowArgumentNullException()
        {
            // Arrange
            var definition = new TransactionDefinition<TestTransactionContext>
            {
                Name = "TestTransaction",
                Description = "测试空上下文",
                Steps = new List<ITransactionStep<TestTransactionContext>>(),
                Timeout = TimeSpan.FromMinutes(5),
                MaxRetryCount = 0,
                EnableAutoCompensation = false,
                EnableParallelExecution = false
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _coordinator.ExecuteAsync(definition, null, CancellationToken.None));
        }
    }

    #region Test Implementations

    /// <summary>
    /// 测试用事务上下文
    /// </summary>
    public class TestTransactionContext : TransactionContext
    {
        public string TestData { get; set; } = string.Empty;
    }

    /// <summary>
    /// 成功执行的测试步骤
    /// </summary>
    public class TestSuccessfulStep : TransactionStepBase<TestTransactionContext>
    {
        public TestSuccessfulStep(string stepName, int order)
        {
            StepName = stepName;
            Order = order;
        }

        public override string StepName { get; }
        public override int Order { get; }
        public override bool SupportsCompensation => false;

        public override Task<TransactionStepResult> ExecuteAsync(TestTransactionContext context, CancellationToken cancellationToken = default)
        {
            context.TestData = $"{context.TestData}-{StepName}";
            return Task.FromResult(CreateSuccessResult());
        }
    }

    /// <summary>
    /// 失败的测试步骤
    /// </summary>
    public class TestFailedStep : TransactionStepBase<TestTransactionContext>
    {
        private readonly string _errorMessage;

        public TestFailedStep(string stepName, int order, string errorMessage)
        {
            StepName = stepName;
            Order = order;
            _errorMessage = errorMessage;
        }

        public override string StepName { get; }
        public override int Order { get; }
        public override bool SupportsCompensation => false;

        public override Task<TransactionStepResult> ExecuteAsync(TestTransactionContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateFailureResult(new InvalidOperationException(_errorMessage)));
        }
    }

    /// <summary>
    /// 支持补偿的测试步骤
    /// </summary>
    public class TestCompensatableStep : TransactionStepBase<TestTransactionContext>
    {
        public bool CompensationCalled { get; private set; }

        public TestCompensatableStep(string stepName, int order)
        {
            StepName = stepName;
            Order = order;
        }

        public override string StepName { get; }
        public override int Order { get; }
        public override bool SupportsCompensation => true;

        public override Task<TransactionStepResult> ExecuteAsync(TestTransactionContext context, CancellationToken cancellationToken = default)
        {
            context.TestData = $"{context.TestData}-{StepName}";
            return Task.FromResult(CreateSuccessResult());
        }

        public override Task<TransactionStepResult> CompensateAsync(TestTransactionContext context, TransactionStepResult originalResult, CancellationToken cancellationToken = default)
        {
            CompensationCalled = true;
            context.TestData = context.TestData.Replace($"-{StepName}", "");
            return Task.FromResult(CreateSuccessResult());
        }
    }

    /// <summary>
    /// 可重试的测试步骤
    /// </summary>
    public class TestRetryableStep : TransactionStepBase<TestTransactionContext>
    {
        private readonly int _failCount;
        public int ExecutionCount { get; private set; }

        public TestRetryableStep(string stepName, int order, int failCount)
        {
            StepName = stepName;
            Order = order;
            _failCount = failCount;
        }

        public override string StepName { get; }
        public override int Order { get; }
        public override bool SupportsCompensation => false;

        public override Task<TransactionStepResult> ExecuteAsync(TestTransactionContext context, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;

            if (ExecutionCount <= _failCount)
            {
                return Task.FromResult(CreateFailureResult(new InvalidOperationException($"第{ExecutionCount}次执行失败")));
            }

            context.TestData = $"{context.TestData}-{StepName}";
            return Task.FromResult(CreateSuccessResult());
        }
    }

    /// <summary>
    /// 长时间运行的测试步骤
    /// </summary>
    public class TestLongRunningStep : TransactionStepBase<TestTransactionContext>
    {
        private readonly TimeSpan _duration;

        public TestLongRunningStep(string stepName, int order, TimeSpan duration)
        {
            StepName = stepName;
            Order = order;
            _duration = duration;
        }

        public override string StepName { get; }
        public override int Order { get; }
        public override bool SupportsCompensation => false;

        public override async Task<TransactionStepResult> ExecuteAsync(TestTransactionContext context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_duration, cancellationToken);
            context.TestData = $"{context.TestData}-{StepName}";
            return CreateSuccessResult();
        }
    }

    #endregion
}