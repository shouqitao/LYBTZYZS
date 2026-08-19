using System.Net.Http;
using System.Net.Http.Headers;
using LYBT.Desktop.Contracts.Api;
using LYBT.Infrastructure.Data;
using LYBT.Module.Identity.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// WebAPI 集成测试基类（P2：WebApplicationFactory 内存 HTTP 替代 localhost:5000 直连）
/// 数据库指向本机 LocalDB（无 Docker）。
/// </summary>
public abstract class WebApiE2ETestBase : WebApplicationFactory<Program>, IAsyncLifetime
{
    // 序列化登录调用，防止并发登录导致409 Conflict
    private static readonly SemaphoreSlim _loginSemaphore = new(1, 1);

    protected IServiceProvider ServiceProvider { get; }
    protected IConfiguration Configuration { get; }
    protected ILogger<WebApiE2ETestBase> Logger { get; }

    // Refit API Clients (A-18 P1-1: 接口 internal 化后仅同程序集可访问)
    internal IAuthApi AuthApi { get; }
    internal IUserApi UserApi { get; }
    internal IPatientApi PatientApi { get; }
    internal IHerbApi HerbApi { get; }
    internal IFormulaApi FormulaApi { get; }
    internal IMedicalCaseApi MedicalCaseApi { get; }
    internal IRegistrationApi RegistrationApi { get; }

    // Token 管理
    protected TokenHolder TokenHolderInstance { get; }
    public string? AccessToken { get; private set; }
    protected string? RefreshToken { get; private set; }
    protected DateTime? TokenExpiresAt { get; private set; }
    protected LoginResponse? CurrentUser { get; private set; }

    protected TestDataTracker DataTracker { get; }

    protected WebApiE2ETestBase()
    {
        // Program.Main 直接读 ASPNETCORE_ENVIRONMENT 环境变量决定加载哪个 appsettings.{env}.json
        // （而非 Host 的 UseEnvironment）。若未置为 Test，Main 会加载 Production 配置 → 连接串为 ${DB_SERVER}
        // 占位符 → SQL 连接失败。此处显式置为 Test。
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        // 连接串经环境变量注入（Main 在最高优先级重加环境变量源，ConfigurationPostProcessor 不会破坏）——
        // 覆盖 config/appsettings.Test.json 的 ConnectionStrings:DefaultConnection="InMemory"。
        var localDb =
            "Server=(localdb)\\mssqllocaldb;Database=LYBTDB_Test_Integration;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False";
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", localDb);
        Environment.SetEnvironmentVariable("Database__ConnectionString", localDb);

        Configuration = Services.GetRequiredService<IConfiguration>();
        Logger = Services.GetRequiredService<ILogger<WebApiE2ETestBase>>();

        // 测试侧 DI：TokenHolder + 认证 handler + Refit 客户端（HTTP 走 TestServer 内存管道）
        var services = new ServiceCollection();
        services.AddSingleton(Configuration);
        services.AddSingleton<TokenHolder>();
        ServiceProvider = services.BuildServiceProvider();
        TokenHolderInstance = ServiceProvider.GetRequiredService<TokenHolder>();

        var refitSettings = CreateRefitSettings();

        AuthApi = RestService.For<IAuthApi>(CreateUnauthenticatedClient(), refitSettings);
        UserApi = RestService.For<IUserApi>(CreateAuthenticatedClientCore(), refitSettings);
        PatientApi = RestService.For<IPatientApi>(CreateAuthenticatedClientCore(), refitSettings);
        HerbApi = RestService.For<IHerbApi>(CreateAuthenticatedClientCore(), refitSettings);
        FormulaApi = RestService.For<IFormulaApi>(CreateAuthenticatedClientCore(), refitSettings);
        MedicalCaseApi = RestService.For<IMedicalCaseApi>(CreateAuthenticatedClientCore(), refitSettings);
        RegistrationApi = RestService.For<IRegistrationApi>(CreateAuthenticatedClientCore(), refitSettings);

        services.AddSingleton(AuthApi);
        services.AddSingleton(UserApi);
        services.AddSingleton(PatientApi);
        services.AddSingleton(HerbApi);
        services.AddSingleton(FormulaApi);
        services.AddSingleton(MedicalCaseApi);
        services.AddSingleton(RegistrationApi);

        DataTracker = new TestDataTracker(ServiceProvider, Logger);
    }

    public async Task InitializeAsync()
    {
        // WebApplicationFactory 不执行 Program.Main 的数据库初始化（Test 环境跳过迁移）——
        // 此处显式建库/迁移 + 播种角色与管理员，保证登录流程可用。
        await EnsureDatabaseInitializedAsync();
    }

    private async Task EnsureDatabaseInitializedAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
        await IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // 数据库指向本机 LocalDB（无 Docker），与 05-dual-mode 一致
                // 注意：config/appsettings.Test.json 将 ConnectionStrings:DefaultConnection 覆写为 "InMemory"，
                // 故用 Database:ConnectionString（该键无 Json 覆盖）承载真实 LocalDB 连接串（优先级最高）。
                ["Database:ConnectionString"] = "Server=(localdb)\\mssqllocaldb;Database=LYBTDB_Test_Integration;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False",
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=LYBTDB_Test_Integration;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False",
                ["ConnectionStrings:DefaultConnection_ForTest"] = "Server=(localdb)\\mssqllocaldb;Database=LYBTDB_Test_Integration;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False",
                // 测试专用确定性 JWT 密钥（Base64，解码≥32字节），与 config/appsettings.Test.json 一致
                ["Jwt:SecretKey"] = "VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2",
                ["TestCredentials:Username"] = "sysadmin",
                ["TestCredentials:Password"] = "TestAdmin2025@",
                ["TestCredentials:Admin:Username"] = "admin",
                ["TestCredentials:Admin:Password"] = "AdminPass123!",
                ["TestCredentials:Doctor:Username"] = "doctor",
                ["TestCredentials:Doctor:Password"] = "DoctorPass123!",
                ["TestCredentials:Receptionist:Username"] = "receptionist",
                ["TestCredentials:Receptionist:Password"] = "ReceptionistPass123!",
                ["WebAPI:BaseUrl"] = "http://localhost",
                ["WebAPI:TimeoutSeconds"] = "30",
                ["WebAPI:SkipSslValidation"] = "false"
            });
        });
    }

    public new async Task DisposeAsync()
    {
        await DataTracker.DisposeAsync();
        await base.DisposeAsync();
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

    protected async Task<LoginResponse> LoginAsAdminAsync()
    {
        return await LoginOrCreateUserAsync(
            username: "admin",
            password: "AdminPass123!",
            realName: "测试管理员",
            role: UserRole.Admin);
    }

    protected async Task<LoginResponse> LoginAsDoctorAsync()
    {
        return await LoginOrCreateUserAsync(
            username: "doctor",
            password: "DoctorPass123!",
            realName: "测试医生",
            role: UserRole.Doctor);
    }

    protected async Task<LoginResponse> LoginAsReceptionistAsync()
    {
        return await LoginOrCreateUserAsync(
            username: "receptionist",
            password: "ReceptionistPass123!",
            realName: "测试前台",
            role: UserRole.Receptionist);
    }

    private async Task<LoginResponse> LoginOrCreateUserAsync(string username, string password, string realName, UserRole role)
    {
        try
        {
            return await LoginAsAsync(username, password);
        }
        catch (Exception)
        {
            Logger.LogInformation("Login failed for {Username}, attempting to create user", username);
        }

        await LoginAsSysadminAsync();

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

        return await LoginAsAsync(username, password);
    }

    protected HttpClient CreateAuthenticatedClient()
    {
        if (string.IsNullOrEmpty(AccessToken))
        {
            throw new InvalidOperationException("Not logged in. Call LoginAsSysadminAsync first.");
        }

        var client = CreateAuthenticatedClientCore();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        return client;
    }

    internal IAuthApi CreateAuthenticatedAuthApi()
    {
        return RestService.For<IAuthApi>(CreateAuthenticatedClientCore(), CreateRefitSettings());
    }

    protected string GetBaseUrl()
    {
        return Server.BaseAddress.ToString();
    }

    /// <summary>
    /// 未认证的 TestServer 内存 HttpClient（登录端点用）
    /// </summary>
    private HttpClient CreateUnauthenticatedClient()
    {
        return CreateClient();
    }

    /// <summary>
    /// 带 Token 认证链的 TestServer 内存 HttpClient（基于 Server.CreateHandler 根管道）
    /// </summary>
    private HttpClient CreateAuthenticatedClientCore()
    {
        var authHandler = new AuthenticationDelegatingHandler(TokenHolderInstance)
        {
            InnerHandler = Server.CreateHandler()
        };

        return new HttpClient(authHandler)
        {
            BaseAddress = Server.BaseAddress,
            Timeout = TimeSpan.FromSeconds(Configuration.GetValue<int>("WebAPI:TimeoutSeconds", 30))
        };
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
}
