// ---------------------------------------------------------------------------
// ModeSwitchE2ETests — US-SHELL-007 远程/本地模式切换 全链路
// ---------------------------------------------------------------------------
// 真实链路：真实 ConnectionSettingsService（user-settings.json 持久化）
//          → 真实 ConnectionModeService（SetModeAsync 守卫）
//          → 真实 SwitchingApiClient（按 URL 路由 HttpClientApiClient / RefitApiClient）
//          → 真实 HTTP → 真实 LocalWebAPI（Kestrel）→ 真实 LocalDB
//
// 双后端（数据隔离需要两台真实服务器，各自独立 LocalDB）：
//   · 本地腿：本类自行启动的嵌入式 LocalWebAPI，监听 http://localhost:5300
//            （ConnectionSettingsService.LocalUrlConstant 固定 5300，且 IsLocalUrl 仅认 :5300；
//              绑定前先占用端口，端口被占则测试立即失败，绝不请求他人服务）
//   · 远程腿：E2ETestBase 基类启动的 LocalWebAPI（随机端口，非 :5300 → IsLocal=false →
//            走 RefitApiClient 远程分支），作为「远程 WebAPI」替身，独立 LocalDB
//
// 已知缺口（登记见 13c，本次仅取证不修）：
//   ERR-70506 未完成医案守卫：ConnectionModeService.FireAndForgetRemoteProbeAsync 只在**后台探测**中
//   记录 warning，未阻断切换（ModeSwitchResult.PendingCasesBlocked 有定义无调用）→ 需求 AC
//   「本地有未完成医案时切换到远程 → 阻断并提示」当前不成立；本套件不为此写断言（避免把缺陷固化为契约）。
//
// 附带发现（本套件取证、登记见 13c）：患者关键词检索 GET /api/v1/patients?keyword=… 在部分关键词下 500
//   （"The invalid escape character … was specified in a LIKE predicate"）。EF 生成的 SQL 对加密列
//   PhoneNumber 的 Contains 翻译为 `LIKE @p ESCAPE N'<密文>'`（值转换器同样作用于 ESCAPE 常量，
//   见 ToQueryString 取证）。本套件的数据隔离断言改用「按 Id 取详情：本库 200 / 对方库 404」，
//   不依赖关键词检索。
// ---------------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Refit;

namespace LYBT.Tests.Desktop.E2E.ShellFlow;

[Collection("E2ELocal")]
public class ModeSwitchE2ETests : E2ETestBase
{
    /// <summary>嵌入式 LocalWebAPI 固定地址（与 ConnectionSettingsService.LocalUrlConstant 同源）。</summary>
    private const string LocalApiUrl = "http://localhost:5300";

    private const string LocalApiPort = "5300";

    // 与 LocalWebApiTestBase 同源的测试专用 JWT 密钥/密码（两套服务器各自签发，测试内分别登录）
    private const string TestJwtSecret = "LYBT-LocalWebAPI-Secret-Key-2024-DoNotUseInProduction";

    private readonly TokenHolder _switchingToken = new();

    private sealed record LocalApiHost(WebApplication App, HttpClient Client, string ConnectionString) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.StopAsync();
            await App.DisposeAsync();
            TryDropDatabase(ConnectionString);
        }

        private static void TryDropDatabase(string connectionString)
        {
            try
            {
                var database = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString).InitialCatalog;
                using var master = new Microsoft.Data.SqlClient.SqlConnection(
                    connectionString.Replace($"Database={database}", "Database=master"));
                master.Open();
                using var command = master.CreateCommand();
                command.CommandText =
                    $"IF DB_ID('{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END";
                command.ExecuteNonQuery();
            }
            catch
            {
                // 测试清理尽力而为（与基类一致：LocalDB 残留不影响后续用例）
            }
        }
    }

    /// <summary>
    /// 启动嵌入式 LocalWebAPI（localhost:5300 + 独立 LocalDB + 4 角色种子）。
    /// 端口被占用时立即抛出——避免误连开发者本机正在运行的真实 LocalWebAPI。
    /// </summary>
    private static async Task<LocalApiHost> StartLocalApiAsync()
    {
        var database = $"LYBTZYZS_ModeSwitchLocal_{Guid.NewGuid():N}";
        var connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database={database};Trusted_Connection=True;TrustServerCertificate=True";

        var builder = LYBT.LocalWebAPI.LocalWebApiProgram.CreateBuilder();
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
        builder.Configuration["Jwt:SecretKey"] = TestJwtSecret;
        builder.Configuration["DefaultPasswords:SysAdminPassword"] = "SysAdmin@2026!";
        builder.Configuration["DefaultPasswords:AdminPassword"] = "Admin@123456";
        builder.Configuration["DefaultPasswords:NewUserPassword"] = "User@123456";
        builder.Configuration["DefaultPasswords:ForceChangeOnFirstLogin"] = "false";
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 5300);
            options.Listen(IPAddress.IPv6Loopback, 5300);
        });

        var app = LYBT.LocalWebAPI.LocalWebApiProgram.CreateApplication(builder, connectionString);
        try
        {
            await app.StartAsync();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            await app.DisposeAsync();
            throw new InvalidOperationException(
                $"无法绑定 {LocalApiUrl}（端口 {LocalApiPort} 已被占用——本机可能正运行嵌入式 LocalWebAPI）。" +
                "模式切换 E2E 需要独占该端口，请在关闭桌面应用后重跑。",
                ex);
        }

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            await LYBT.Module.Identity.Services.IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<LYBT.Entities.Users.ApplicationUser>>();
            await EnsureLocalApiUserAsync(userManager, "admin", "管理员", "admin@lybtzyzs.local", "Admin@123456",
                LYBT.Infrastructure.Constants.RoleConstants.Admin, UserRole.Admin);

            await LYBT.LocalWebAPI.Data.LocalWebApiSeedData.SeedAsync(db, scope.ServiceProvider);
        }

        var client = new HttpClient { BaseAddress = new Uri(LocalApiUrl), Timeout = TimeSpan.FromSeconds(60) };
        return new LocalApiHost(app, client, connectionString);
    }

    private static async Task EnsureLocalApiUserAsync(
        UserManager<LYBT.Entities.Users.ApplicationUser> userManager,
        string userName,
        string realName,
        string email,
        string password,
        string role,
        UserRole userRole)
    {
        if (await userManager.FindByNameAsync(userName) != null)
            return;

        var user = new LYBT.Entities.Users.ApplicationUser
        {
            UserName = userName,
            RealName = realName,
            Email = email,
            Role = userRole,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        if ((await userManager.CreateAsync(user, password)).Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }

    private static async Task<string> LoginOnAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { userName, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("success").GetBoolean(), json.ToString());
        return json.GetProperty("data").GetProperty("token").GetString()!;
    }

    /// <summary>把令牌同时挂到 SwitchingApiClient 的网络工厂与直连 HttpClient 上。</summary>
    private void SetSwitchingToken(HttpClient directClient, string token)
    {
        _switchingToken.AccessToken = token;
        directClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private sealed class SwitchingHttpClientFactory(TokenHolder tokenHolder) : IHttpClientFactory
    {
        public string BaseUrl { get; set; } = LocalApiUrl;

        public HttpClient CreateClient(string name) => new(
            new AuthenticationDelegatingHandler(tokenHolder) { InnerHandler = new HttpClientHandler() })
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    private static RefitSettings CreateRefitSettings() => new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        })
    };

    /// <summary>
    /// 组装真实模式切换栈（设置服务 + 模式服务 + 切换客户端），远程 URL 指向基类服务器。
    /// </summary>
    private (IConnectionSettingsService Settings, IConnectionModeService Mode, SwitchingApiClient ApiClient, string SettingsPath)
        CreateSwitchingStack(string preferredMode, bool configureRemoteUrl)
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), $"lybt-e2e-usersettings-{Guid.NewGuid():N}.json");
        var options = new ApiClientOptions
        {
            BaseUrl = preferredMode == "Remote" ? Client.BaseAddress!.ToString().TrimEnd('/') : LocalApiUrl,
            RemoteUrl = configureRemoteUrl ? Client.BaseAddress!.ToString().TrimEnd('/') : string.Empty,
            PreferredMode = preferredMode
        };

        var settings = new ConnectionSettingsService(
            Options.Create(options),
            NullLogger<ConnectionSettingsService>.Instance,
            settingsPath);
        var modeService = new ConnectionModeService(
            settings,
            new ApplicationStateService(null, Options.Create(options), NullLogger<ApplicationStateService>.Instance),
            new ThrowingApiClient(),
            NullLogger<ConnectionModeService>.Instance);

        var localFactory = new SwitchingHttpClientFactory(_switchingToken) { BaseUrl = LocalApiUrl };
        var apiClient = new SwitchingApiClient(
            settings,
            url => new HttpClient(new AuthenticationDelegatingHandler(_switchingToken) { InnerHandler = new HttpClientHandler() })
            {
                BaseAddress = new Uri(url),
                Timeout = TimeSpan.FromSeconds(60)
            },
            _ => localFactory,
            CreateRefitSettings(),
            NullLogger<SwitchingApiClient>.Instance);

        return (settings, modeService, apiClient, settingsPath);
    }

    private static PatientInputDto NewPatient() => new()
    {
        Name = UniqueName("模式患者"),
        PhoneNumber = UniquePhone(),
        Gender = Gender.Male,
        BirthDate = new DateTime(1988, 3, 12)
    };

    private static async Task<JsonElement> GetPatientAsync(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"/api/v1/patients/{id}");
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"意外的状态码 {(int)response.StatusCode}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>US-SHELL-007：远程 → 本地切换成功，且流量真实落到嵌入式 LocalWebAPI。</summary>
    [Fact]
    public async Task SwitchToLocalStorage_Succeeds()
    {
        await using var localApi = await StartLocalApiAsync();
        var (settings, mode, apiClient, settingsPath) = CreateSwitchingStack("Remote", configureRemoteUrl: true);
        using (apiClient)
        {
            try
            {
                Assert.Equal(ConnectionMode.Remote, mode.CurrentMode);

                // Act
                var result = await mode.SetModeAsync(ConnectionMode.Local);

                // Assert — 模式状态
                Assert.True(result.Succeeded, result.Message);
                Assert.Equal(ConnectionMode.Local, mode.CurrentMode);
                Assert.True(mode.IsLocal);
                Assert.False(mode.IsRemote);
                Assert.Equal("本地模式", mode.CurrentModeDisplay);
                Assert.Equal("本地 WebAPI 已连接", mode.ApiStatusDisplay);
                Assert.Equal("Local", settings.PreferredMode);
                Assert.Contains(LocalApiPort, settings.CurrentUrl);

                // Assert — 持久化：新建服务实例读取同一用户设置文件仍是 Local
                var reloaded = new ConnectionSettingsService(
                    Options.Create(new ApiClientOptions()),
                    NullLogger<ConnectionSettingsService>.Instance,
                    settingsPath);
                Assert.Equal("Local", reloaded.PreferredMode);
                Assert.True(reloaded.IsLocal);

                // Assert — 路由：本地模式请求走嵌入式 LocalWebAPI（本地可查、远程不可见）
                var localToken = await LoginOnAsync(localApi.Client, "admin", "Admin@123456");
                await LoginAsAdminAsync(); // 远程腿令牌（供隔离断言使用）
                SetSwitchingToken(localApi.Client, localToken);

                var input = NewPatient();
                var created = await apiClient.Patients.CreatePatientAsync(input);
                Assert.True(created.Success, created.Message);

                var onLocal = await GetPatientAsync(localApi.Client, created.Data!.Id);
                Assert.True(onLocal.GetProperty("success").GetBoolean(), onLocal.ToString());
                Assert.Equal(input.Name, onLocal.GetProperty("data").GetProperty("name").GetString());

                var onRemote = await GetPatientAsync(Client, created.Data.Id);
                Assert.Equal("患者不存在", onRemote.GetProperty("message").GetString());
            }
            finally
            {
                File.Delete(settingsPath);
            }
        }
    }

    /// <summary>US-SHELL-007：本地 → 远程切换成功，且流量真实落到远程 WebAPI。</summary>
    [Fact]
    public async Task SwitchToRemoteStorage_Succeeds()
    {
        await using var localApi = await StartLocalApiAsync();
        var (settings, mode, apiClient, settingsPath) = CreateSwitchingStack("Local", configureRemoteUrl: true);
        using (apiClient)
        {
            try
            {
                Assert.Equal(ConnectionMode.Local, mode.CurrentMode);

                // Act
                var result = await mode.SetModeAsync(ConnectionMode.Remote);

                // Assert — 模式状态
                Assert.True(result.Succeeded, result.Message);
                Assert.Equal(ConnectionMode.Remote, mode.CurrentMode);
                Assert.True(mode.IsRemote);
                Assert.False(mode.IsLocal);
                Assert.Equal("远程模式", mode.CurrentModeDisplay);
                Assert.Equal("远程 WebAPI 已连接", mode.ApiStatusDisplay);
                Assert.Equal("Remote", settings.PreferredMode);
                Assert.Equal(Client.BaseAddress!.ToString().TrimEnd('/'), settings.CurrentUrl);

                // Assert — 路由：远程模式请求走远程服务器（远程可查、本地不可见）
                var remoteSession = await LoginAsAdminAsync();
                _switchingToken.AccessToken = remoteSession.Token;

                var input = NewPatient();
                var created = await apiClient.Patients.CreatePatientAsync(input);
                Assert.True(created.Success, created.Message);

                var onRemote = await GetPatientAsync(Client, created.Data!.Id);
                Assert.True(onRemote.GetProperty("success").GetBoolean(), onRemote.ToString());
                Assert.Equal(input.Name, onRemote.GetProperty("data").GetProperty("name").GetString());

                var localToken = await LoginOnAsync(localApi.Client, "admin", "Admin@123456");
                SetSwitchingToken(localApi.Client, localToken);
                var onLocal = await GetPatientAsync(localApi.Client, created.Data.Id);
                Assert.Equal("患者不存在", onLocal.GetProperty("message").GetString());
            }
            finally
            {
                File.Delete(settingsPath);
            }
        }
    }

    /// <summary>US-SHELL-007：两种模式各自持有独立数据存储——互不可见（数据隔离）。</summary>
    [Fact]
    public async Task ModeSwitch_DataIsolation()
    {
        await using var localApi = await StartLocalApiAsync();
        var (settings, mode, apiClient, settingsPath) = CreateSwitchingStack("Local", configureRemoteUrl: true);
        using (apiClient)
        {
            try
            {
                // 本地模式：建患者 A
                var localToken = await LoginOnAsync(localApi.Client, "admin", "Admin@123456");
                var localInput = NewPatient();
                SetSwitchingToken(localApi.Client, localToken);
                var localPatient = await apiClient.Patients.CreatePatientAsync(localInput);
                Assert.True(localPatient.Success, localPatient.Message);

                // 切远程：建患者 B
                var remoteSession = await LoginAsAdminAsync();
                Assert.True((await mode.SetModeAsync(ConnectionMode.Remote)).Succeeded);
                var remoteInput = NewPatient();
                _switchingToken.AccessToken = remoteSession.Token;
                var remotePatient = await apiClient.Patients.CreatePatientAsync(remoteInput);
                Assert.True(remotePatient.Success, remotePatient.Message);

                // 切回本地：A 仍在、B 不在
                Assert.True((await mode.SetModeAsync(ConnectionMode.Local)).Succeeded);
                SetSwitchingToken(localApi.Client, localToken);

                var aOnLocal = await GetPatientAsync(localApi.Client, localPatient.Data!.Id);
                var bOnLocal = await GetPatientAsync(localApi.Client, remotePatient.Data!.Id);
                var aOnRemote = await GetPatientAsync(Client, localPatient.Data.Id);
                var bOnRemote = await GetPatientAsync(Client, remotePatient.Data.Id);

                Assert.True(aOnLocal.GetProperty("success").GetBoolean(), aOnLocal.ToString());
                Assert.Equal("患者不存在", bOnLocal.GetProperty("message").GetString());
                Assert.True(bOnRemote.GetProperty("success").GetBoolean(), bOnRemote.ToString());
                Assert.Equal("患者不存在", aOnRemote.GetProperty("message").GetString());
            }
            finally
            {
                File.Delete(settingsPath);
            }
        }
    }

    /// <summary>US-SHELL-007：未配置远程地址时切远程被阻断（NO_REMOTE_URL）且模式回退不变。</summary>
    [Fact]
    public async Task SwitchToRemote_WithoutRemoteUrl_IsBlocked()
    {
        var (settings, mode, apiClient, settingsPath) = CreateSwitchingStack("Local", configureRemoteUrl: false);
        using (apiClient)
        {
            try
            {
                // Act
                var blocked = await mode.SetModeAsync(ConnectionMode.Remote);

                // Assert — 阻断且保持切换前模式
                Assert.False(blocked.Succeeded);
                Assert.Equal("NO_REMOTE_URL", blocked.ErrorCode);
                Assert.False(string.IsNullOrWhiteSpace(blocked.Message));
                Assert.Equal(ConnectionMode.Local, mode.CurrentMode);
                Assert.Equal("Local", settings.PreferredMode);

                // 阻断后可正常切回本地（幂等、无副作用）
                var local = await mode.SetModeAsync(ConnectionMode.Local);
                Assert.True(local.Succeeded);
                Assert.Equal(ConnectionMode.Local, mode.CurrentMode);
            }
            finally
            {
                File.Delete(settingsPath);
            }
        }
    }

    /// <summary>模式服务依赖的 IApiClient 桩：本套件不验证后台探测，禁止其发起真实医疗案查询。</summary>
    private sealed class ThrowingApiClient : LYBT.Desktop.Contracts.ApiClient.IApiClient
    {
        private static Exception Unsupported() =>
            new NotSupportedException("模式切换 E2E 不验证后台探测路径");

        public LYBT.Desktop.Contracts.ApiClient.IApiClientIdentity Identity => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientPatients Patients => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientHerbs Herbs => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientFormulas Formulas => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientMedicalCases MedicalCases => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientRegistrations Registrations => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientReports Reports => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientDeploy Deploy => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientDiagnostics Diagnostics => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientConfiguration Configuration => throw Unsupported();
        public LYBT.Desktop.Contracts.ApiClient.IApiClientBackup Backup => throw Unsupported();
    }
}
