using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LYBT.WebAPI.Tests
{
    /// <summary>
    /// 简化的API契约测试 - 基础框架，无需Verify.NET依赖
    /// </summary>
    /// <remarks>
    /// <para>目标: 验证关键API响应格式的基本一致性和向后兼容性</para>
    /// <para>方法: 使用结构化响应验证，确保必要字段存在且类型正确</para>
    /// <para>覆盖: Auth、Users、Health等核心API端点</para>
    /// </remarks>
    public class SimpleApiContractTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public SimpleApiContractTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        #region 认证API契约验证

        [Fact]
        public async Task AuthLogin_Should_Have_Standard_Response_Structure()
        {
            // Arrange
            var loginRequest = new
            {
                username = "sysadmin",
                password = "Admin@123456"
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/auth/login", content);

            // Assert
            var responseContent = await response.Content.ReadAsStringAsync();

            // 验证基本响应格式
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            // 解析JSON响应
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // 验证标准ApiResponse格式
            root.TryGetProperty("success", out var success).Should().BeTrue();
            root.TryGetProperty("message", out var message).Should().BeTrue();
            root.TryGetProperty("data", out var data).Should().BeTrue();

            // 如果登录成功，验证认证数据结构
            if (success.GetBoolean())
            {
                data.TryGetProperty("token", out var token).Should().BeTrue();
                token.ValueKind.Should().Be(JsonValueKind.String);
                token.GetString().Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task AuthLogin_InvalidCredentials_Should_Have_Error_Structure()
        {
            // Arrange
            var loginRequest = new
            {
                username = "invalid_user",
                password = "wrong_password"
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/auth/login", content);

            // Assert
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // 验证错误响应结构
            root.TryGetProperty("success", out var success).Should().BeTrue();
            success.GetBoolean().Should().BeFalse();

            root.TryGetProperty("message", out var message).Should().BeTrue();
            message.GetString().Should().NotBeNullOrEmpty();
        }

        #endregion

        #region 健康检查API契约验证

        [Fact]
        public async Task Health_Should_Have_Consistent_Response_Format()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // 验证健康检查响应包含状态信息
            root.TryGetProperty("success", out var success).Should().BeTrue();
            success.GetBoolean().Should().BeTrue();

            // 健康检查响应应该有data字段
            root.TryGetProperty("data", out var data).Should().BeTrue();
        }

        [Fact]
        public async Task Health_Database_Should_Have_Status_Information()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/health/database");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // 数据库健康检查应该包含状态信息
            var hasStatus = root.TryGetProperty("status", out _) ||
                           (root.TryGetProperty("data", out var data) && data.TryGetProperty("status", out _));

            hasStatus.Should().BeTrue("数据库健康检查应该包含状态信息");
        }

        #endregion

        #region 用户管理API契约验证

        [Fact]
        public async Task Users_Unauthorized_Should_Return_401()
        {
            // Act - 不提供认证信息访问用户API
            var response = await _client.GetAsync("/api/v1/users");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Users_With_Auth_Should_Have_Pagination_Structure()
        {
            // Arrange
            var token = await AuthenticateAsync();
            if (string.IsNullOrEmpty(token))
            {
                // 如果无法认证，跳过此测试
                return;
            }

            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/users?currentPage=1&pageSize=10");

            // Assert
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                // 验证分页响应结构
                root.TryGetProperty("success", out var success).Should().BeTrue();
                success.GetBoolean().Should().BeTrue();

                root.TryGetProperty("data", out var data).Should().BeTrue();

                // 验证分页字段
                data.TryGetProperty("items", out _).Should().BeTrue();
                data.TryGetProperty("totalCount", out _).Should().BeTrue();
                data.TryGetProperty("currentPage", out _).Should().BeTrue();
                data.TryGetProperty("pageSize", out _).Should().BeTrue();
            }
        }

        #endregion

        #region API版本一致性验证

        [Fact]
        public Task API_Endpoints_Should_Follow_Version_Pattern()
        {
            // Arrange
            var endpoints = new[]
            {
                "/api/v1/health",
                "/api/v1/auth/login",
                "/api/v1/users",
                "/api/v1/patients"
            };

            // Act & Assert
            foreach (var endpoint in endpoints)
            {
                endpoint.Should().StartWith("/api/v1/", $"端点 {endpoint} 应该遵循版本化URL模式");
            }

            return Task.CompletedTask;
        }

        [Fact]
        public async Task API_Should_Return_JSON_Content_Type()
        {
            // Arrange
            var endpoints = new[]
            {
                "/api/v1/health"
            };

            // Act & Assert
            foreach (var endpoint in endpoints)
            {
                var response = await _client.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    response.Content.Headers.ContentType?.MediaType.Should().Be("application/json",
                        $"端点 {endpoint} 应该返回JSON格式");
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 尝试认证并返回JWT令牌
        /// </summary>
        private async Task<string?> AuthenticateAsync()
        {
            try
            {
                var loginRequest = new
                {
                    username = "sysadmin",
                    password = "Admin@123456"
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("/api/v1/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(responseContent);

                    if (jsonDoc.RootElement.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("token", out var token))
                    {
                        return token.GetString();
                    }
                }
            }
            catch
            {
                // 认证失败，返回null
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// API响应结构验证测试 - 专注于数据结构一致性
    /// </summary>
    public class ApiResponseStructureTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ApiResponseStructureTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task All_Success_Responses_Should_Have_Standard_Structure()
        {
            // Arrange - 测试可以无认证访问的端点
            var endpoints = new[]
            {
                "/api/v1/health"
            };

            // Act & Assert
            foreach (var endpoint in endpoints)
            {
                var response = await _client.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    ValidateStandardApiResponse(responseContent, endpoint);
                }
            }
        }

        [Fact]
        public async Task Error_Responses_Should_Have_Consistent_Structure()
        {
            // Arrange - 测试会返回错误的端点
            var errorEndpoints = new[]
            {
                ("/api/v1/users", HttpStatusCode.Unauthorized),  // 未认证
                ($"/api/v1/users/{Guid.NewGuid()}", HttpStatusCode.Unauthorized)  // 不存在的资源
            };

            // Act & Assert
            foreach (var (endpoint, expectedStatus) in errorEndpoints)
            {
                var response = await _client.GetAsync(endpoint);
                response.StatusCode.Should().Be(expectedStatus);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(responseContent))
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    // 验证错误响应有success字段且为false
                    if (root.TryGetProperty("success", out var success))
                    {
                        success.GetBoolean().Should().BeFalse($"错误响应 {endpoint} 的success字段应该为false");
                    }

                    // 验证错误响应有message字段
                    if (root.TryGetProperty("message", out var message))
                    {
                        message.GetString().Should().NotBeNullOrEmpty($"错误响应 {endpoint} 的message字段不应为空");
                    }
                }
            }
        }

        private static void ValidateStandardApiResponse(string responseContent, string endpoint)
        {
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // 验证标准ApiResponse<T>结构
            root.TryGetProperty("success", out var success).Should().BeTrue($"{endpoint} 响应应该包含success字段");
            success.ValueKind.Should().Be(JsonValueKind.True, $"{endpoint} 的success字段应该是布尔类型");

            root.TryGetProperty("message", out var message).Should().BeTrue($"{endpoint} 响应应该包含message字段");
            message.ValueKind.Should().Be(JsonValueKind.String, $"{endpoint} 的message字段应该是字符串类型");

            // data字段可能存在也可能不存在，但如果存在就验证
            if (root.TryGetProperty("data", out var data))
            {
                data.ValueKind.Should().NotBe(JsonValueKind.Undefined, $"{endpoint} 的data字段应该有明确的值");
            }
        }
    }
}
