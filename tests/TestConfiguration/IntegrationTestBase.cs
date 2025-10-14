using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Net.Http;
using Xunit;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.WebAPI;

namespace LYBT.Tests.Common
{
    /// <summary>
    /// 集成测试基类 - 提供Web API测试的标准基础设施
    /// 包含内存数据库、认证、配置管理等支持
    /// </summary>
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly WebApplicationFactory<Program> Factory;
        protected readonly HttpClient Client;
        protected readonly IServiceProvider ServiceProvider;
        protected readonly Mock<ILogger> MockLogger;

        protected IntegrationTestBase()
        {
            Factory = CreateWebApplicationFactory();
            Client = CreateHttpClient(Factory);
            ServiceProvider = Factory.Services;

            // 创建通用Logger Mock
            MockLogger = new Mock<ILogger>();
        }

        /// <summary>
        /// 创建WebApplicationFactory - 子类可重写来自定义配置
        /// </summary>
        protected virtual WebApplicationFactory<Program> CreateWebApplicationFactory()
        {
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    ConfigureWebHost(builder);
                });

            return factory;
        }

        /// <summary>
        /// 配置WebHost - 子类可重写来自定义主机配置
        /// </summary>
        protected virtual void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                ConfigureTestConfiguration(config);
            });

            builder.ConfigureServices(services =>
            {
                ConfigureTestServices(services);
            });
        }

        /// <summary>
        /// 配置测试环境配置
        /// </summary>
        protected virtual void ConfigureTestConfiguration(IConfigurationBuilder configBuilder)
        {
            // 添加测试配置
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                ["AllowedHosts"] = "*",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:SecretKey"] = "ThisIsASecretKeyForTestingOnly123456789",
                ["Jwt:ExpirationMinutes"] = "60"
            });
        }

        /// <summary>
        /// 配置测试服务 - 子类可重写来自定义服务配置
        /// </summary>
        protected virtual void ConfigureTestServices(IServiceCollection services)
        {
            // 配置内存数据库
            ConfigureInMemoryDatabase(services);

            // 配置测试特定的服务
            ConfigureTestSpecificServices(services);
        }

        /// <summary>
        /// 配置内存数据库
        /// </summary>
        protected virtual void ConfigureInMemoryDatabase(IServiceCollection services)
        {
            // 移除现有的DbContext配置
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 添加内存数据库
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                options.EnableSensitiveDataLogging();
                options.EnableServiceProviderCaching();
            });

            // 构建服务提供器并创建数据库
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                SeedTestData(db);
            }
        }

        /// <summary>
        /// 配置测试特定的服务 - 子类可重写
        /// </summary>
        protected virtual void ConfigureTestSpecificServices(IServiceCollection services)
        {
            // 注册Mock Logger
            services.AddSingleton(MockLogger.Object);

            // 子类可重写此方法来添加额外的服务配置
        }

        /// <summary>
        /// 创建HttpClient - 子类可重写来自定义客户端配置
        /// </summary>
        protected virtual HttpClient CreateHttpClient(WebApplicationFactory<Program> factory)
        {
            var client = factory.CreateClient();

            // 设置默认请求头
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // 设置认证头
            SetAuthorizationHeader(client);

            return client;
        }

        /// <summary>
        /// 设置认证头 - 子类可重写来自定义认证逻辑
        /// </summary>
        protected virtual void SetAuthorizationHeader(HttpClient client)
        {
            // 模拟JWT token
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestToken());
        }

        /// <summary>
        /// 生成测试JWT Token
        /// </summary>
        protected virtual string GenerateTestToken()
        {
            // 简单的测试token - 实际项目中应该使用真实的JWT生成逻辑
            return "test-jwt-token-for-integration-testing";
        }

        /// <summary>
        /// 种子测试数据 - 子类可重写来自定义测试数据
        /// </summary>
        protected virtual void SeedTestData(AppDbContext context)
        {
            // 基础种子数据 - 子类可重写来添加更多测试数据
            SeedBasicTestData(context);
        }

        /// <summary>
        /// 种子基础测试数据
        /// </summary>
        protected virtual void SeedBasicTestData(AppDbContext context)
        {
            // 这里可以添加基础的用户、角色等数据
            // 具体实现取决于项目的实体模型
        }

        /// <summary>
        /// 清理测试环境
        /// </summary>
        protected virtual void Cleanup()
        {
            // 清理数据库
            using var scope = ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
        }

        /// <summary>
        /// 创建测试用的JSON内容
        /// </summary>
        protected static StringContent CreateJsonContent<T>(T obj)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(obj);
            return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// 解析响应内容为指定类型
        /// </summary>
        protected static T? ParseResponseContent<T>(string content)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(content);
        }

        /// <summary>
        /// 验证日志记录
        /// </summary>
        protected void VerifyLogged(LogLevel logLevel, string? message = null)
        {
            if (message != null)
            {
                MockLogger.Verify(
                    x => x.Log(
                        logLevel,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(message)),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            else
            {
                MockLogger.Verify(
                    x => x.Log(
                        logLevel,
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
        }

        public virtual void Dispose()
        {
            Cleanup();
            Client?.Dispose();
            Factory?.Dispose();
        }
    }

    /// <summary>
    /// 集成测试基类 - 支持泛型配置
    /// 允许子类指定自定义的Program类型
    /// </summary>
    public abstract class IntegrationTestBase<TProgram> : IntegrationTestBase
        where TProgram : class
    {
        protected WebApplicationFactory<TProgram> TypedFactory = null!;
        protected override HttpClient CreateHttpClient(WebApplicationFactory<Program> factory)
        {
            if (factory is WebApplicationFactory<TProgram> typedFactory)
            {
                TypedFactory = typedFactory;
                var client = typedFactory.CreateClient();

                // 设置默认请求头
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // 设置认证头
                SetAuthorizationHeader(client);

                return client;
            }

            throw new InvalidOperationException("无法将WebApplicationFactory<Program>转换为WebApplicationFactory<TProgram>");
        }
    }
}