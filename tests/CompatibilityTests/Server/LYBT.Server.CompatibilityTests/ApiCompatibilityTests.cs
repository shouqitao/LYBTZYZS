using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Herbs;
using Xunit;
using LYBT.WebAPI;

namespace LYBT.Server.CompatibilityTests
{
    /// <summary>
    /// API兼容性测试 - Issue #2143
    /// 验证Entity直接返回优化后，现有API契约保持兼容
    /// 确保现有客户端无需修改即可正常工作
    /// </summary>
    public class ApiCompatibilityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly ILogger<ApiCompatibilityTests> _logger;

        public ApiCompatibilityTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 配置测试数据库
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("ApiCompatibilityTestDb");
                    });
                });
            });
            _logger = _factory.Services.GetRequiredService<ILogger<ApiCompatibilityTests>>();
        }

        [Fact]
        public async Task Users_Api_Should_Return_Compatible_Response()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act - 获取用户列表
            var response = await client.GetAsync("/api/users?page=1&pageSize=20");

            // Assert - 验证响应格式和结构
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // 验证可以解析为标准API响应格式
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.True(apiResponse.TryGetProperty("success", out var successProperty));
            Assert.True(apiResponse.TryGetProperty("data", out var dataProperty));
            Assert.True(apiResponse.TryGetProperty("message", out var messageProperty));

            // 验证数据包含预期字段
            Assert.True(successProperty.GetBoolean());
            Assert.True(dataProperty.ValueKind == JsonValueKind.Array);
        }

        [Fact]
        public async Task Users_GetById_Api_Should_Return_UserDto()
        {
            // Arrange
            var client = _factory.CreateClient();
            var testUserId = Guid.NewGuid();

            // Act
            var response = await client.GetAsync($"/api/users/{testUserId}");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var userDto = JsonSerializer.Deserialize<UserDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 验证UserDto结构完整性
                Assert.NotNull(userDto);
                // UserDto应包含的所有字段都应该存在（即使有些为null）
                Assert.NotNull(userDto.Id);
                Assert.NotNull(userDto.UserName);
            }
            // 404是可接受的（用户不存在）
            else if (response.StatusCode != HttpStatusCode.NotFound)
            {
                Assert.True(false, $"Unexpected status code: {response.StatusCode}");
            }
        }

        [Fact]
        public async Task Patients_Api_Should_Return_Compatible_Response()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/patients?page=1&pageSize=20");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // 验证响应格式兼容性
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.True(apiResponse.TryGetProperty("success", out var successProperty));
            Assert.True(apiResponse.TryGetProperty("data", out var dataProperty));
            Assert.True(successProperty.GetBoolean());
            Assert.True(dataProperty.ValueKind == JsonValueKind.Array);
        }

        [Fact]
        public async Task Herbs_Api_Should_Return_Compatible_Response()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/herbs?page=1&pageSize=20");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // 验证响应格式兼容性
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.True(apiResponse.TryGetProperty("success", out var successProperty));
            Assert.True(apiResponse.TryGetProperty("data", out var dataProperty));
            Assert.True(successProperty.GetBoolean());
            Assert.True(dataProperty.ValueKind == JsonValueKind.Array);
        }

        [Fact]
        public async Task Prescriptions_Api_Should_Return_Compatible_Response()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/prescriptions?page=1&pageSize=20");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // 验证响应格式兼容性
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.True(apiResponse.TryGetProperty("success", out var successProperty));
            Assert.True(apiResponse.TryGetProperty("data", out var dataProperty));
            Assert.True(successProperty.GetBoolean());
            Assert.True(dataProperty.ValueKind == JsonValueKind.Array);
        }

        [Theory]
        [InlineData("/api/users")]
        [InlineData("/api/patients")]
        [InlineData("/api/herbs")]
        [InlineData("/api/prescriptions")]
        public async Task All_Apis_Should_Return_Standard_ApiResponse_Format(string endpoint)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync($"{endpoint}?page=1&pageSize=5");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // 验证标准API响应格式
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);

            // 检查必要字段存在
            Assert.True(apiResponse.TryGetProperty("success", out var successProperty));
            Assert.True(apiResponse.TryGetProperty("data", out var dataProperty));
            Assert.True(apiResponse.TryGetProperty("message", out var messageProperty));

            // 检查字段类型
            Assert.True(successProperty.ValueKind == JsonValueKind.True || successProperty.ValueKind == JsonValueKind.False);
            Assert.True(messageProperty.ValueKind == JsonValueKind.String);

            _logger.LogInformation($"API兼容性验证通过: {endpoint}");
        }

        [Fact]
        public async Task Error_Response_Should_Maintain_Compatible_Format()
        {
            // Arrange
            var client = _factory.CreateClient();
            var nonExistentId = Guid.NewGuid();

            // Act - 请求不存在的资源
            var response = await client.GetAsync($"/api/users/{nonExistentId}");

            // Assert - 验证错误响应格式兼容性
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);

            // 验证错误响应也包含标准字段
            Assert.True(apiResponse.TryGetProperty("success", out var successProperty));
            Assert.True(apiResponse.TryGetProperty("message", out var messageProperty));

            Assert.False(successProperty.GetBoolean());
            Assert.False(string.IsNullOrEmpty(messageProperty.GetString()));
        }
    }
}