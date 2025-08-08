using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.IntegrationTests.Base
{
    /// <summary>
    /// 集成测试基类
    /// </summary>
    public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        protected readonly WebApplicationFactory<Program> Factory;
        protected readonly HttpClient Client;
        protected readonly JsonSerializerOptions JsonOptions;
        protected string? AuthToken;
        private IServiceScope? _scope;

        protected IntegrationTestBase(WebApplicationFactory<Program> factory)
        {
            Factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 移除原有的DbContext配置
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // 使用内存数据库进行测试
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });

            Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            JsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// 测试初始化
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            _scope = Factory.Services.CreateScope();
            var dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // 确保数据库已创建
            await dbContext.Database.EnsureCreatedAsync();
            
            // 初始化测试数据
            await SeedTestDataAsync(dbContext);
            
            // 登录获取令牌
            await AuthenticateAsync();
        }

        /// <summary>
        /// 测试清理
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            if (_scope != null)
            {
                var dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.EnsureDeletedAsync();
                _scope.Dispose();
            }
            
            Client?.Dispose();
        }

        /// <summary>
        /// 种子数据初始化
        /// </summary>
        protected virtual async Task SeedTestDataAsync(AppDbContext context)
        {
            // 子类可以重写此方法来添加测试数据
            await Task.CompletedTask;
        }

        /// <summary>
        /// 身份验证
        /// </summary>
        protected virtual async Task AuthenticateAsync()
        {
            await AuthenticateAsAsync("sysadmin", "Admin@123456");
        }

        /// <summary>
        /// 使用指定用户身份验证
        /// </summary>
        protected async Task AuthenticateAsAsync(string username, string password)
        {
            var loginDto = new LoginRequestDto
            {
                Username = username,
                Password = password,
                RememberMe = false
            };

            var response = await PostAsync("/api/v1/auth/login", loginDto);
            response.EnsureSuccessStatusCode();

            var loginResponse = await DeserializeResponseAsync<LoginResponseDto>(response);
            AuthToken = loginResponse?.Token;

            if (!string.IsNullOrEmpty(AuthToken))
            {
                Client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", AuthToken);
            }
        }

        #region HTTP 辅助方法

        /// <summary>
        /// 发送 GET 请求
        /// </summary>
        protected Task<HttpResponseMessage> GetAsync(string url)
        {
            return Client.GetAsync(url);
        }

        /// <summary>
        /// 发送 POST 请求
        /// </summary>
        protected Task<HttpResponseMessage> PostAsync<T>(string url, T data)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(data, JsonOptions),
                Encoding.UTF8,
                "application/json");
            return Client.PostAsync(url, content);
        }

        /// <summary>
        /// 发送 PUT 请求
        /// </summary>
        protected Task<HttpResponseMessage> PutAsync<T>(string url, T data)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(data, JsonOptions),
                Encoding.UTF8,
                "application/json");
            return Client.PutAsync(url, content);
        }

        /// <summary>
        /// 发送 DELETE 请求
        /// </summary>
        protected Task<HttpResponseMessage> DeleteAsync(string url)
        {
            return Client.DeleteAsync(url);
        }

        #endregion

        #region 响应处理辅助方法

        /// <summary>
        /// 反序列化响应内容
        /// </summary>
        protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(content, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize response: {content}", ex);
            }
        }

        /// <summary>
        /// 获取错误响应内容
        /// </summary>
        protected async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            try
            {
                var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content, JsonOptions);
                if (errorResponse != null && errorResponse.TryGetValue("message", out var message))
                {
                    return message.ToString() ?? "Unknown error";
                }
            }
            catch
            {
                // 忽略反序列化错误
            }
            return content;
        }

        #endregion

        #region 数据库访问辅助方法

        /// <summary>
        /// 获取数据库上下文
        /// </summary>
        protected AppDbContext GetDbContext()
        {
            if (_scope == null)
            {
                throw new InvalidOperationException("Test not initialized");
            }
            return _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        /// <summary>
        /// 在事务中执行数据库操作
        /// </summary>
        protected async Task ExecuteInTransactionAsync(Func<AppDbContext, Task> action)
        {
            var dbContext = GetDbContext();
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                await action(dbContext);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion
    }
}