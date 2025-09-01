using LYBT.Shared.Interfaces.Api;
using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace LYBT.Desktop.Infrastructure.Api;

/// <summary>
/// 统一API客户端管理器实现
/// 集中管理所有8个业务模块的API客户端
/// </summary>
public class UnifiedApiClientManager : IUnifiedApiClientManager, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UnifiedApiClientManager> _logger;
    private readonly RefitSettings _refitSettings;

    // API客户端实例
    private readonly Lazy<IAuthApi> _authApi;
    private readonly Lazy<IUserApi> _userApi;
    private readonly Lazy<IPatientApi> _patientApi;
    private readonly Lazy<IMedicalCaseApi> _medicalCaseApi;
    private readonly Lazy<IConsultationApi> _consultationApi;
    private readonly Lazy<IPrescriptionApi> _prescriptionApi;
    private readonly Lazy<IHerbApi> _herbApi;
    private readonly Lazy<IFormulaApi> _formulaApi;

    public UnifiedApiClientManager(HttpClient httpClient, ILogger<UnifiedApiClientManager> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 配置HttpClient基础设置
        ConfigureHttpClient();

        // 配置Refit设置
        _refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            })
        };

        // 延迟初始化API客户端
        _authApi = new Lazy<IAuthApi>(() => RestService.For<IAuthApi>(_httpClient, _refitSettings));
        _userApi = new Lazy<IUserApi>(() => RestService.For<IUserApi>(_httpClient, _refitSettings));
        _patientApi = new Lazy<IPatientApi>(() => RestService.For<IPatientApi>(_httpClient, _refitSettings));
        _medicalCaseApi = new Lazy<IMedicalCaseApi>(() => RestService.For<IMedicalCaseApi>(_httpClient, _refitSettings));
        _consultationApi = new Lazy<IConsultationApi>(() => RestService.For<IConsultationApi>(_httpClient, _refitSettings));
        _prescriptionApi = new Lazy<IPrescriptionApi>(() => RestService.For<IPrescriptionApi>(_httpClient, _refitSettings));
        _herbApi = new Lazy<IHerbApi>(() => RestService.For<IHerbApi>(_httpClient, _refitSettings));
        _formulaApi = new Lazy<IFormulaApi>(() => RestService.For<IFormulaApi>(_httpClient, _refitSettings));

        _logger.LogInformation("统一API客户端管理器初始化完成");
    }

    #region API客户端属性

    public IAuthApi AuthApi => _authApi.Value;
    public IUserApi UserApi => _userApi.Value;
    public IPatientApi PatientApi => _patientApi.Value;
    public IMedicalCaseApi MedicalCaseApi => _medicalCaseApi.Value;
    public IConsultationApi ConsultationApi => _consultationApi.Value;
    public IPrescriptionApi PrescriptionApi => _prescriptionApi.Value;
    public IHerbApi HerbApi => _herbApi.Value;
    public IFormulaApi FormulaApi => _formulaApi.Value;

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置认证令牌
    /// </summary>
    public void SetAuthorizationToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _logger.LogInformation("已清除认证令牌");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogInformation("已设置认证令牌");
        }
    }

    /// <summary>
    /// 更新API基地址
    /// </summary>
    public void UpdateBaseAddress(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("基地址不能为空", nameof(baseUrl));

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("基地址格式无效", nameof(baseUrl));

        _httpClient.BaseAddress = uri;
        _logger.LogInformation("API基地址已更新为: {BaseUrl}", baseUrl);
    }

    /// <summary>
    /// 检查API连接健康状态
    /// </summary>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/v1/health");
            var isHealthy = response.IsSuccessStatusCode;
            
            _logger.LogInformation("API健康检查结果: {IsHealthy}, 状态码: {StatusCode}", 
                isHealthy ? "健康" : "异常", response.StatusCode);
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API健康检查失败");
            return false;
        }
    }

    /// <summary>
    /// 获取当前API基地址
    /// </summary>
    public string? GetCurrentBaseAddress()
    {
        return _httpClient.BaseAddress?.ToString();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 配置HttpClient基础设置
    /// </summary>
    private void ConfigureHttpClient()
    {
        // 设置默认超时时间
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // 设置默认请求头
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LYBT-Desktop-Client/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("X-Client-Version", "1.0.0");

        // 如果BaseAddress未设置，使用默认地址
        _httpClient.BaseAddress ??= new Uri("https://localhost:7001");

        _logger.LogDebug("HttpClient配置完成，基地址: {BaseAddress}", _httpClient.BaseAddress);
    }

    #endregion

    #region IDisposable实现

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
                _logger.LogInformation("统一API客户端管理器已释放资源");
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}