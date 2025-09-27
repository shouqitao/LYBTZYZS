using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using LYBT.Shared.Interfaces.Api;
using Microsoft.Extensions.Logging;
using Refit;

namespace LYBT.Desktop.Infrastructure.Api;

/// <summary>
/// 统一API客户端管理器实现
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// 集中管理所有8个业务模块的API客户端，提供类型安全的REST API访问
/// 支持认证令牌管理、健康检查、连接配置等企业级功能
/// </summary>
/// <param name="httpClient">HTTP客户端实例，用于发送REST请求</param>
/// <param name="logger">日志记录器，用于记录API操作和异常</param>
/// <exception cref="ArgumentNullException">当任何参数为 null 时抛出</exception>
public class UnifiedApiClientManager(HttpClient httpClient, ILogger<UnifiedApiClientManager> logger)
    : IUnifiedApiClientManager, IDisposable
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<UnifiedApiClientManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly RefitSettings _refitSettings = CreateRefitSettings();

    // API客户端实例 - 使用延迟初始化提升性能
    private readonly Lazy<IAuthApi> _authApi = new(() => RestService.For<IAuthApi>(httpClient, CreateRefitSettings()));

    private readonly Lazy<IUserApi> _userApi = new(() => RestService.For<IUserApi>(httpClient, CreateRefitSettings()));
    private readonly Lazy<IPatientApi> _patientApi = new(() => RestService.For<IPatientApi>(httpClient, CreateRefitSettings()));
    private readonly Lazy<IMedicalCaseApi> _medicalCaseApi = new(() => RestService.For<IMedicalCaseApi>(httpClient, CreateRefitSettings()));
    private readonly Lazy<IConsultationApi> _consultationApi = new(() => RestService.For<IConsultationApi>(httpClient, CreateRefitSettings()));
    private readonly Lazy<IPrescriptionApi> _prescriptionApi = new(() => RestService.For<IPrescriptionApi>(httpClient, CreateRefitSettings()));
    private readonly Lazy<IHerbApi> _herbApi = new(() => RestService.For<IHerbApi>(httpClient, CreateRefitSettings()));
    private readonly Lazy<IFormulaApi> _formulaApi = new(() => RestService.For<IFormulaApi>(httpClient, CreateRefitSettings()));

    private bool _disposed;

    // 实例构造函数体 - 初始化HTTP客户端配置
    static UnifiedApiClientManager()
    {
        // 静态初始化可以在这里添加全局配置
    }

    /// <summary>
    /// 实例初始化 - 配置HTTP客户端和记录日志
    /// </summary>
    private void InitializeApiManager()
    {
        ConfigureHttpClient();
        _logger.LogInformation(
            "统一API客户端管理器初始化完成 - 基地址: {BaseAddress}",
            _httpClient.BaseAddress);
    }

    /// <summary>
    /// 创建并初始化API客户端管理器
    /// </summary>
    public static UnifiedApiClientManager Create(HttpClient httpClient, ILogger<UnifiedApiClientManager> logger)
    {
        var manager = new UnifiedApiClientManager(httpClient, logger);
        manager.InitializeApiManager();
        return manager;
    }

    #region API客户端属性

    /// <summary>
    /// 获取身份认证API客户端
    /// </summary>
    /// <value>用于处理用户登录、注销、令牌刷新等认证操作的API客户端</value>
    public IAuthApi AuthApi => _authApi.Value;

    /// <summary>
    /// 获取用户管理API客户端
    /// </summary>
    /// <value>用于处理用户CRUD操作、角色管理等的API客户端</value>
    public IUserApi UserApi => _userApi.Value;

    /// <summary>
    /// 获取患者档案API客户端
    /// </summary>
    /// <value>用于处理患者信息管理、病历查询等的API客户端</value>
    public IPatientApi PatientApi => _patientApi.Value;

    /// <summary>
    /// 获取医疗案例API客户端
    /// </summary>
    /// <value>用于处理医疗案例管理、诊疗流程控制的API客户端</value>
    public IMedicalCaseApi MedicalCaseApi => _medicalCaseApi.Value;

    /// <summary>
    /// 获取诊疗咨询API客户端
    /// </summary>
    /// <value>用于处理中医四诊、辨证论治等诊疗操作的API客户端</value>
    public IConsultationApi ConsultationApi => _consultationApi.Value;

    /// <summary>
    /// 获取处方管理API客户端
    /// </summary>
    /// <value>用于处理处方开具、药材配伍、打印输出的API客户端</value>
    public IPrescriptionApi PrescriptionApi => _prescriptionApi.Value;

    /// <summary>
    /// 获取中药材管理API客户端
    /// </summary>
    /// <value>用于处理中药材信息维护、用法管理的API客户端</value>
    public IHerbApi HerbApi => _herbApi.Value;

    /// <summary>
    /// 获取验方管理API客户端
    /// </summary>
    /// <value>用于处理经典验方、个人验方模板管理的API客户端</value>
    public IFormulaApi FormulaApi => _formulaApi.Value;

    #endregion API客户端属性

    #region 公共方法

    /// <summary>
    /// 设置认证令牌
    /// 用于JWT Bearer Token认证，支持令牌设置和清除
    /// </summary>
    /// <param name="token">JWT认证令牌，空值将清除当前令牌</param>
    public void SetAuthorizationToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _logger.LogInformation("已清除认证令牌");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogInformation("已设置认证令牌 - 长度: {TokenLength}", token.Length);
        }
    }

    /// <summary>
    /// 更新API基地址
    /// 动态切换API服务器地址，支持开发、测试、生产环境切换
    /// </summary>
    /// <param name="baseUrl">新的API基地址，必须是有效的绝对URL</param>
    /// <exception cref="ArgumentException">当基地址为空或格式无效时抛出</exception>
    public void UpdateBaseAddress(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl, nameof(baseUrl));

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"基地址格式无效: {baseUrl}", nameof(baseUrl));
        }

        _httpClient.BaseAddress = uri;
        _logger.LogInformation(
            "API基地址已更新: {OldBaseUrl} → {NewBaseUrl}",
            _httpClient.BaseAddress, baseUrl);
    }

    /// <summary>
    /// 检查API连接健康状态
    /// 向服务器发送健康检查请求，验证连接和服务可用性
    /// </summary>
    /// <returns>如果API服务健康则返回 true；否则返回 false</returns>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/v1/health").ConfigureAwait(false);
            var isHealthy = response.IsSuccessStatusCode;

            _logger.LogInformation(
                "API健康检查结果: {HealthStatus}, 状态码: {StatusCode}, 响应时间: {ResponseTime}ms",
                isHealthy ? "健康" : "异常",
                response.StatusCode,
                response.Headers.Date?.Subtract(DateTime.UtcNow).TotalMilliseconds ?? 0);

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API健康检查失败 - 基地址: {BaseAddress}", _httpClient.BaseAddress);
            return false;
        }
    }

    /// <summary>
    /// 获取当前API基地址
    /// </summary>
    /// <returns>当前配置的API基地址，如果未设置则返回 null</returns>
    public string? GetCurrentBaseAddress()
    {
        return _httpClient.BaseAddress?.ToString();
    }

    /// <summary>
    /// 获取连接状态信息
    /// 提供详细的连接状态和配置信息，用于诊断和监控
    /// </summary>
    /// <returns>包含连接状态、配置信息的状态对象</returns>
    public async Task<ApiConnectionStatus> GetConnectionStatusAsync()
    {
        var status = new ApiConnectionStatus
        {
            BaseAddress = GetCurrentBaseAddress(),
            HasAuthToken = _httpClient.DefaultRequestHeaders.Authorization != null,
            LastCheckTime = DateTime.Now
        };

        try
        {
            var startTime = DateTime.Now;
            var isHealthy = await CheckHealthAsync();
            var responseTime = (DateTime.Now - startTime).TotalMilliseconds;

            status.IsHealthy = isHealthy;
            status.ResponseTimeMs = responseTime;
            status.StatusMessage = isHealthy ? "连接正常" : "连接异常";
        }
        catch (Exception ex)
        {
            status.IsHealthy = false;
            status.ResponseTimeMs = -1;
            status.StatusMessage = $"连接检查失败: {ex.Message}";
        }

        return status;
    }

    #endregion 公共方法

    #region 私有方法

    /// <summary>
    /// 创建Refit配置设置
    /// 配置JSON序列化、错误处理等Refit特定设置
    /// </summary>
    /// <returns>配置完成的RefitSettings实例</returns>
    private static RefitSettings CreateRefitSettings()
    {
        return new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })
        };
    }

    /// <summary>
    /// 配置HttpClient基础设置
    /// 设置超时、请求头、基地址等HTTP客户端配置
    /// </summary>
    private void ConfigureHttpClient()
    {
        // 设置企业级超时时间
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // 设置标准化请求头
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LYBT-Desktop-Client/2.1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("X-Client-Version", "2.1.0");
        _httpClient.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        _httpClient.DefaultRequestHeaders.Add("X-Client-Platform", Environment.OSVersion.ToString());

        // 如果BaseAddress未设置，使用默认地址（Windows 本地部署：5001）
        _httpClient.BaseAddress ??= new Uri("http://localhost:5001");

        _logger.LogDebug(
            "HttpClient配置完成 - 基地址: {BaseAddress}, 超时: {Timeout}s",
            _httpClient.BaseAddress, _httpClient.Timeout.TotalSeconds);
    }

    #endregion 私有方法

    #region IDisposable实现

    /// <summary>
    /// 释放由该类使用的资源
    /// </summary>
    /// <param name="disposing">如果为 true，则释放托管和非托管资源；如果为 false，则仅释放非托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                try
                {
                    // 清理认证令牌
                    _httpClient.DefaultRequestHeaders.Authorization = null;

                    // 释放HTTP客户端
                    _httpClient?.Dispose();

                    _logger.LogInformation(
                        "统一API客户端管理器已释放资源 - 基地址: {BaseAddress}",
                        _httpClient?.BaseAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "释放API客户端管理器资源时发生异常");
                }
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// 释放由该对象使用的所有资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable实现
}
