using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using LYBT.WebAPI.Middleware;
using LYBT.WebAPI.Services;
using FluentAssertions;
using System.Net;
using System.Text;

namespace LYBT.WebAPI.Tests.Middleware
{
    /// <summary>
    /// PerformanceMonitoringMiddleware 单元测试
    /// </summary>
    public class PerformanceMonitoringMiddlewareTests
    {
        private readonly Mock<ILogger<PerformanceMonitoringMiddleware>> _loggerMock;
        private readonly Mock<ISystemMetricsCollector> _metricsCollectorMock;
        private readonly PerformanceMonitoringMiddleware _middleware;
        private readonly DefaultHttpContext _httpContext;

        public PerformanceMonitoringMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<PerformanceMonitoringMiddleware>>();
            _metricsCollectorMock = new Mock<ISystemMetricsCollector>();
            
            _httpContext = new DefaultHttpContext();
            _httpContext.Response.Body = new MemoryStream();
            _httpContext.Request.Method = "GET";
            _httpContext.Request.Path = "/api/test";
            _httpContext.Request.QueryString = new QueryString("?id=123");
            _httpContext.Request.Headers["User-Agent"] = "Test Agent";
            _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");
            
            _middleware = new PerformanceMonitoringMiddleware(
                next: async (innerHttpContext) => await Task.CompletedTask,
                logger: _loggerMock.Object,
                metricsCollector: _metricsCollectorMock.Object
            );
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddRequestIdToResponseHeader()
        {
            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.Headers.Should().ContainKey("X-Request-Id");
            var requestId = _httpContext.Response.Headers["X-Request-Id"].ToString();
            requestId.Should().NotBeNullOrEmpty();
            requestId.Should().HaveLength(8);
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddRequestIdToHttpContextItems()
        {
            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Items.Should().ContainKey("RequestId");
            _httpContext.Items["RequestId"].Should().NotBeNull();
            _httpContext.Items["RequestId"].Should().BeOfType<string>();
            
            _httpContext.Items.Should().ContainKey("RequestStartTime");
            _httpContext.Items["RequestStartTime"].Should().NotBeNull();
            _httpContext.Items["RequestStartTime"].Should().BeOfType<DateTime>();
        }

        [Fact]
        public async Task InvokeAsync_WhenSuccessful_ShouldRecordSuccessMetrics()
        {
            // Arrange
            _httpContext.Response.StatusCode = 200;
            RequestPerformanceMetrics? capturedMetrics = null;
            
            _metricsCollectorMock.Setup(x => x.RecordRequestMetricsAsync(It.IsAny<RequestPerformanceMetrics>()))
                .Callback<RequestPerformanceMetrics>(metrics => capturedMetrics = metrics)
                .Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordRequestMetricsAsync(It.IsAny<RequestPerformanceMetrics>()), Times.Once);
            
            capturedMetrics.Should().NotBeNull();
            capturedMetrics!.Method.Should().Be("GET");
            capturedMetrics.Path.Should().Be("/api/test");
            capturedMetrics.StatusCode.Should().Be(200);
            capturedMetrics.Success.Should().BeTrue();
            capturedMetrics.Exception.Should().BeNull();
            capturedMetrics.ClientIp.Should().Be("192.168.1.100");
            capturedMetrics.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task InvokeAsync_WhenException_ShouldRecordFailureMetrics()
        {
            // Arrange
            var expectedException = new InvalidOperationException("测试异常");
            RequestPerformanceMetrics? capturedMetrics = null;
            
            var middleware = new PerformanceMonitoringMiddleware(
                next: async (innerHttpContext) =>
                {
                    throw expectedException;
                },
                logger: _loggerMock.Object,
                metricsCollector: _metricsCollectorMock.Object
            );
            
            _metricsCollectorMock.Setup(x => x.RecordRequestMetricsAsync(It.IsAny<RequestPerformanceMetrics>()))
                .Callback<RequestPerformanceMetrics>(metrics => capturedMetrics = metrics)
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(_httpContext));
            
            _metricsCollectorMock.Verify(x => x.RecordRequestMetricsAsync(It.IsAny<RequestPerformanceMetrics>()), Times.Once);
            
            capturedMetrics.Should().NotBeNull();
            capturedMetrics!.Success.Should().BeFalse();
            capturedMetrics.Exception.Should().Be("InvalidOperationException");
        }

        [Fact]
        public async Task InvokeAsync_WithClientIpFromXForwardedFor_ShouldUseForwardedIp()
        {
            // Arrange
            _httpContext.Request.Headers["X-Forwarded-For"] = "10.0.0.1, 10.0.0.2";
            RequestPerformanceMetrics? capturedMetrics = null;
            
            _metricsCollectorMock.Setup(x => x.RecordRequestMetricsAsync(It.IsAny<RequestPerformanceMetrics>()))
                .Callback<RequestPerformanceMetrics>(metrics => capturedMetrics = metrics)
                .Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            capturedMetrics.Should().NotBeNull();
            capturedMetrics!.ClientIp.Should().Be("10.0.0.1, 10.0.0.2");
        }

        [Fact]
        public async Task InvokeAsync_WithClientIpFromXRealIP_ShouldUseRealIp()
        {
            // Arrange
            _httpContext.Request.Headers["X-Real-IP"] = "10.0.0.3";
            RequestPerformanceMetrics? capturedMetrics = null;
            
            _metricsCollectorMock.Setup(x => x.RecordRequestMetricsAsync(It.IsAny<RequestPerformanceMetrics>()))
                .Callback<RequestPerformanceMetrics>(metrics => capturedMetrics = metrics)
                .Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            capturedMetrics.Should().NotBeNull();
            capturedMetrics!.ClientIp.Should().Be("10.0.0.3");
        }

        [Fact]
        public async Task InvokeAsync_WithNoRemoteIp_ShouldUseUnknown()
        {
            // Arrange
            _httpContext.Connection.RemoteIpAddress = null;
            RequestPerformanceMetrics? capturedMetrics = null;
            
            _metricsCollectorMock.Setup(x => x.RecordRequestMetricsAsync(It.IsAny<RequestPerformanceMetrics>()))
                .Callback<RequestPerformanceMetrics>(metrics => capturedMetrics = metrics)
                .Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            capturedMetrics.Should().NotBeNull();
            capturedMetrics!.ClientIp.Should().Be("Unknown");
        }

        [Fact]
        public async Task InvokeAsync_WhenDebugEnabled_ShouldLogRequestDetails()
        {
            // Arrange
            _loggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
            
            var requestBody = "{\"test\": \"data\"}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
            _httpContext.Request.Body = stream;
            _httpContext.Request.ContentLength = stream.Length;
            _httpContext.Request.Method = "POST";

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request started")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithLargeRequestBody_ShouldNotLogBody()
        {
            // Arrange
            _loggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
            
            _httpContext.Request.ContentLength = 5000; // 超过4096字节限制
            _httpContext.Request.Method = "POST";

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            // 应该记录请求开始，但不应该包含请求体
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request started") && v.ToString()!.Contains("Body:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WithSlowRequest_ShouldLogWarning()
        {
            // Arrange
            var slowMiddleware = new PerformanceMonitoringMiddleware(
                next: async (innerHttpContext) =>
                {
                    await Task.Delay(6000); // 6秒延迟，超过5秒阈值
                },
                logger: _loggerMock.Object,
                metricsCollector: _metricsCollectorMock.Object
            );

            // Act
            await slowMiddleware.InvokeAsync(_httpContext);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Slow request detected")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(200, LogLevel.Debug)]
        [InlineData(400, LogLevel.Warning)]
        [InlineData(404, LogLevel.Warning)]
        [InlineData(500, LogLevel.Error)]
        [InlineData(503, LogLevel.Error)]
        public async Task InvokeAsync_WithDifferentStatusCodes_ShouldLogAtCorrectLevel(int statusCode, LogLevel expectedLogLevel)
        {
            // Arrange
            _httpContext.Response.StatusCode = statusCode;
            _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    expectedLogLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithSensitiveDataInRequestBody_ShouldSanitize()
        {
            // Arrange
            _loggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
            
            var requestBody = "{\"username\": \"testuser\", \"password\": \"secret123\", \"email\": \"test@example.com\"}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
            _httpContext.Request.Body = stream;
            _httpContext.Request.ContentLength = stream.Length;
            _httpContext.Request.Method = "POST";

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString()!.Contains("***") && 
                        !v.ToString()!.Contains("secret123") &&
                        v.ToString()!.Contains("testuser")), // username应该保留
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithNonJsonRequestBody_ShouldLogContentLength()
        {
            // Arrange
            _loggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
            
            var requestBody = "This is not JSON content";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
            _httpContext.Request.Body = stream;
            _httpContext.Request.ContentLength = stream.Length;
            _httpContext.Request.Method = "POST";

            // Act
            await _middleware.InvokeAsync(_httpContext);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString()!.Contains("[Non-JSON content, length: 24]")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithResponseTime_ShouldLogCorrectDuration()
        {
            // Arrange
            var delay = 1500; // 1.5秒
            _loggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            
            var middleware = new PerformanceMonitoringMiddleware(
                next: async (innerHttpContext) =>
                {
                    await Task.Delay(delay);
                },
                logger: _loggerMock.Object,
                metricsCollector: _metricsCollectorMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information, // 1-2秒之间应该是Information级别
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ShouldCallNextMiddleware()
        {
            // Arrange
            var nextCalled = false;
            var middleware = new PerformanceMonitoringMiddleware(
                next: async (innerHttpContext) =>
                {
                    nextCalled = true;
                    await Task.CompletedTask;
                },
                logger: _loggerMock.Object,
                metricsCollector: _metricsCollectorMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            nextCalled.Should().BeTrue();
        }
    }
}