using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace LYBT.Tests.IntegrationTests.Controllers
{
    /// <summary>
    /// HealthController集成测试 - 100%方法覆盖率
    /// 符合PRD要求：使用SQL Server进行集成测试
    /// </summary>
    public class HealthControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public HealthControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region 辅助方法

        /// <summary>
        /// 获取认证的HTTP客户端
        /// </summary>
        private async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            await _factory.InitializeTestDatabaseAsync();

            var loginRequest = new
            {
                Username = "sysadmin",
                Password = "LybtAdmin2025@SecurePass!"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            loginResponse.EnsureSuccessStatusCode();

            // 从响应头获取token
            if (loginResponse.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                var token = authHeaders.First();
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return _client;
        }

        #endregion

        #region 1. Get 测试 - GET /api/v1/health

        [Fact]
        public async Task Get_BasicHealthCheck_ShouldReturnHealthyStatus()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var healthData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            healthData.GetProperty("status").GetString().Should().Be("Healthy");
            healthData.TryGetProperty("timestamp", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Get_InProductionEnvironment_ShouldReturnMinimalInfo()
        {
            // Arrange
            var productionFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });

            using var productionClient = productionFactory.CreateClient();

            // Act
            var response = await productionClient.GetAsync("/api/v1/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var healthData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            healthData.GetProperty("status").GetString().Should().Be("Healthy");
            healthData.GetProperty("timestamp").ValueKind.Should().Be(JsonValueKind.String);

            // 生产环境不应包含版本和环境信息
            healthData.TryGetProperty("version", out _).Should().BeFalse();
            healthData.TryGetProperty("environment", out _).Should().BeFalse();
        }

        [Fact]
        public async Task Get_InDevelopmentEnvironment_ShouldReturnDetailedInfo()
        {
            // Arrange
            var developmentFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

            using var developmentClient = developmentFactory.CreateClient();

            // Act
            var response = await developmentClient.GetAsync("/api/v1/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var healthData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            healthData.GetProperty("status").GetString().Should().Be("Healthy");
            healthData.GetProperty("timestamp").ValueKind.Should().Be(JsonValueKind.String);

            // 开发环境应包含版本和环境信息
            healthData.TryGetProperty("version", out _).Should().BeTrue();
            healthData.TryGetProperty("environment", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Get_AllowsAnonymousAccess()
        {
            // Arrange - 使用未认证的客户端
            using var anonymousClient = _factory.CreateClient();

            // Act
            var response = await anonymousClient.GetAsync("/api/v1/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region 2. Ping 测试 - GET /api/v1/health/ping

        [Fact]
        public async Task Ping_ShouldReturnPongResponse()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/health/ping");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var pingData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            pingData.GetProperty("message").GetString().Should().Be("pong");
            pingData.TryGetProperty("timestamp", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Ping_AllowsAnonymousAccess()
        {
            // Arrange - 使用未认证的客户端
            using var anonymousClient = _factory.CreateClient();

            // Act
            var response = await anonymousClient.GetAsync("/api/v1/health/ping");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var pingData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            pingData.GetProperty("message").GetString().Should().Be("pong");
        }

        [Fact]
        public async Task Ping_MultipleConcurrentRequests_ShouldAllSucceed()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - 发送10个并发Ping请求
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client.GetAsync("/api/v1/health/ping"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadAsStringAsync();
                var pingData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
                pingData.GetProperty("message").GetString().Should().Be("pong");
            }
        }

        #endregion

        #region 3. GetDetailedHealth 测试 - GET /api/v1/health/details

        [Fact]
        public async Task GetDetailedHealth_WithAuthentication_ShouldReturnDetailedStatus()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            // Act
            var response = await client.GetAsync("/api/v1/health/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var healthData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            healthData.GetProperty("status").GetString().Should().BeOneOf("Healthy", "Degraded");
            healthData.TryGetProperty("checks", out var checks).Should().BeTrue();
            checks.ValueKind.Should().Be(JsonValueKind.Array);
            checks.GetArrayLength().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetDetailedHealth_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange - 使用未认证的客户端
            using var anonymousClient = _factory.CreateClient();

            // Act
            var response = await anonymousClient.GetAsync("/api/v1/health/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDetailedHealth_InDevelopmentEnvironment_ShouldReturnFullDetails()
        {
            // Arrange
            var developmentFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

            using var developmentClient = developmentFactory.CreateClient();

            // 认证开发环境的客户端
            var loginRequest = new
            {
                Username = "sysadmin",
                Password = "LybtAdmin2025@SecurePass!"
            };

            await _factory.InitializeTestDatabaseAsync();
            var loginResponse = await developmentClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            if (loginResponse.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                var token = authHeaders.First();
                developmentClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            // Act
            var response = await developmentClient.GetAsync("/api/v1/health/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var healthData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            // 开发环境应包含完整信息
            healthData.TryGetProperty("uptimeMs", out _).Should().BeTrue();
            healthData.TryGetProperty("nowUtc", out _).Should().BeTrue();
            healthData.TryGetProperty("checks", out var checks).Should().BeTrue();

            // 检查是否包含详细的检查信息
            if (checks.ValueKind == JsonValueKind.Array && checks.GetArrayLength() > 0)
            {
                var firstCheck = checks[0];
                firstCheck.TryGetProperty("name", out _).Should().BeTrue();
                firstCheck.TryGetProperty("status", out _).Should().BeTrue();
                firstCheck.TryGetProperty("description", out _).Should().BeTrue();
                firstCheck.TryGetProperty("duration", out _).Should().BeTrue();
            }
        }

        [Fact]
        public async Task GetDetailedHealth_InProductionEnvironment_ShouldReturnMinimalDetails()
        {
            // Arrange
            var productionFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });

            using var productionClient = productionFactory.CreateClient();

            // 认证生产环境的客户端
            var loginRequest = new
            {
                Username = "sysadmin",
                Password = "LybtAdmin2025@SecurePass!"
            };

            await _factory.InitializeTestDatabaseAsync();
            var loginResponse = await productionClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            if (loginResponse.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                var token = authHeaders.First();
                productionClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            // Act
            var response = await productionClient.GetAsync("/api/v1/health/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var healthData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            // 生产环境应只包含基本信息
            healthData.GetProperty("status").ValueKind.Should().Be(JsonValueKind.String);
            healthData.GetProperty("timestamp").ValueKind.Should().Be(JsonValueKind.String);
            healthData.TryGetProperty("checks", out var checks).Should().BeTrue();

            // 生产环境不应包含详细信息
            healthData.TryGetProperty("uptimeMs", out _).Should().BeFalse();

            // 检查项应该是简化的
            if (checks.ValueKind == JsonValueKind.Array && checks.GetArrayLength() > 0)
            {
                var firstCheck = checks[0];
                firstCheck.TryGetProperty("name", out _).Should().BeTrue();
                firstCheck.TryGetProperty("status", out _).Should().BeTrue();
                // 生产环境不应包含详细字段
                firstCheck.TryGetProperty("description", out _).Should().BeFalse();
                firstCheck.TryGetProperty("data", out _).Should().BeFalse();
            }
        }

        [Fact]
        public async Task GetDetailedHealth_WithDatabaseConnection_ShouldIncludeDatabaseCheck()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            // Act
            var response = await client.GetAsync("/api/v1/health/details");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var healthData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            healthData.TryGetProperty("checks", out var checks).Should().BeTrue();

            // 在开发环境中应该包含数据库检查
            var checksArray = checks.EnumerateArray().ToArray();
            var hasDbCheck = checksArray.Any(check =>
            {
                if (check.TryGetProperty("name", out var name))
                {
                    return name.GetString() == "db";
                }
                return false;
            });
            hasDbCheck.Should().BeTrue();
        }

        #endregion

        #region 边界和异常测试

        [Fact]
        public async Task HealthController_WithInvalidRoute_ShouldReturnNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/health/invalid");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task HealthController_WithUnsupportedMethod_ShouldReturnMethodNotAllowed()
        {
            // Act
            var response = await _client.PostAsync("/api/v1/health", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public async Task HealthController_ResponseTime_ShouldBeReasonable()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync("/api/v1/health/ping");
            stopwatch.Stop();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5秒内完成
        }

        [Fact]
        public async Task HealthController_HighFrequencyRequests_ShouldNotDegradePerformance()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - 发送50个快速请求
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(_client.GetAsync("/api/v1/health/ping"));
            }

            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // 50个请求应该在合理时间内完成
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // 30秒内完成
        }

        #endregion

        public async ValueTask DisposeAsync()
        {
            await _factory.CleanupTestDatabaseAsync();
            _client?.Dispose();
        }
    }
}
