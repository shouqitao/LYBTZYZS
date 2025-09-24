using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Caching.Models;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.WebAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LYBT.WebAPI.Tests.Controllers
{
    /// <summary>
    /// CacheHealthController集成测试
    /// </summary>
    public class CacheHealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<ICacheDiagnosticsService> _mockDiagnosticsService;
        private readonly Mock<ICacheService> _mockCacheService;

        public CacheHealthControllerTests(WebApplicationFactory<Program> factory)
        {
            _mockDiagnosticsService = new Mock<ICacheDiagnosticsService>();
            _mockCacheService = new Mock<ICacheService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // 替换真实服务为Mock
                    services.AddSingleton(_mockDiagnosticsService.Object);
                    services.AddSingleton(_mockCacheService.Object);

                    // 配置测试用的CacheOptions
                    services.Configure<CacheOptions>(options =>
                    {
                        options.Monitoring = new CacheOptions.MonitoringConfig
                        {
                            Enabled = true,
                            HitRateThreshold = 0.8,
                            CapacityThreshold = 0.85,
                            EvictionRateThreshold = 100
                        };
                    });
                });
            });
        }

        #region 健康检查端点测试

        [Fact]
        public async Task GetHealth_WithoutAuth_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/system/cache/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetHealth_WithAdminAuth_ReturnsHealthStatus()
        {
            // Arrange
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
                    HasAnyAlert = false,
                    CurrentHitRate = 0.85,
                    HitRateThreshold = 0.8
                },
                SamplingWindowSeconds = 60
            };

            _mockDiagnosticsService.Setup(x => x.GetLatestSnapshot())
                .Returns(snapshot);

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/api/v1/system/cache/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            json["success"]?.Value<bool>().Should().BeTrue();
            json["data"]?["healthLevel"]?.ToString().Should().Be("Healthy");
            json["data"]?["statistics"]?["hitRate"]?.Value<double>().Should().BeApproximately(0.85, 0.01);
        }

        [Fact]
        public async Task GetHealth_WhenNoSnapshot_ReturnsWarning()
        {
            // Arrange
            _mockDiagnosticsService.Setup(x => x.GetLatestSnapshot())
                .Returns((CacheHealthSnapshot)null);

            _mockDiagnosticsService.Setup(x => x.RunDiagnosticsAsync(default))
                .ReturnsAsync(new CacheDiagnosticResult
                {
                    DiagnosticId = Guid.NewGuid().ToString(),
                    DiagnosticTime = DateTime.UtcNow,
                    HealthStatus = new CacheHealthStatus
                    {
                        IsHealthy = true,
                        Level = HealthLevel.Healthy
                    }
                });

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/api/v1/system/cache/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // 验证诊断服务被调用
            _mockDiagnosticsService.Verify(x => x.RunDiagnosticsAsync(default), Times.Once);
        }

        #endregion

        #region 诊断端点测试

        [Fact]
        public async Task RunDiagnostics_WithoutSystemAdmin_ReturnsForbidden()
        {
            // Arrange
            var client = CreateAuthenticatedClient(isSystemAdmin: false);

            // Act
            var response = await client.PostAsync("/api/v1/system/cache/diagnose", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK); // API返回200但包含错误信息
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            json["success"]?.Value<bool>().Should().BeFalse();
            json["message"]?.ToString().Should().Contain("系统管理员权限");
        }

        [Fact]
        public async Task RunDiagnostics_WithSystemAdmin_ReturnsResult()
        {
            // Arrange
            var diagnosticResult = new CacheDiagnosticResult
            {
                DiagnosticId = Guid.NewGuid().ToString(),
                DiagnosticTime = DateTime.UtcNow,
                ElapsedMilliseconds = 150,
                HealthStatus = new CacheHealthStatus
                {
                    IsHealthy = true,
                    Level = HealthLevel.Healthy,
                    Message = "缓存运行正常",
                    Recommendations = new List<string> { "继续监控" }
                },
                Performance = new CachePerformance
                {
                    HitRate = 0.85,
                    AverageResponseTime = 5.2,
                    EvictionRate = 50,
                    Throughput = 1000
                },
                Capacity = new CacheCapacity
                {
                    UsedCapacity = 500,
                    MaxCapacity = 1000,
                    UsageRatio = 0.5,
                    EstimatedTimeToFull = 3600,
                    MemoryUsageBytes = 10 * 1024 * 1024
                }
            };

            _mockDiagnosticsService.Setup(x => x.RunDiagnosticsAsync(default))
                .ReturnsAsync(diagnosticResult);

            var client = CreateAuthenticatedClient(isSystemAdmin: true);

            // Act
            var response = await client.PostAsync("/api/v1/system/cache/diagnose", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            json["success"]?.Value<bool>().Should().BeTrue();
            json["data"]?["diagnosticId"]?.ToString().Should().NotBeNullOrWhiteSpace();
            json["data"]?["healthStatus"]?["isHealthy"]?.Value<bool>().Should().BeTrue();
            json["data"]?["performance"]?["hitRate"]?.Value<double>().Should().BeApproximately(0.85, 0.01);
        }

        #endregion

        #region 历史快照端点测试

        [Fact]
        public async Task GetHistorySnapshots_WithValidCount_ReturnsSnapshots()
        {
            // Arrange
            var snapshots = new List<CacheHealthSnapshot>
            {
                new CacheHealthSnapshot
                {
                    SnapshotId = "1",
                    SnapshotTime = DateTime.UtcNow.AddMinutes(-2),
                    HealthLevel = HealthLevel.Healthy,
                    Statistics = new CacheStatistics { HitCount = 80, MissCount = 20 }
                },
                new CacheHealthSnapshot
                {
                    SnapshotId = "2",
                    SnapshotTime = DateTime.UtcNow.AddMinutes(-1),
                    HealthLevel = HealthLevel.Warning,
                    Statistics = new CacheStatistics { HitCount = 60, MissCount = 40 },
                    ThresholdCheck = new ThresholdCheckResult { HasAnyAlert = true }
                }
            };

            _mockDiagnosticsService.Setup(x => x.GetHistorySnapshots(It.IsAny<int>()))
                .Returns(snapshots);

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/api/v1/system/cache/history?count=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            json["success"]?.Value<bool>().Should().BeTrue();
            var data = json["data"] as JArray;
            data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetHistorySnapshots_WithInvalidCount_ReturnsBadRequest()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/api/v1/system/cache/history?count=0");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            json["success"]?.Value<bool>().Should().BeFalse();
            json["message"]?.ToString().Should().Contain("获取数量");
        }

        #endregion

        #region 统计信息端点测试

        [Fact]
        public async Task GetStatistics_ReturnsStatistics()
        {
            // Arrange
            var statistics = new CacheStatistics
            {
                TotalKeys = 100,
                HitCount = 850,
                MissCount = 150,
                CurrentItemCount = 500,
                MaxCapacity = 1000,
                UsedMemory = 10 * 1024 * 1024,
                TotalMemoryUsage = 15 * 1024 * 1024,
                ExpiredKeys = 20,
                EvictedKeys = 10,
                EvictionCount = 30,
                EvictionRate = 50,
                Timestamp = DateTime.UtcNow
            };

            _mockCacheService.Setup(x => x.GetStatisticsAsync(default))
                .ReturnsAsync(statistics);

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/api/v1/system/cache/statistics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            json["success"]?.Value<bool>().Should().BeTrue();
            json["data"]?["summary"]?["totalKeys"]?.Value<long>().Should().Be(100);
            json["data"]?["summary"]?["hitCount"]?.Value<long>().Should().Be(850);
            json["data"]?["memory"]?["usedMemoryMB"]?.Value<double>().Should().BeApproximately(10.0, 0.1);
        }

        #endregion

        #region 清空缓存端点测试

        [Fact]
        public async Task ClearCache_WithoutSystemAdmin_ReturnsForbidden()
        {
            // Arrange
            var client = CreateAuthenticatedClient(isSystemAdmin: false);

            // Act
            var response = await client.DeleteAsync("/api/v1/system/cache/clear");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            json["success"]?.Value<bool>().Should().BeFalse();
            json["message"]?.ToString().Should().Contain("系统管理员权限");
        }

        [Fact]
        public async Task ClearCache_WithSystemAdmin_ClearsCache()
        {
            // Arrange
            var beforeStats = new CacheStatistics
            {
                TotalKeys = 100,
                UsedMemory = 10 * 1024 * 1024
            };

            var afterStats = new CacheStatistics
            {
                TotalKeys = 0,
                UsedMemory = 0
            };

            _mockCacheService.SetupSequence(x => x.GetStatisticsAsync(default))
                .ReturnsAsync(beforeStats)
                .ReturnsAsync(afterStats);

            var client = CreateAuthenticatedClient(isSystemAdmin: true);

            // Act
            var response = await client.DeleteAsync("/api/v1/system/cache/clear");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            json["success"]?.Value<bool>().Should().BeTrue();
            json["data"]?["clearedItems"]?.Value<long>().Should().Be(100);

            // 验证Clear被调用
            _mockCacheService.Verify(x => x.Clear(), Times.Once);
        }

        #endregion

        #region 按模式清除端点测试

        [Fact]
        public async Task ClearByPattern_WithoutPattern_ReturnsBadRequest()
        {
            // Arrange
            var client = CreateAuthenticatedClient(isSystemAdmin: true);

            // Act
            var response = await client.DeleteAsync("/api/v1/system/cache/clear-pattern");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            json["success"]?.Value<bool>().Should().BeFalse();
            json["message"]?.ToString().Should().Contain("模式参数");
        }

        [Fact]
        public async Task ClearByPattern_WithPattern_RemovesMatchingKeys()
        {
            // Arrange
            _mockCacheService.Setup(x => x.RemoveByPatternAsync("user:*", default))
                .ReturnsAsync(25);

            var client = CreateAuthenticatedClient(isSystemAdmin: true);

            // Act
            var response = await client.DeleteAsync("/api/v1/system/cache/clear-pattern?pattern=user:*");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            json["success"]?.Value<bool>().Should().BeTrue();
            json["data"]?["pattern"]?.ToString().Should().Be("user:*");
            json["data"]?["removedCount"]?.Value<int>().Should().Be(25);
        }

        #endregion

        #region 异常处理测试

        [Fact]
        public async Task GetHealth_WhenServiceThrows_ReturnsError()
        {
            // Arrange
            _mockDiagnosticsService.Setup(x => x.GetLatestSnapshot())
                .Throws(new Exception("诊断服务错误"));

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/api/v1/system/cache/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            json["success"]?.Value<bool>().Should().BeFalse();
            json["message"]?.ToString().Should().Contain("错误");
        }

        [Fact]
        public async Task ClearCache_WhenClearFails_ReturnsError()
        {
            // Arrange
            _mockCacheService.Setup(x => x.GetStatisticsAsync(default))
                .ReturnsAsync(new CacheStatistics { TotalKeys = 100 });

            _mockCacheService.Setup(x => x.Clear())
                .Throws(new Exception("清空失败"));

            var client = CreateAuthenticatedClient(isSystemAdmin: true);

            // Act
            var response = await client.DeleteAsync("/api/v1/system/cache/clear");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            json["success"]?.Value<bool>().Should().BeFalse();
            json["message"]?.ToString().Should().Contain("错误");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 创建带身份认证的客户端
        /// </summary>
        private HttpClient CreateAuthenticatedClient(bool isSystemAdmin = false)
        {
            var client = _factory.CreateClient();

            // 模拟JWT Token
            // 实际测试中应该使用真实的JWT生成逻辑
            var token = GenerateTestToken(isSystemAdmin);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        /// <summary>
        /// 生成测试用Token
        /// </summary>
        private string GenerateTestToken(bool isSystemAdmin)
        {
            // 在实际测试中，这里应该生成真实的JWT Token
            // 这里简化处理，返回一个模拟token
            return isSystemAdmin ? "test-admin-token" : "test-user-token";
        }

        #endregion
    }
}