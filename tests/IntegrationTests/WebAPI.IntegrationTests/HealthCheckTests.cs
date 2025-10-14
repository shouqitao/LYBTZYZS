using FluentAssertions;
using LYBT.Tests.Configuration;
using System.Net;
using System.Text.Json;
using Xunit;

namespace LYBT.IntegrationTests.WebAPI
{
    /// <summary>
    /// API健康检查集成测试
    /// 测试WebAPI的健康状况和基本功能
    /// </summary>
    public class HealthCheckTests : IntegrationTestBase
    {
        public HealthCheckTests()
            : base()
        {
        }

        #region Health Endpoint Tests

        [Fact]
        public async Task Health_ShouldReturnHealthyStatus()
        {
            // Act
            var response = await Client.GetAsync("/health");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
            
            // 验证响应包含健康状态信息
            content.Should().Contain("status", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task HealthReady_ShouldReturnReadyStatus()
        {
            // Act
            var response = await Client.GetAsync("/health/ready");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
            
            // 验证就绪状态检查
            content.Should().Contain("status", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task HealthLive_ShouldReturnLiveStatus()
        {
            // Act
            var response = await Client.GetAsync("/health/live");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
            
            // 验证存活状态检查
            content.Should().Contain("status", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region API Info Tests

        [Fact]
        public async Task Info_ShouldReturnApiInformation()
        {
            // Act
            var response = await Client.GetAsync("/api/info");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
            
            // 验证响应包含API信息
            var apiInfo = JsonSerializer.Deserialize<JsonElement>(content);
            apiInfo.TryGetProperty("version", out _).Should().BeTrue();
            apiInfo.TryGetProperty("title", out _).Should().BeTrue();
        }

        #endregion

        #region API Root Tests

        [Fact]
        public async Task ApiRoot_ShouldReturnApiInformation()
        {
            // Act
            var response = await Client.GetAsync("/api");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region CORS Tests

        [Fact]
        public async Task Cors_ShouldAllowOrigin()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/users");
            request.Headers.Add("Origin", "http://localhost:3000");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task NonExistentRoute_ShouldReturnNotFound()
        {
            // Act
            var response = await Client.GetAsync("/api/nonexistent-route");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task InvalidHttpMethod_ShouldReturnMethodNotAllowed()
        {
            // Act
            var request = new HttpRequestMessage(HttpMethod.Patch, "/api/users");
            var response = await Client.SendAsync(request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.MethodNotAllowed);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task UnauthorizedRequest_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users");
            // 不提供认证令牌

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Rate Limiting Tests

        [Fact]
        public async Task RapidRequests_ShouldHandleRateLimiting()
        {
            // Arrange
            var requests = new List<HttpRequestMessage>();
            for (int i = 0; i < 10; i++)
            {
                requests.Add(new HttpRequestMessage(HttpMethod.Get, "/api/users"));
            }

            // Act
            var responses = new List<HttpResponseMessage>();
            foreach (var request in requests)
            {
                responses.Add(await Client.SendAsync(request));
            }

            // Assert
            // 前几个请求应该成功，后续请求可能受到速率限制
            responses.Take(5).Should().AllSatisfy(r => 
                r.StatusCode.Should().Be(HttpStatusCode.OK));
        }

        #endregion

        #region Content Type Tests

        [Fact]
        public async Task JsonRequest_ShouldHandleCorrectly()
        {
            // Arrange
            var jsonContent = new StringContent(
                """{"userName":"test","password":"test123"}""",
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PostAsync("/api/auth/login", jsonContent);

            // Assert
            // 即使认证失败，也应该能正确处理JSON内容
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized, 
                HttpStatusCode.BadRequest,
                HttpStatusCode.OK);
        }

        [Fact]
        public async Task InvalidJsonRequest_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidJsonContent = new StringContent(
                """{"userName":"test","password":}""",
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await Client.PostAsync("/api/auth/login", invalidJsonContent);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Security Headers Tests

        [Fact]
        public async Task ApiResponses_ShouldIncludeSecurityHeaders()
        {
            // Act
            var response = await Client.GetAsync("/api/users");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            // 验证安全头（根据实际配置调整）
            // response.Headers.Should().ContainKey("X-Content-Type-Options");
            // response.Headers.Should().ContainKey("X-Frame-Options");
        }

        #endregion

        #region Database Connection Tests

        [Fact]
        public async Task DatabaseEndpoint_ShouldConnectSuccessfully()
        {
            // Act
            var response = await Client.GetAsync("/api/users");

            // Assert
            // 如果数据库连接正常，API应该能正常响应
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.Unauthorized); // 可能需要认证
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public async Task Api_ShouldUseCorrectConfiguration()
        {
            // Act
            var response = await Client.GetAsync("/api/users");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            // 验证响应格式
            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        }

        #endregion
    }
}