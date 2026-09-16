// ---------------------------------------------------------------------------
// E2ETestBase — 端到端集成测试基类（真实链路：桌面 IApiClient → LocalWebAPI → LocalDB）
// ---------------------------------------------------------------------------
// 复用 LocalWebApiTestBase（真实 LocalWebAPI Kestrel 内存启动 + 每测试类独立 LocalDB +
// 4 角色种子数据 + Token 管理），并注入真实桌面 API 客户端实现（LYBT.Desktop.Foundation
// 的 {Domain}HttpApiClient 本地模式适配器，InternalsVisibleTo 已授权）。
// 不 mock 任何东西：客户端走真实 HTTP → 真实控制器 → 真实服务 → 真实 SQL Server LocalDB。
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Foundation.Http.Clients;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Desktop.E2E;

/// <summary>
/// E2E 集成测试基类 — 继承 LocalWebApiTestBase 获得真实 LocalWebAPI 启动 / 独立 LocalDB /
/// 角色种子 / Token 管理 / 唯一数据辅助，另注入 8 个真实桌面 API 客户端。
/// LocalWebApiTestBase.InitializeAsync 非 virtual（xUnit 直接调用基类实现），
/// 客户端改为惰性初始化：基类启动完成（Client.BaseAddress 就绪）后首次访问才构造。
/// </summary>
public abstract class E2ETestBase : Integration.LocalApi.LocalWebApiTestBase, IDisposable
{
    private bool _disposed;

    private readonly TokenHolder _tokenHolder = new();
    // 角色级 Token 缓存：LocalWebAPI 登录限流 5 次/分钟（FixedWindow，按 app 实例独立桶），
    // 每测试类每角色只登录一次，避免 8 测试/类的用例触发 429 偶发失败
    private readonly Dictionary<string, LoginResponse> _roleSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    private IHttpClientFactory? _factory;
    private IApiClientIdentity? _identityApi;
    private IApiClientPatients? _patientsApi;
    private IApiClientHerbs? _herbsApi;
    private IApiClientFormulas? _formulasApi;
    private IApiClientMedicalCases? _medicalCasesApi;
    private IApiClientRegistrations? _registrationsApi;
    private IApiClientDiagnostics? _diagnosticsApi;
    private IApiClientReports? _reportsApi;

    private IHttpClientFactory Factory
        => _factory ??= new TestHttpClientFactory(Client.BaseAddress!, _tokenHolder);

    // ========== 真实桌面 API 客户端（本地模式适配器，惰性构造） ==========

    /// <summary>认证 + 用户管理（/api/v1/auth/* + /api/v1/users/*）</summary>
    protected IApiClientIdentity IdentityApi
        => _identityApi ??= new IdentityHttpApiClient(Factory, NullLogger<IdentityHttpApiClient>.Instance);

    /// <summary>患者 CRUD/搜索/批量（/api/v1/patients/*）</summary>
    protected IApiClientPatients PatientsApi
        => _patientsApi ??= new PatientsHttpApiClient(Factory, NullLogger<PatientsHttpApiClient>.Instance);

    /// <summary>药材 CRUD/批量/导入导出（/api/v1/herbs/*）</summary>
    protected IApiClientHerbs HerbsApi
        => _herbsApi ??= new HerbsHttpApiClient(Factory, NullLogger<HerbsHttpApiClient>.Instance);

    /// <summary>验方 CRUD/克隆/导入（/api/v1/formulas/*）</summary>
    protected IApiClientFormulas FormulasApi
        => _formulasApi ??= new FormulasHttpApiClient(Factory, NullLogger<FormulasHttpApiClient>.Instance);

    /// <summary>医案生命周期（/api/v1/medicalcases/*）</summary>
    protected IApiClientMedicalCases MedicalCasesApi
        => _medicalCasesApi ??= new MedicalCasesHttpApiClient(Factory, NullLogger<MedicalCasesHttpApiClient>.Instance);

    /// <summary>挂号/队列/接诊（/api/v1/registrations/*）</summary>
    protected IApiClientRegistrations RegistrationsApi
        => _registrationsApi ??= new RegistrationsHttpApiClient(Factory, NullLogger<RegistrationsHttpApiClient>.Instance);

    /// <summary>诊断/日志（/api/v1/diagnostics/*）</summary>
    protected IApiClientDiagnostics DiagnosticsApi
        => _diagnosticsApi ??= new DiagnosticsHttpApiClient(Factory, NullLogger<DiagnosticsHttpApiClient>.Instance);

    /// <summary>报表（/api/v1/reports/*）</summary>
    protected IApiClientReports ReportsApi
        => _reportsApi ??= new ReportsHttpApiClient(Factory, NullLogger<ReportsHttpApiClient>.Instance);

    // ========== 角色登录辅助 ==========

    protected Task<LoginResponse> LoginAsAdminAsync() => LoginAndSetTokenAsync("admin", "Admin@123456");
    protected Task<LoginResponse> LoginAsSysadminAsync() => LoginAndSetTokenAsync("sysadmin", "SysAdmin@2026!");
    protected Task<LoginResponse> LoginAsDoctorAsync() => LoginAndSetTokenAsync("doctor", "Doctor@123456");
    protected Task<LoginResponse> LoginAsReceptionistAsync() => LoginAndSetTokenAsync("receptionist", "Receptionist@123456");

    /// <summary>
    /// 登录并设置认证状态。每测试类每角色仅登录一次（缓存 Token），
    /// 规避 LocalWebAPI 登录限流（5 次/分钟/实例）在 8 测试/类场景下的 429。
    /// </summary>
    protected async Task<LoginResponse> LoginAndSetTokenAsync(string username, string password)
    {
        await _loginLock.WaitAsync();
        try
        {
            if (_roleSessions.TryGetValue(username, out var cached))
            {
                SetToken(cached.Token);
                return cached;
            }

            var response = await IdentityApi.LoginAsync(new LoginRequest { UserName = username, Password = password });
            Assert.True(response.Success, $"登录失败: {response.Message}");
            Assert.NotNull(response.Data);

            _roleSessions[username] = response.Data!;
            SetToken(response.Data!.Token);
            return response.Data!;
        }
        finally
        {
            _loginLock.Release();
        }
    }

    /// <summary>清除全部认证状态。</summary>
    protected void ClearAuth()
    {
        _tokenHolder.AccessToken = null;
        ClearAuthHeader();
    }

    /// <summary>设置当前认证 Token（同时作用于桌面客户端与原始 Client）。</summary>
    protected void SetToken(string token)
    {
        _tokenHolder.AccessToken = token;
        SetAuthHeader(token);
    }

    /// <summary>断言桌面客户端调用返回 403（HttpRequestException.StatusCode）。</summary>
    protected static async Task AssertForbiddenAsync(Func<Task> action)
    {
        var ex = await Assert.ThrowsAnyAsync<HttpRequestException>(action);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, ex.StatusCode);
    }

    /// <summary>断言桌面客户端调用返回 401（HttpRequestException.StatusCode）。</summary>
    protected static async Task AssertUnauthorizedAsync(Func<Task> action)
    {
        var ex = await Assert.ThrowsAnyAsync<HttpRequestException>(action);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, ex.StatusCode);
    }
    /// <summary>释放登录锁（CA1001）。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _loginLock.Dispose();
    }

    /// <summary>
    /// 测试专用 IHttpClientFactory — 每个调用返回绑定到测试服务器地址 + 自动注入
    /// 当前 Token 的 HttpClient（与生产 DI 的命名客户端等价）。
    /// </summary>
    private sealed class TestHttpClientFactory(Uri baseAddress, TokenHolder tokenHolder) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var handler = new AuthenticationDelegatingHandler(tokenHolder)
            {
                InnerHandler = new HttpClientHandler()
            };
            return new HttpClient(handler)
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromSeconds(60)
            };
        }
    }
}
