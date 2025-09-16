using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LYBT.WebAPI.Controllers;
using LYBT.WebAPI.Services;
using FluentAssertions;
using System.Collections.Generic;

namespace LYBT.WebAPI.Tests.Controllers
{
    /// <summary>
    /// MonitoringController 单元测试
    /// </summary>
    public class MonitoringControllerTests
    {
        private readonly Mock<ISystemMetricsCollector> _metricsCollectorMock;
        private readonly Mock<ILogger<MonitoringController>> _loggerMock;
        private readonly MonitoringController _controller;

        public MonitoringControllerTests()
        {
            _metricsCollectorMock = new Mock<ISystemMetricsCollector>();
            _loggerMock = new Mock<ILogger<MonitoringController>>();

            _controller = new MonitoringController(
                _metricsCollectorMock.Object,
                _loggerMock.Object
            );
        }

        #region GetApiPerformanceStats Tests

        [Fact]
        public async Task GetApiPerformanceStats_ShouldReturnPerformanceStats()
        {
            // Arrange
            var expectedStats = new ApiPerformanceStats
            {
                TotalRequests = 10000,
                SuccessfulRequests = 9800,
                FailedRequests = 200,
                SuccessRate = 0.98,
                RequestsPerMinute = 250.5,
                AverageResponseTime = TimeSpan.FromMilliseconds(150),
                MaxResponseTime = TimeSpan.FromMilliseconds(5000),
                MinResponseTime = TimeSpan.FromMilliseconds(10),
                P95ResponseTime = TimeSpan.FromMilliseconds(500),
                P99ResponseTime = TimeSpan.FromMilliseconds(1000)
            };

            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetApiPerformanceStats();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var stats = okResult.Value as ApiPerformanceStats;
            stats.Should().NotBeNull();
            stats!.TotalRequests.Should().Be(10000);
            stats.SuccessRate.Should().Be(0.98);
            stats.RequestsPerMinute.Should().Be(250.5);
        }

        [Fact]
        public async Task GetApiPerformanceStats_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ThrowsAsync(new Exception("数据库连接失败"));

            // Act
            var result = await _controller.GetApiPerformanceStats();

            // Assert
            result.Should().NotBeNull();
            var objectResult = result.Result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetErrorStats Tests

        [Fact]
        public async Task GetErrorStats_ShouldReturnErrorStatistics()
        {
            // Arrange
            var expectedStats = new ErrorStats
            {
                TotalErrors = 200,
                ErrorRate = 0.02,
                ErrorsByType = new Dictionary<string, int>
                {
                    ["BadRequest"] = 100,
                    ["NotFound"] = 50,
                    ["InternalServerError"] = 50
                },
                ErrorsByEndpoint = new Dictionary<string, int>
                {
                    ["/api/v1/users"] = 80,
                    ["/api/v1/patients"] = 120
                },
                RecentErrors = new List<ErrorDetail>
                {
                    new() { Message = "用户不存在", Endpoint = "/api/v1/users/123", Timestamp = DateTime.UtcNow.AddMinutes(-5) }
                }
            };

            _metricsCollectorMock.Setup(x => x.GetErrorStatsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetErrorStats();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var stats = okResult.Value as ErrorStats;
            stats.Should().NotBeNull();
            stats!.TotalErrors.Should().Be(200);
            stats.ErrorRate.Should().Be(0.02);
            stats.ErrorsByType.Should().HaveCount(3);
        }

        #endregion

        #region GetHotApiEndpoints Tests

        [Fact]
        public async Task GetHotApiEndpoints_WithDefaultCount_ShouldReturnTop10Endpoints()
        {
            // Arrange
            var expectedEndpoints = new List<ApiEndpointStats>
            {
                new() { Endpoint = "/api/v1/auth/login", RequestCount = 5000, AverageResponseTime = TimeSpan.FromMilliseconds(100) },
                new() { Endpoint = "/api/v1/users", RequestCount = 3000, AverageResponseTime = TimeSpan.FromMilliseconds(150) }
            };

            _metricsCollectorMock.Setup(x => x.GetHotApiEndpointsAsync(10))
                .ReturnsAsync(expectedEndpoints);

            // Act
            var result = await _controller.GetHotApiEndpoints();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var endpoints = okResult.Value as List<ApiEndpointStats>;
            endpoints.Should().NotBeNull();
            endpoints!.Should().HaveCount(2);
            endpoints[0].RequestCount.Should().Be(5000);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(20)]
        [InlineData(50)]
        public async Task GetHotApiEndpoints_WithValidCount_ShouldReturnSpecifiedCount(int count)
        {
            // Arrange
            var expectedEndpoints = new List<ApiEndpointStats>();

            _metricsCollectorMock.Setup(x => x.GetHotApiEndpointsAsync(count))
                .ReturnsAsync(expectedEndpoints);

            // Act
            var result = await _controller.GetHotApiEndpoints(count);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
            
            _metricsCollectorMock.Verify(x => x.GetHotApiEndpointsAsync(count), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(51)]
        [InlineData(100)]
        public async Task GetHotApiEndpoints_WithInvalidCount_ShouldReturnBadRequest(int count)
        {
            // Act
            var result = await _controller.GetHotApiEndpoints(count);

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("count参数必须在1-50之间");
        }

        #endregion

        #region GetPerformanceTrend Tests

        [Theory]
        [InlineData("1m")]
        [InlineData("1h")]
        [InlineData("24h")]
        public async Task GetPerformanceTrend_WithValidPeriod_ShouldReturnTrendData(string period)
        {
            // Arrange
            var expectedTrend = new SystemPerformanceTrend
            {
                Period = period,
                DataPoints = new List<PerformanceDataPoint>
                {
                    new() { Timestamp = DateTime.UtcNow.AddMinutes(-30), RequestCount = 100, AverageResponseTimeMs = 150 },
                    new() { Timestamp = DateTime.UtcNow, RequestCount = 120, AverageResponseTimeMs = 140 }
                },
                TrendDirection = "improving",
                AverageRequestRate = 110,
                AverageResponseTime = 145
            };

            _metricsCollectorMock.Setup(x => x.GetPerformanceTrendAsync(It.IsAny<TimeSpan>()))
                .ReturnsAsync(expectedTrend);

            // Act
            var result = await _controller.GetPerformanceTrend(period);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var trend = okResult.Value as SystemPerformanceTrend;
            trend.Should().NotBeNull();
            trend!.Period.Should().Be(period);
            trend.DataPoints.Should().HaveCount(2);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("2d")]
        [InlineData("")]
        public async Task GetPerformanceTrend_WithInvalidPeriod_ShouldReturnBadRequest(string period)
        {
            // Act
            var result = await _controller.GetPerformanceTrend(period);

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("无效的时间段格式，支持格式：1m, 5m, 15m, 30m, 1h, 6h, 12h, 24h");
        }

        #endregion

        #region GetDashboardData Tests

        [Fact]
        public async Task GetDashboardData_ShouldReturnAllMonitoringData()
        {
            // Arrange
            var apiPerformance = new ApiPerformanceStats { TotalRequests = 1000 };
            var errorStats = new ErrorStats { TotalErrors = 10 };
            var hotEndpoints = new List<ApiEndpointStats> { new() { Endpoint = "/api/v1/test" } };
            var performanceTrend = new SystemPerformanceTrend { Period = "1h" };

            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ReturnsAsync(apiPerformance);
            _metricsCollectorMock.Setup(x => x.GetErrorStatsAsync())
                .ReturnsAsync(errorStats);
            _metricsCollectorMock.Setup(x => x.GetHotApiEndpointsAsync(5))
                .ReturnsAsync(hotEndpoints);
            _metricsCollectorMock.Setup(x => x.GetPerformanceTrendAsync(TimeSpan.FromHours(1)))
                .ReturnsAsync(performanceTrend);

            // Act
            var result = await _controller.GetDashboardData();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var dashboard = okResult.Value as MonitoringDashboardData;
            dashboard.Should().NotBeNull();
            dashboard!.ApiPerformance.TotalRequests.Should().Be(1000);
            dashboard.ErrorStats.TotalErrors.Should().Be(10);
            dashboard.HotEndpoints.Should().HaveCount(1);
            dashboard.PerformanceTrend.Period.Should().Be("1h");
            dashboard.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GetDashboardData_ShouldCallAllMetricsMethodsInParallel()
        {
            // Arrange
            var apiPerformance = new ApiPerformanceStats();
            var errorStats = new ErrorStats();
            var hotEndpoints = new List<ApiEndpointStats>();
            var performanceTrend = new SystemPerformanceTrend();

            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ReturnsAsync(apiPerformance);
            _metricsCollectorMock.Setup(x => x.GetErrorStatsAsync())
                .ReturnsAsync(errorStats);
            _metricsCollectorMock.Setup(x => x.GetHotApiEndpointsAsync(5))
                .ReturnsAsync(hotEndpoints);
            _metricsCollectorMock.Setup(x => x.GetPerformanceTrendAsync(TimeSpan.FromHours(1)))
                .ReturnsAsync(performanceTrend);

            // Act
            await _controller.GetDashboardData();

            // Assert
            _metricsCollectorMock.Verify(x => x.GetApiPerformanceStatsAsync(), Times.Once);
            _metricsCollectorMock.Verify(x => x.GetErrorStatsAsync(), Times.Once);
            _metricsCollectorMock.Verify(x => x.GetHotApiEndpointsAsync(5), Times.Once);
            _metricsCollectorMock.Verify(x => x.GetPerformanceTrendAsync(TimeSpan.FromHours(1)), Times.Once);
        }

        #endregion

        #region CleanupExpiredData Tests

        [Fact]
        public async Task CleanupExpiredData_ShouldReturnSuccessMessage()
        {
            // Arrange
            _metricsCollectorMock.Setup(x => x.CleanupExpiredMetricsAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CleanupExpiredData();

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var response = okResult.Value;
            response.Should().NotBeNull();
            
            // 验证返回的对象包含预期的属性
            var messageProperty = response!.GetType().GetProperty("message");
            messageProperty?.GetValue(response).Should().Be("过期监控数据清理完成");

            _metricsCollectorMock.Verify(x => x.CleanupExpiredMetricsAsync(), Times.Once);
        }

        [Fact]
        public async Task CleanupExpiredData_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            _metricsCollectorMock.Setup(x => x.CleanupExpiredMetricsAsync())
                .ThrowsAsync(new Exception("清理操作失败"));

            // Act
            var result = await _controller.CleanupExpiredData();

            // Assert
            result.Should().NotBeNull();
            var objectResult = result as ObjectResult;
            objectResult.Should().NotBeNull();
            objectResult!.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetMonitoringConfig Tests

        [Fact]
        public void GetMonitoringConfig_ShouldReturnConfigurationInfo()
        {
            // Act
            var result = _controller.GetMonitoringConfig();

            // Assert
            result.Should().NotBeNull();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var config = okResult.Value as MonitoringConfigInfo;
            config.Should().NotBeNull();
            config!.MetricsRetentionHours.Should().Be(24);
            config.MaxMetricsInMemory.Should().Be(10000);
            config.SnapshotIntervalMinutes.Should().Be(1);
            config.CleanupIntervalHours.Should().Be(1);
            
            config.PerformanceThresholds.Should().NotBeNull();
            config.PerformanceThresholds.SlowRequestMs.Should().Be(2000);
            config.PerformanceThresholds.VerySlowRequestMs.Should().Be(5000);
            
            config.EnabledFeatures.Should().NotBeNull();
            config.EnabledFeatures.Should().Contain("ApiPerformanceTracking");
            config.EnabledFeatures.Should().Contain("ErrorTracking");
        }

        #endregion

        #region GetRealtimeStatus Tests

        [Fact]
        public async Task GetRealtimeStatus_WhenSystemHealthy_ShouldReturnHealthyStatus()
        {
            // Arrange
            var apiStats = new ApiPerformanceStats
            {
                RequestsPerMinute = 200,
                AverageResponseTime = TimeSpan.FromMilliseconds(150),
                SuccessRate = 0.98
            };

            var errorStats = new ErrorStats
            {
                ErrorRate = 0.02 // 低于5%，健康
            };

            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ReturnsAsync(apiStats);
            _metricsCollectorMock.Setup(x => x.GetErrorStatsAsync())
                .ReturnsAsync(errorStats);

            // Act
            var result = await _controller.GetRealtimeStatus();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);

            var status = okResult.Value as RealtimeMonitoringStatus;
            status.Should().NotBeNull();
            status!.IsHealthy.Should().BeTrue();
            status.RequestsPerMinute.Should().Be(200);
            status.AverageResponseTimeMs.Should().Be(150);
            status.ErrorRate.Should().Be(0.02);
            status.SuccessRate.Should().Be(0.98);
            status.StatusLevel.Should().Be("Healthy");
            status.ActiveAlertsCount.Should().Be(0);
        }

        [Fact]
        public async Task GetRealtimeStatus_WhenSystemUnhealthy_ShouldReturnUnhealthyStatus()
        {
            // Arrange
            var apiStats = new ApiPerformanceStats
            {
                RequestsPerMinute = 200,
                AverageResponseTime = TimeSpan.FromMilliseconds(3000), // 高响应时间
                SuccessRate = 0.88
            };

            var errorStats = new ErrorStats
            {
                ErrorRate = 0.12 // 高于10%，危急
            };

            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ReturnsAsync(apiStats);
            _metricsCollectorMock.Setup(x => x.GetErrorStatsAsync())
                .ReturnsAsync(errorStats);

            // Act
            var result = await _controller.GetRealtimeStatus();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var status = okResult!.Value as RealtimeMonitoringStatus;
            status.Should().NotBeNull();
            status!.IsHealthy.Should().BeFalse();
            status.ErrorRate.Should().Be(0.12);
            status.StatusLevel.Should().Be("Critical");
            status.ActiveAlertsCount.Should().BeGreaterThan(0);
        }

        #endregion

        #region GetActiveAlerts Tests

        [Fact]
        public async Task GetActiveAlerts_WithNoIssues_ShouldReturnEmptyList()
        {
            // Arrange
            var apiStats = new ApiPerformanceStats
            {
                AverageResponseTime = TimeSpan.FromMilliseconds(150),
                RequestsPerMinute = 100
            };

            var errorStats = new ErrorStats
            {
                ErrorRate = 0.01 // 低错误率
            };

            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ReturnsAsync(apiStats);
            _metricsCollectorMock.Setup(x => x.GetErrorStatsAsync())
                .ReturnsAsync(errorStats);

            // Act
            var result = await _controller.GetActiveAlerts();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var alerts = okResult!.Value as List<MonitoringAlert>;
            alerts.Should().NotBeNull();
            alerts!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetActiveAlerts_WithPerformanceIssues_ShouldReturnPerformanceAlerts()
        {
            // Arrange
            var apiStats = new ApiPerformanceStats
            {
                AverageResponseTime = TimeSpan.FromMilliseconds(3000), // 慢响应
                RequestsPerMinute = 1500 // 高请求量
            };

            var errorStats = new ErrorStats
            {
                ErrorRate = 0.08 // 高错误率
            };

            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ReturnsAsync(apiStats);
            _metricsCollectorMock.Setup(x => x.GetErrorStatsAsync())
                .ReturnsAsync(errorStats);

            // Act
            var result = await _controller.GetActiveAlerts();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var alerts = okResult!.Value as List<MonitoringAlert>;
            alerts.Should().NotBeNull();
            alerts!.Should().HaveCountGreaterThan(0);
            
            // 应该有响应时间告警
            alerts.Should().Contain(a => a.Id == "slow_response_time" && a.Level == AlertLevel.Warning);
            
            // 应该有错误率告警
            alerts.Should().Contain(a => a.Id == "high_error_rate" && a.Level == AlertLevel.Warning);
            
            // 应该有请求量告警
            alerts.Should().Contain(a => a.Id == "high_request_volume" && a.Level == AlertLevel.Info);
        }

        [Fact]
        public async Task GetActiveAlerts_WithCriticalIssues_ShouldReturnCriticalAlerts()
        {
            // Arrange
            var apiStats = new ApiPerformanceStats
            {
                AverageResponseTime = TimeSpan.FromMilliseconds(6000), // 非常慢的响应
                RequestsPerMinute = 100
            };

            var errorStats = new ErrorStats
            {
                ErrorRate = 0.15 // 非常高的错误率
            };

            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ReturnsAsync(apiStats);
            _metricsCollectorMock.Setup(x => x.GetErrorStatsAsync())
                .ReturnsAsync(errorStats);

            // Act
            var result = await _controller.GetActiveAlerts();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();

            var alerts = okResult!.Value as List<MonitoringAlert>;
            alerts.Should().NotBeNull();
            
            // 应该有关键级别的告警
            alerts.Should().Contain(a => a.Level == AlertLevel.Critical);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task AllEndpoints_WhenUnexpectedExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var exception = new Exception("未预期的错误");
            _metricsCollectorMock.Setup(x => x.GetApiPerformanceStatsAsync())
                .ThrowsAsync(exception);

            // Act
            await _controller.GetApiPerformanceStats();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }

    #region Test Data Models

    /// <summary>
    /// API性能统计
    /// </summary>
    public class ApiPerformanceStats
    {
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long FailedRequests { get; set; }
        public double SuccessRate { get; set; }
        public double RequestsPerMinute { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan MaxResponseTime { get; set; }
        public TimeSpan MinResponseTime { get; set; }
        public TimeSpan P95ResponseTime { get; set; }
        public TimeSpan P99ResponseTime { get; set; }
    }

    /// <summary>
    /// 错误统计
    /// </summary>
    public class ErrorStats
    {
        public long TotalErrors { get; set; }
        public double ErrorRate { get; set; }
        public Dictionary<string, int> ErrorsByType { get; set; } = new();
        public Dictionary<string, int> ErrorsByEndpoint { get; set; } = new();
        public List<ErrorDetail> RecentErrors { get; set; } = new();
    }

    /// <summary>
    /// 错误详情
    /// </summary>
    public class ErrorDetail
    {
        public string Message { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// API端点统计
    /// </summary>
    public class ApiEndpointStats
    {
        public string Endpoint { get; set; } = string.Empty;
        public long RequestCount { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
    }

    /// <summary>
    /// 系统性能趋势
    /// </summary>
    public class SystemPerformanceTrend
    {
        public string Period { get; set; } = string.Empty;
        public List<PerformanceDataPoint> DataPoints { get; set; } = new();
        public string TrendDirection { get; set; } = string.Empty;
        public double AverageRequestRate { get; set; }
        public double AverageResponseTime { get; set; }
    }

    /// <summary>
    /// 性能数据点
    /// </summary>
    public class PerformanceDataPoint
    {
        public DateTime Timestamp { get; set; }
        public long RequestCount { get; set; }
        public double AverageResponseTimeMs { get; set; }
    }

    #endregion
}