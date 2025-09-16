using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LYBT.WebAPI.Controllers;
using LYBT.WebAPI.Services;
using FluentAssertions;
using System.Reflection;

namespace LYBT.WebAPI.Tests.Controllers
{
    /// <summary>
    /// HealthController 单元测试
    /// </summary>
    public class HealthControllerTests
    {
        private readonly Mock<ISystemHealthService> _healthServiceMock;
        private readonly Mock<ILogger<HealthController>> _loggerMock;
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            _healthServiceMock = new Mock<ISystemHealthService>();
            _loggerMock = new Mock<ILogger<HealthController>>();

            _controller = new HealthController(
                _healthServiceMock.Object,
                _loggerMock.Object
            );
        }

        #region Get Tests

        [Fact]
        public async Task Get_ShouldReturnHealthyStatus()
        {
            // Arrange
            var expectedHealth = new SystemHealthStatus
            {
                Status = "Healthy",
                IsHealthy = true,
                TotalChecks = 3,
                HealthyChecks = 3,
                UnhealthyChecks = 0,
                CheckDuration = TimeSpan.FromMilliseconds(150),
                Timestamp = DateTime.UtcNow
            };

            _healthServiceMock.Setup(x => x.GetOverallHealthAsync())
                .ReturnsAsync(expectedHealth);

            // Act
            var result = await _controller.Get();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            var healthStatus = actionResult.Value;
            healthStatus.Should().NotBeNull();
        }

        [Fact]
        public async Task Get_WhenSystemUnhealthy_ShouldReturnServiceUnavailable()
        {
            // Arrange
            var expectedHealth = new SystemHealthStatus
            {
                Status = "Unhealthy",
                IsHealthy = false,
                TotalChecks = 3,
                HealthyChecks = 1,
                UnhealthyChecks = 2,
                CheckDuration = TimeSpan.FromMilliseconds(250),
                Timestamp = DateTime.UtcNow
            };

            _healthServiceMock.Setup(x => x.GetOverallHealthAsync())
                .ReturnsAsync(expectedHealth);

            // Act
            var result = await _controller.Get();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as ObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(503); // Service Unavailable
        }

        #endregion

        #region GetDetailed Tests

        [Fact]
        public async Task GetDetailed_ShouldReturnDetailedHealthStatus()
        {
            // Arrange
            var expectedHealth = new SystemHealthStatus
            {
                Status = "Healthy",
                IsHealthy = true,
                TotalChecks = 3,
                HealthyChecks = 3,
                UnhealthyChecks = 0,
                CheckDuration = TimeSpan.FromMilliseconds(150),
                Timestamp = DateTime.UtcNow,
                Components = new Dictionary<string, ComponentHealthStatus>
                {
                    ["Database"] = new ComponentHealthStatus
                    {
                        Status = "Healthy",
                        IsHealthy = true,
                        ResponseTime = TimeSpan.FromMilliseconds(50),
                        Description = "数据库连接正常"
                    }
                }
            };

            _healthServiceMock.Setup(x => x.GetOverallHealthAsync())
                .ReturnsAsync(expectedHealth);

            // Act
            var result = await _controller.GetDetailed();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            var healthStatus = actionResult.Value as SystemHealthStatus;
            healthStatus.Should().NotBeNull();
            healthStatus!.IsHealthy.Should().BeTrue();
            healthStatus.Components.Should().ContainKey("Database");
        }

        #endregion

        #region GetDatabaseHealth Tests

        [Fact]
        public async Task GetDatabaseHealth_ShouldReturnDatabaseStatus()
        {
            // Arrange
            var expectedDbHealth = new ComponentHealthStatus
            {
                Status = "Healthy",
                IsHealthy = true,
                ResponseTime = TimeSpan.FromMilliseconds(25),
                Description = "数据库连接正常",
                Data = new Dictionary<string, object>
                {
                    ["ServerVersion"] = "SQL Server 2022",
                    ["ConnectionCount"] = 5
                }
            };

            _healthServiceMock.Setup(x => x.GetDatabaseHealthAsync())
                .ReturnsAsync(expectedDbHealth);

            // Act
            var result = await _controller.GetDatabaseHealth();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            var dbHealth = actionResult.Value as ComponentHealthStatus;
            dbHealth.Should().NotBeNull();
            dbHealth!.IsHealthy.Should().BeTrue();
            dbHealth.Data.Should().ContainKey("ServerVersion");
        }

        #endregion

        #region GetSystemResources Tests

        [Fact]
        public async Task GetSystemResources_ShouldReturnResourceStatus()
        {
            // Arrange
            var expectedResources = new SystemResourceStatus
            {
                CpuUsagePercent = 25.5,
                MemoryUsedMB = 1024,
                MemoryTotalMB = 4096,
                MemoryUsagePercent = 25.0,
                DiskUsedGB = 50,
                DiskTotalGB = 500,
                DiskUsagePercent = 10.0,
                Timestamp = DateTime.UtcNow
            };

            _healthServiceMock.Setup(x => x.GetSystemResourcesAsync())
                .ReturnsAsync(expectedResources);

            // Act
            var result = await _controller.GetSystemResources();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            var resources = actionResult.Value as SystemResourceStatus;
            resources.Should().NotBeNull();
            resources!.CpuUsagePercent.Should().Be(25.5);
            resources.MemoryUsagePercent.Should().Be(25.0);
        }

        #endregion

        #region GetApplicationMetrics Tests

        [Fact]
        public async Task GetApplicationMetrics_ShouldReturnMetrics()
        {
            // Arrange
            var expectedMetrics = new ApplicationMetrics
            {
                RequestCount = 1000,
                ErrorCount = 5,
                AverageResponseTime = TimeSpan.FromMilliseconds(200),
                ActiveConnections = 50,
                TotalMemoryUsed = 512 * 1024 * 1024, // 512 MB
                StartTime = DateTime.UtcNow.AddHours(-2),
                Uptime = TimeSpan.FromHours(2)
            };

            _healthServiceMock.Setup(x => x.GetApplicationMetricsAsync())
                .ReturnsAsync(expectedMetrics);

            // Act
            var result = await _controller.GetApplicationMetrics();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            var metrics = actionResult.Value as ApplicationMetrics;
            metrics.Should().NotBeNull();
            metrics!.RequestCount.Should().Be(1000);
            metrics.ErrorCount.Should().Be(5);
            metrics.Uptime.Should().BeCloseTo(TimeSpan.FromHours(2), TimeSpan.FromMinutes(1));
        }

        #endregion

        #region Kubernetes Probes Tests

        [Fact]
        public async Task GetReadiness_WhenReady_ShouldReturnOk()
        {
            // Arrange
            var readinessStatus = new ReadinessStatus
            {
                IsReady = true,
                ReadyComponents = new[] { "Database", "Cache", "ExternalService" }.ToList(),
                NotReadyComponents = new List<string>(),
                Message = "所有组件就绪"
            };

            _healthServiceMock.Setup(x => x.GetReadinessAsync())
                .ReturnsAsync(readinessStatus);

            // Act
            var result = await _controller.GetReadiness();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetReadiness_WhenNotReady_ShouldReturnServiceUnavailable()
        {
            // Arrange
            var readinessStatus = new ReadinessStatus
            {
                IsReady = false,
                ReadyComponents = new[] { "Database" }.ToList(),
                NotReadyComponents = new[] { "Cache", "ExternalService" }.ToList(),
                Message = "部分组件未就绪"
            };

            _healthServiceMock.Setup(x => x.GetReadinessAsync())
                .ReturnsAsync(readinessStatus);

            // Act
            var result = await _controller.GetReadiness();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as ObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(503);
        }

        [Fact]
        public void GetLiveness_ShouldAlwaysReturnOk()
        {
            // Act
            var result = _controller.GetLiveness();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetStartupStatus_WhenStarted_ShouldReturnOk()
        {
            // Arrange
            var startupStatus = new StartupStatus
            {
                IsStarted = true,
                StartupDuration = TimeSpan.FromSeconds(30),
                CompletedInitializations = new[] { "Database", "Configuration", "Services" }.ToList(),
                FailedInitializations = new List<string>(),
                Message = "应用程序启动完成"
            };

            _healthServiceMock.Setup(x => x.GetStartupStatusAsync())
                .ReturnsAsync(startupStatus);

            // Act
            var result = await _controller.GetStartupStatus();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetStartupStatus_WhenNotStarted_ShouldReturnServiceUnavailable()
        {
            // Arrange
            var startupStatus = new StartupStatus
            {
                IsStarted = false,
                StartupDuration = TimeSpan.FromSeconds(60),
                CompletedInitializations = new[] { "Database" }.ToList(),
                FailedInitializations = new[] { "ExternalService" }.ToList(),
                Message = "应用程序启动中"
            };

            _healthServiceMock.Setup(x => x.GetStartupStatusAsync())
                .ReturnsAsync(startupStatus);

            // Act
            var result = await _controller.GetStartupStatus();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as ObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(503);
        }

        #endregion

        #region GetVersion Tests

        [Fact]
        public void GetVersion_ShouldReturnVersionInformation()
        {
            // Act
            var result = _controller.GetVersion();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            var versionInfo = actionResult.Value;
            versionInfo.Should().NotBeNull();

            // 验证返回的版本信息包含基本字段
            var versionType = versionInfo!.GetType();
            versionType.GetProperty("Version")?.GetValue(versionInfo).Should().NotBeNull();
            versionType.GetProperty("BuildDate")?.GetValue(versionInfo).Should().NotBeNull();
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task Get_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            _healthServiceMock.Setup(x => x.GetOverallHealthAsync())
                .ThrowsAsync(new Exception("健康检查服务异常"));

            // Act
            var result = await _controller.Get();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as ObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetDatabaseHealth_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            _healthServiceMock.Setup(x => x.GetDatabaseHealthAsync())
                .ThrowsAsync(new InvalidOperationException("数据库连接失败"));

            // Act
            var result = await _controller.GetDatabaseHealth();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as ObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(500);
        }

        #endregion
    }

    #region Test Data Models

    /// <summary>
    /// 系统健康状态
    /// </summary>
    public class SystemHealthStatus
    {
        public string Status { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public int TotalChecks { get; set; }
        public int HealthyChecks { get; set; }
        public int UnhealthyChecks { get; set; }
        public TimeSpan CheckDuration { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, ComponentHealthStatus> Components { get; set; } = new();
    }

    /// <summary>
    /// 组件健康状态
    /// </summary>
    public class ComponentHealthStatus
    {
        public string Status { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// 系统资源状态
    /// </summary>
    public class SystemResourceStatus
    {
        public double CpuUsagePercent { get; set; }
        public long MemoryUsedMB { get; set; }
        public long MemoryTotalMB { get; set; }
        public double MemoryUsagePercent { get; set; }
        public long DiskUsedGB { get; set; }
        public long DiskTotalGB { get; set; }
        public double DiskUsagePercent { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 应用程序指标
    /// </summary>
    public class ApplicationMetrics
    {
        public long RequestCount { get; set; }
        public long ErrorCount { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public int ActiveConnections { get; set; }
        public long TotalMemoryUsed { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    /// <summary>
    /// 就绪状态
    /// </summary>
    public class ReadinessStatus
    {
        public bool IsReady { get; set; }
        public List<string> ReadyComponents { get; set; } = new();
        public List<string> NotReadyComponents { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 启动状态
    /// </summary>
    public class StartupStatus
    {
        public bool IsStarted { get; set; }
        public TimeSpan StartupDuration { get; set; }
        public List<string> CompletedInitializations { get; set; } = new();
        public List<string> FailedInitializations { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 详细健康报告
    /// </summary>
    public class DetailedHealthReport
    {
        public SystemHealthStatus OverallHealth { get; set; } = new();
        public SystemResourceStatus Resources { get; set; } = new();
        public ApplicationMetrics Metrics { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    #endregion
}