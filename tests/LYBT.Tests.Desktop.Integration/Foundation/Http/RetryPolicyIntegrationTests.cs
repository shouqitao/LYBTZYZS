using System.Net;
using System.Net.Http;
using FluentAssertions;
using LYBT.Desktop.Foundation.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;
using Polly.CircuitBreaker;

namespace LYBT.Tests.Desktop.Integration.Foundation.Http;

/// <summary>
/// Polly韧性策略集成测试
/// 验证重试、熔断器、超时策略是否正确工作
/// </summary>
public class RetryPolicyIntegrationTests
{
    private readonly ILogger _logger;

    public RetryPolicyIntegrationTests()
    {
        _logger = Substitute.For<ILogger>();
    }

    #region 重试策略测试

    [Fact]
    public async Task RetryPolicy_WhenTransientFailure_ShouldRetryAndSucceed()
    {
        // Arrange
        var attemptCount = 0;
        var policy = RetryPolicyExtensions.CreateHttpRetryPolicy(_logger, retryCount: 3);

        // Act
        var result = await policy.ExecuteAsync(() =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task RetryPolicy_WhenAllRetriesFail_ShouldReturnLastFailure()
    {
        // Arrange
        var attemptCount = 0;
        var policy = RetryPolicyExtensions.CreateHttpRetryPolicy(_logger, retryCount: 3);

        // Act
        var result = await policy.ExecuteAsync(() =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        attemptCount.Should().Be(4);
    }

    [Fact]
    public async Task RetryPolicy_WhenHttpRequestException_ShouldRetry()
    {
        // Arrange
        var attemptCount = 0;
        var policy = RetryPolicyExtensions.CreateHttpRetryPolicy(_logger, retryCount: 2);

        // Act
        var result = await policy.ExecuteAsync(() =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("模拟网络故障");
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task RetryPolicy_WhenNonRetryableStatusCode_ShouldNotRetry()
    {
        // Arrange
        var attemptCount = 0;
        var policy = RetryPolicyExtensions.CreateHttpRetryPolicy(_logger, retryCount: 3);

        // Act - 400 Bad Request 不应重试
        var result = await policy.ExecuteAsync(() =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        attemptCount.Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_WhenInternalServerError_ShouldNotRetry()
    {
        // Arrange - Issue #1262: 500错误不再重试
        var attemptCount = 0;
        var policy = RetryPolicyExtensions.CreateHttpRetryPolicy(_logger, retryCount: 3);

        // Act
        var result = await policy.ExecuteAsync(() =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        attemptCount.Should().Be(1);
    }

    #endregion

    #region 熔断器策略测试

    [Fact]
    public async Task CircuitBreaker_WhenThresholdExceeded_ShouldBreak()
    {
        // Arrange
        var policy = RetryPolicyExtensions.CreateCircuitBreakerPolicy(
            _logger,
            failureThreshold: 3,
            durationOfBreak: TimeSpan.FromSeconds(30));

        // Act - 触发3次失败
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await policy.ExecuteAsync(() =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
            }
            catch { /* 忽略 */ }
        }

        // Assert - 熔断器应该打开
        var act = async () => await policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        await act.Should().ThrowAsync<BrokenCircuitException>();
    }

    [Fact]
    public async Task CircuitBreaker_WhenBelowThreshold_ShouldNotBreak()
    {
        // Arrange
        var policy = RetryPolicyExtensions.CreateCircuitBreakerPolicy(
            _logger,
            failureThreshold: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));

        // Act - 只触发2次失败（低于阈值5）
        for (var i = 0; i < 2; i++)
        {
            await policy.ExecuteAsync(() =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        }

        // Assert - 熔断器应该仍然关闭
        var result = await policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region 超时策略测试

    [Fact]
    public async Task TimeoutPolicy_WhenWithinTimeout_ShouldSucceed()
    {
        // Arrange
        var policy = RetryPolicyExtensions.CreateTimeoutPolicy(
            TimeSpan.FromSeconds(5),
            _logger);

        // Act
        var result = await policy.ExecuteAsync(async (ct) =>
        {
            await Task.Delay(100, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TimeoutPolicy_WhenExceedsTimeout_ShouldThrow()
    {
        // Arrange
        var policy = RetryPolicyExtensions.CreateTimeoutPolicy(
            TimeSpan.FromMilliseconds(100),
            _logger);

        // Act
        var act = async () => await policy.ExecuteAsync(async (ct) =>
        {
            await Task.Delay(5000, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Polly.Timeout.TimeoutRejectedException>();
    }

    #endregion

    #region 组合策略测试

    [Fact]
    public async Task CompositePolicy_WhenTransientFailure_ShouldRetryAndSucceed()
    {
        // Arrange
        var attemptCount = 0;
        var policy = RetryPolicyExtensions.CreateCompositePolicy(
            _logger,
            retryCount: 3,
            baseDelay: TimeSpan.FromMilliseconds(10),
            timeout: TimeSpan.FromSeconds(30),
            circuitBreakerThreshold: 10);

        // Act
        var result = await policy.ExecuteAsync((ct) =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task CompositePolicy_WhenSuccessOnFirstAttempt_ShouldNotRetry()
    {
        // Arrange
        var attemptCount = 0;
        var policy = RetryPolicyExtensions.CreateCompositePolicy(
            _logger,
            retryCount: 3,
            timeout: TimeSpan.FromSeconds(30),
            circuitBreakerThreshold: 5);

        // Act
        var result = await policy.ExecuteAsync((ct) =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(1);
    }

    #endregion
}
