using LYBT.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

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
        protected readonly ILogger MockLogger;

        //  Issue #1669: 固定数据库名，确保同一测试实例的所有HTTP请求使用同一内存数据库
        protected readonly string TestDatabaseName;

        //  Issue #1669 Phase 6: 共享InMemoryDatabaseRoot，确保所有DbContext实例共享同一数据库
        // 静态字段在整个测试运行期间保持不变，所有测试实例共享
        private static readonly InMemoryDatabaseRoot _sharedDatabaseRoot = new InMemoryDatabaseRoot();

        protected IntegrationTestBase()
        {
            //  必须在创建Factory之前设置环境变量，确保Program.Main正确加载appsettings.Test.json
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

            // 为当前测试实例生成唯一但固定的数据库名
            TestDatabaseName = $"TestDb_{Guid.NewGuid()}";

            Factory = CreateWebApplicationFactory();
            Client = CreateHttpClient(Factory);
            ServiceProvider = Factory.Services;

            // 创建通用Logger Substitute
            MockLogger = Substitute.For<ILogger>();
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
            // 设置测试环境
            builder.UseEnvironment("Test");

            // 设置正确的内容根路径 - WebApplicationFactory默认使用程序集名称推断路径
            // 但我们的项目结构是 src/Server/Services/LYBT.WebAPI，需要显式指定
            var solutionRoot = GetSolutionRoot();
            var webApiPath = Path.Combine(solutionRoot, "src", "Server", "Services", "LYBT.WebAPI");
            builder.UseContentRoot(webApiPath);

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
        /// 配置测试环境配置
        /// </summary>
        protected virtual void ConfigureTestConfiguration(IConfigurationBuilder configBuilder)
        {
            // 添加测试配置（补充或覆盖appsettings.Test.json中的配置）
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                ["AllowedHosts"] = "*"
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
        /// 配置SQL Server真实数据库 - 集成测试使用真实数据库
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

            // 集成测试使用真实SQL Server数据库(LYBTDB)
            // 配置来源: TestConfiguration/appsettings.Test.json
            var connectionString = "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Application Name=LYBT.IntegrationTests";

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
                options.EnableSensitiveDataLogging();
                options.EnableServiceProviderCaching();
            });

            // 构建服务提供器并验证数据库连接
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // 确保数据库存在(不删除现有数据)
                db.Database.EnsureCreated();
                SeedTestData(db);
            }
        }

        /// <summary>
        /// 配置测试特定的服务 - 子类可重写
        /// </summary>
        protected virtual void ConfigureTestSpecificServices(IServiceCollection services)
        {
            // 集成测试使用真实的Serilog logger（已在Program.cs中配置）
            // MockLogger仅供单元测试使用

            //  Issue #1668 Solution A：使用真实JWT Token（在GenerateTestToken中生成）
            // 不需要自定义认证处理器，直接使用Program.cs中配置的JWT认证

            // 移除长时间运行的后台服务，避免测试后dotnet进程驻留
            // 这些服务在测试环境中不需要，且会导致WebApplicationFactory.Dispose()无法及时完成
            RemoveLongRunningHostedServices(services);

            // 子类可重写此方法来添加额外的服务配置
        }

        /// <summary>
        /// 移除长时间运行的后台服务
        /// 解决测试后dotnet.exe进程驻留问题
        /// </summary>
        private static void RemoveLongRunningHostedServices(IServiceCollection services)
        {
            // 需要移除的后台服务类型名称
            var servicesToRemove = new[]
            {
                "SecurityAuditCleanupService",   // 等待到凌晨3点的定时清理服务
                "LogCleanupService",             // 日志清理服务
                "DatabaseStartupDiagnostics"    // 数据库启动诊断（测试环境不需要）
            };

            // 查找并移除这些服务
            var hostedServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                           d.ImplementationType != null &&
                           servicesToRemove.Contains(d.ImplementationType.Name))
                .ToList();

            foreach (var descriptor in hostedServiceDescriptors)
            {
                services.Remove(descriptor);
            }
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
            // 生成真实的JWT Token用于集成测试认证（Issue #1668 Solution A）
            // 密钥、Issuer、Audience必须与LYBT.WebAPI/appsettings.Test.json完全一致
            // 注意：Auth middleware 使用 Encoding.UTF8.GetBytes(configValue) 作为签名密钥
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.UTF8.GetBytes("VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2");

            //  Issue #1669: NameIdentifier必须是有效的Guid，用于填充CreatedBy审计字段
            var testUserId = Guid.NewGuid();

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, testUserId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User"),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
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
                MockLogger.Received().Log(
                    logLevel,
                    Arg.Any<EventId>(),
                    Arg.Is<object>(o => o.ToString()!.Contains(message)),
                    Arg.Any<Exception>(),
                    Arg.Any<Func<object, Exception?, string>>());
            }
            else
            {
                MockLogger.Received().Log(
                    logLevel,
                    Arg.Any<EventId>(),
                    Arg.Any<object>(),
                    Arg.Any<Exception>(),
                    Arg.Any<Func<object, Exception?, string>>());
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

    /// <summary>
    /// 测试环境Model定制器 - Issue #1669 Phase 7
    /// 移除RowVersion的IsConcurrencyToken配置，避免InMemory数据库并发冲突
    /// </summary>
    internal class TestModelCustomizer : Microsoft.EntityFrameworkCore.Infrastructure.ModelCustomizer
    {
        public TestModelCustomizer(Microsoft.EntityFrameworkCore.Infrastructure.ModelCustomizerDependencies dependencies)
            : base(dependencies)
        {
        }

        public override void Customize(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);

            // 移除所有实体的RowVersion并发令牌配置
            // InMemory数据库不支持RowVersion并发检查，会导致DbUpdateConcurrencyException
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var rowVersionProperty = entityType.FindProperty("RowVersion");
                if (rowVersionProperty != null)
                {
                    rowVersionProperty.IsConcurrencyToken = false;
                }
            }
        }
    }
}
