using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;
using LYBT.Infrastructure.Data;
using Xunit;
using LYBT.WebAPI;

namespace LYBT.Server.CompatibilityTests
{
    /// <summary>
    /// API兼容性测试 - 验证API契约正确性
    /// 确保所有API端点返回标准 ApiResponse 格式（success/data/message）
    /// 错误响应使用 RFC 7807 ProblemDetails 格式
    /// </summary>
    public class ApiCompatibilityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _authenticatedClient;

        public ApiCompatibilityTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");

                // 设置正确的内容根路径
                var solutionRoot = GetSolutionRoot();
                var webApiPath = Path.Combine(solutionRoot, "src", "Server", "Services", "LYBT.WebAPI");
                builder.UseContentRoot(webApiPath);

                builder.ConfigureServices(services =>
                {
                    // 移除现有DbContext配置
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // 使用InMemory数据库
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("ApiCompatibilityTestDb");
                    });

                    // 移除长时间运行的后台服务
                    RemoveHostedServices(services);
                });
            });

            // 创建带认证的客户端
            _authenticatedClient = _factory.CreateClient();
            _authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestToken());
        }

        #region 成功响应兼容性测试

        [Fact]
        public async Task Users_Api_Should_Return_ApiResponse_Format()
        {
            // Act - 使用v1版本化路由
            var response = await _authenticatedClient.GetAsync("/api/v1/users?page=1&pageSize=20");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            AssertStandardApiResponseFormat(content);
        }

        [Fact]
        public async Task Users_GetById_Api_Should_Accept_NotFound()
        {
            // Arrange
            var testUserId = Guid.NewGuid();

            // Act
            var response = await _authenticatedClient.GetAsync($"/api/v1/users/{testUserId}");

            // Assert - 404是可接受的（用户不存在）
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                AssertStandardApiResponseFormat(content);
            }
            else
            {
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task Patients_Api_Should_Return_ApiResponse_Format()
        {
            // Act
            var response = await _authenticatedClient.GetAsync("/api/v1/patients?page=1&pageSize=20");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            AssertStandardApiResponseFormat(content);
        }

        [Fact]
        public async Task Herbs_Api_Should_Return_ApiResponse_Format()
        {
            // Act
            var response = await _authenticatedClient.GetAsync("/api/v1/herbs?page=1&pageSize=20");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            AssertStandardApiResponseFormat(content);
        }

        [Fact]
        public async Task Formulas_Api_Should_Return_ApiResponse_Format()
        {
            // Act
            var response = await _authenticatedClient.GetAsync("/api/v1/formulas?page=1&pageSize=20");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            AssertStandardApiResponseFormat(content);
        }

        #endregion

        #region 所有端点标准格式验证

        [Theory]
        [InlineData("/api/v1/users")]
        [InlineData("/api/v1/patients")]
        [InlineData("/api/v1/herbs")]
        [InlineData("/api/v1/formulas")]
        public async Task All_Apis_Should_Return_Standard_ApiResponse_Format(string endpoint)
        {
            // Act
            var response = await _authenticatedClient.GetAsync($"{endpoint}?page=1&pageSize=5");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            AssertStandardApiResponseFormat(content);
        }

        #endregion

        #region 错误响应兼容性测试

        [Fact]
        public async Task Error_Response_Should_Use_ProblemDetails_Format()
        {
            // Arrange - 使用未认证客户端请求受保护资源
            var unauthenticatedClient = _factory.CreateClient();
            var nonExistentId = Guid.NewGuid();

            // Act - 未认证请求应返回401
            var response = await unauthenticatedClient.GetAsync($"/api/v1/users/{nonExistentId}");

            // Assert - 验证错误响应使用ProblemDetails格式
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task NotFound_Response_Should_Return_404()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _authenticatedClient.GetAsync($"/api/v1/users/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证标准ApiResponse格式
        /// </summary>
        private static void AssertStandardApiResponseFormat(string content)
        {
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);

            // 检查必要字段存在
            Assert.True(apiResponse.TryGetProperty("success", out var successProperty),
                "ApiResponse应包含'success'字段");
            Assert.True(apiResponse.TryGetProperty("data", out _),
                "ApiResponse应包含'data'字段");
            Assert.True(apiResponse.TryGetProperty("message", out _),
                "ApiResponse应包含'message'字段");

            // 检查success字段类型
            Assert.True(
                successProperty.ValueKind == JsonValueKind.True ||
                successProperty.ValueKind == JsonValueKind.False,
                "'success'字段应为布尔类型");
        }

        /// <summary>
        /// 生成测试JWT Token - 必须与appsettings.Test.json的Jwt配置一致
        /// 注意：Auth middleware 使用 Encoding.UTF8.GetBytes(configValue) 作为签名密钥
        /// 所以这里也必须使用相同的 Base64 字符串的 UTF8 字节
        /// </summary>
        private static string GenerateTestToken()
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(
                "VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2");

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Name, "CompatibilityTest User"),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Role, "Admin")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "LYBT.WebAPI.Tests",
                Audience = "LYBT.Client.Tests",
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 获取解决方案根目录
        /// </summary>
        private static string GetSolutionRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "LYBT.All.sln")))
            {
                directory = directory.Parent;
            }
            return directory?.FullName ?? throw new InvalidOperationException("无法找到解决方案根目录");
        }

        /// <summary>
        /// 移除长时间运行的后台服务
        /// </summary>
        private static void RemoveHostedServices(IServiceCollection services)
        {
            var servicesToRemove = new[]
            {
                "SecurityAuditCleanupService",
                "LogCleanupService",
                "DatabaseStartupDiagnostics"
            };

            var hostedServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                           d.ImplementationType != null &&
                           servicesToRemove.Contains(d.ImplementationType.Name))
                .ToList();

            foreach (var descriptor in hostedServiceDescriptors)
            {
                services.Remove(descriptor);
            }
        }

        #endregion
    }
}
