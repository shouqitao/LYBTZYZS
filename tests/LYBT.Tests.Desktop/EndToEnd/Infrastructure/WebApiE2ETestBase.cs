using System.Net.Http;
using System.Net.Http.Headers;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;

namespace LYBT.Tests.Desktop.EndToEnd.Infrastructure;

public abstract class WebApiE2ETestBase : IDisposable, IAsyncDisposable
{
    // 序列化登录调用，防止并发登录导致409 Conflict
    private static readonly SemaphoreSlim _loginSemaphore = new(1, 1);

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
    protected LoginResponse? CurrentUser { get; private set; }

    protected TestDataTracker DataTracker { get; }

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
        
        DataTracker = new TestDataTracker(ServiceProvider, Logger);
    }

    protected async Task<LoginResponse> LoginAsAsync(string username, string password)
    {
        await _loginSemaphore.WaitAsync();
        try
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
            CurrentUser = response.Data;
            TokenHolderInstance.AccessToken = AccessToken;
            
            Logger.LogInformation("Login successful for {Username}, token expires at {ExpiresAt}", username, TokenExpiresAt);
            
            return response.Data;
        }
        finally
        {
            _loginSemaphore.Release();
        }
    }

    protected async Task<LoginResponse> LoginAsSysadminAsync()
    {
        await _loginSemaphore.WaitAsync();
        try
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
            CurrentUser = response.Data;
            TokenHolderInstance.AccessToken = AccessToken;
            
            Logger.LogInformation("Login successful, token expires at {ExpiresAt}", TokenExpiresAt);
            
            return response.Data;
        }
        finally
        {
            _loginSemaphore.Release();
        }
    }

    private readonly HashSet<string> _createdUsers = new();

    /// <summary>
    /// Login as Admin. Auto-creates the user if it doesn't exist.
    /// </summary>
    protected async Task<LoginResponse> LoginAsAdminAsync()
    {
        return await LoginOrCreateUserAsync(
            username: "admin",
            password: "AdminPass123!",
            realName: "测试管理员",
            role: UserRole.Admin);
    }

    /// <summary>
    /// Login as Doctor. Auto-creates the user if it doesn't exist.
    /// </summary>
    protected async Task<LoginResponse> LoginAsDoctorAsync()
    {
        return await LoginOrCreateUserAsync(
            username: "doctor",
            password: "DoctorPass123!",
            realName: "测试医生",
            role: UserRole.Doctor);
    }

    /// <summary>
    /// Login as Receptionist. Auto-creates the user if it doesn't exist.
    /// </summary>
    protected async Task<LoginResponse> LoginAsReceptionistAsync()
    {
        return await LoginOrCreateUserAsync(
            username: "receptionist",
            password: "ReceptionistPass123!",
            realName: "测试前台",
            role: UserRole.Receptionist);
    }

    /// <summary>
    /// Attempts login first; if user doesn't exist (401), creates it via sysadmin then retries.
    /// </summary>
    private async Task<LoginResponse> LoginOrCreateUserAsync(string username, string password, string realName, UserRole role)
    {
        // Try login first
        try
        {
            return await LoginAsAsync(username, password);
        }
        catch (Exception)
        {
            // User might not exist, try to create it
            Logger.LogInformation("Login failed for {Username}, attempting to create user", username);
        }

        // Login as sysadmin to create the user
        await LoginAsSysadminAsync();

        // Check if user already exists (maybe password wrong)
        if (!_createdUsers.Contains(username))
        {
            try
            {
                var createResponse = await UserApi.CreateUserAsync(new UserInputDto
                {
                    UserName = username,
                    Password = password,
                    ConfirmPassword = password,
                    RealName = realName,
                    Role = role,
                    Remark = "E2E测试自动创建"
                });

                if (createResponse.Success)
                {
                    _createdUsers.Add(username);
                    Logger.LogInformation("Created test user: {Username} with role {Role}", username, role);
                }
                else
                {
                    Logger.LogWarning("Failed to create user {Username}: {Message}", username, createResponse.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Exception creating user {Username}, may already exist", username);
            }
        }

        // Now try login again as the target user
        return await LoginAsAsync(username, password);
    }

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
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        await DataTracker.DisposeAsync();
        
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
