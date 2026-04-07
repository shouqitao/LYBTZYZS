using System.Net.Http;
using System.Net.Http.Headers;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;

namespace LYBT.Tests.Desktop.EndToEnd.Infrastructure;

/// <summary>
/// E2E 测试基类 - 连接真实 WebAPI
/// 
/// 设计原则：
/// - 每个测试类继承此类，获得配置好的 HTTP Client
/// - 自动处理 JWT Token 获取和刷新
/// - 支持测试之间的依赖（通过测试顺序控制）
/// </summary>
public abstract class WebApiE2ETestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    protected IConfiguration Configuration { get; }
    protected ILogger<WebApiE2ETestBase> Logger { get; }
    
    // Refit API Clients
    protected IAuthApi AuthApi { get; }
    protected IUserApi UserApi { get; }
    protected IPatientApi PatientApi { get; }
    protected IHerbApi HerbApi { get; }
    protected IFormulaApi FormulaApi { get; }
    protected IMedicalCaseApi MedicalCaseApi { get; }
    protected ISyncApi SyncApi { get; }
    protected IRegistrationApi RegistrationApi { get; }
    
    // Token 管理
    protected TokenHolder TokenHolderInstance { get; }
    public string? AccessToken { get; private set; }
    protected string? RefreshToken { get; private set; }
    protected DateTime? TokenExpiresAt { get; private set; }

    protected WebApiE2ETestBase()
    {
        Configuration = BuildConfiguration();
        
        var services = new ServiceCollection();
        services.AddSingleton(Configuration);
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        services.AddSingleton<TokenHolder>();
        
        // 配置 Refit Clients
        ConfigureRefitClients(services);
        
        ServiceProvider = services.BuildServiceProvider();
        
        // 获取 Logger
        Logger = ServiceProvider.GetRequiredService<ILogger<WebApiE2ETestBase>>();
        TokenHolderInstance = ServiceProvider.GetRequiredService<TokenHolder>();
        
        // 获取 API Clients
        AuthApi = ServiceProvider.GetRequiredService<IAuthApi>();
        UserApi = ServiceProvider.GetRequiredService<IUserApi>();
        PatientApi = ServiceProvider.GetRequiredService<IPatientApi>();
        HerbApi = ServiceProvider.GetRequiredService<IHerbApi>();
        FormulaApi = ServiceProvider.GetRequiredService<IFormulaApi>();
        MedicalCaseApi = ServiceProvider.GetRequiredService<IMedicalCaseApi>();
        SyncApi = ServiceProvider.GetRequiredService<ISyncApi>();
        RegistrationApi = ServiceProvider.GetRequiredService<IRegistrationApi>();
    }

    /// <summary>
    /// 以指定用户身份登录
    /// </summary>
    protected async Task<LoginResponse> LoginAsAsync(string username, string password)
    {
        Logger.LogInformation("Logging in as {Username}", username);
        
        var response = await AuthApi.LoginAsync(new LoginRequest
        {
            UserName = username,
            Password = password
        });
        
        if (!response.Success || response.Data == null)
        {
            throw new InvalidOperationException($"Login failed for {username}: {response.Message}");
        }
        
        AccessToken = response.Data.Token;
        RefreshToken = response.Data.RefreshToken;
        TokenExpiresAt = response.Data.ExpiresAt;
        TokenHolderInstance.AccessToken = AccessToken;
        
        Logger.LogInformation("Login successful for {Username}, token expires at {ExpiresAt}", username, TokenExpiresAt);
        
        return response.Data;
    }

    /// <summary>
    /// 执行 sysadmin 登录，获取 JWT Token
    /// </summary>
    protected async Task<LoginResponse> LoginAsSysadminAsync()
    {
        var username = Configuration["TestCredentials:Username"]!;
        var password = Configuration["TestCredentials:Password"]!;
        
        Logger.LogInformation("Logging in as {Username}", username);
        
        var response = await AuthApi.LoginAsync(new LoginRequest
        {
            UserName = username,
            Password = password
        });
        
        if (!response.Success || response.Data == null)
        {
            throw new InvalidOperationException($"Login failed: {response.Message}");
        }
        
        AccessToken = response.Data.Token;
        RefreshToken = response.Data.RefreshToken;
        TokenExpiresAt = response.Data.ExpiresAt;
        TokenHolderInstance.AccessToken = AccessToken;
        
        Logger.LogInformation("Login successful, token expires at {ExpiresAt}", TokenExpiresAt);
        
        return response.Data;
    }

    /// <summary>
    /// 创建带认证 Header 的 HTTP Client（用于手动请求）
    /// </summary>
    protected HttpClient CreateAuthenticatedClient()
    {
        if (string.IsNullOrEmpty(AccessToken))
        {
            throw new InvalidOperationException("Not logged in. Call LoginAsSysadminAsync first.");
        }
        
        var client = new HttpClient
        {
            BaseAddress = new Uri(GetBaseUrl())
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        
        return client;
    }

    protected IAuthApi CreateAuthenticatedAuthApi()
    {
        var timeoutSeconds = Configuration.GetValue<int>("WebAPI:TimeoutSeconds", 30);
        var skipSslValidation = Configuration.GetValue<bool>("WebAPI:SkipSslValidation", false);

        var baseHandler = CreateHttpMessageHandler(skipSslValidation);
        var authHandler = ActivatorUtilities.CreateInstance<AuthenticationDelegatingHandler>(ServiceProvider);
        authHandler.InnerHandler = baseHandler;

        var client = new HttpClient(authHandler)
        {
            BaseAddress = new Uri(GetBaseUrl()),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        return RestService.For<IAuthApi>(client, CreateRefitSettings());
    }

    /// <summary>
    /// 获取基础 URL
    /// </summary>
    protected string GetBaseUrl()
    {
        return Configuration["WebAPI:BaseUrl"]!;
    }

    private IConfiguration BuildConfiguration()
    {
        var basePath = AppContext.BaseDirectory;
        
        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
    }

    private void ConfigureRefitClients(IServiceCollection services)
    {
        var baseUrl = GetBaseUrl();
        var timeoutSeconds = Configuration.GetValue<int>("WebAPI:TimeoutSeconds", 30);
        var skipSslValidation = Configuration.GetValue<bool>("WebAPI:SkipSslValidation", false);
        
        var refitSettings = CreateRefitSettings();

        services.AddTransient<AuthenticationDelegatingHandler>();
        
        ConfigureClient<IAuthApi>(services, baseUrl, timeoutSeconds, skipSslValidation, refitSettings, false);
        ConfigureClient<IUserApi>(services, baseUrl, timeoutSeconds, skipSslValidation, refitSettings, true);
        ConfigureClient<IPatientApi>(services, baseUrl, timeoutSeconds, skipSslValidation, refitSettings, true);
        ConfigureClient<IHerbApi>(services, baseUrl, timeoutSeconds, skipSslValidation, refitSettings, true);
        ConfigureClient<IFormulaApi>(services, baseUrl, timeoutSeconds, skipSslValidation, refitSettings, true);
        ConfigureClient<IMedicalCaseApi>(services, baseUrl, timeoutSeconds, skipSslValidation, refitSettings, true);
        ConfigureClient<ISyncApi>(services, baseUrl, timeoutSeconds, skipSslValidation, refitSettings, true);
        ConfigureClient<IRegistrationApi>(services, baseUrl, timeoutSeconds, skipSslValidation, refitSettings, true);
    }

    private static RefitSettings CreateRefitSettings()
    {
        return new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })
        };
    }

    private static HttpClientHandler CreateHttpMessageHandler(bool skipSslValidation)
    {
        return skipSslValidation 
            ? new HttpClientHandler 
            { 
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true 
            } 
            : new HttpClientHandler();
    }

    private static void ConfigureClient<T>(
        IServiceCollection services, 
        string baseUrl, 
        int timeoutSeconds,
        bool skipSslValidation,
        RefitSettings refitSettings,
        bool useAuth) where T : class
    {
        services.AddSingleton<T>(sp =>
        {
            var primaryHandler = CreateHttpMessageHandler(skipSslValidation);
            HttpMessageHandler pipeline = primaryHandler;

            if (useAuth)
            {
                var authHandler = ActivatorUtilities.CreateInstance<AuthenticationDelegatingHandler>(sp);
                authHandler.InnerHandler = primaryHandler;
                pipeline = authHandler;
            }

            var client = new HttpClient(pipeline)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            return RestService.For<T>(client, refitSettings);
        });
    }

    public virtual void Dispose()
    {
        // 清理资源
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
