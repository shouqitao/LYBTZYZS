using System.Net.Http;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Options;

namespace LYBT.Desktop.Foundation.HealthCheck;

/// <summary>
/// WebAPI 健康检查服务实现
/// </summary>
public class ApiHealthCheckService : IApiHealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly ApiClientOptions _apiOptions;
    private readonly IConnectionSettingsService? _connectionSettings;

    public string? LastErrorMessage { get; private set; }

    public ApiHealthCheckService(
        HttpClient httpClient,
        IOptions<ApiClientOptions> apiOptions,
        IConnectionSettingsService? connectionSettings = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
        _connectionSettings = connectionSettings;
    }

    /// <summary>
    /// 异步检查 WebAPI 连接状态
    /// </summary>
    public async Task<ApiHealthStatus> CheckHealthAsync(int timeout = 5000)
    {
        LastErrorMessage = null;

        try
        {
            // 健康检查目标跟随当前连接模式（用户配置的 RemoteUrl/LocalUrl），
            // 回退 appsettings 静态 BaseUrl。路径统一为 /api/v1/health：
            // LocalWebAPI 仅暴露该路径（无 /health 中间件端点），Server 双路径均可用且匿名。
            var baseUrl = !string.IsNullOrWhiteSpace(_connectionSettings?.CurrentUrl)
                ? _connectionSettings!.CurrentUrl
                : _apiOptions.BaseUrl;
            var healthUrl = $"{baseUrl.TrimEnd('/')}/api/v1/health";

            using var cts = new CancellationTokenSource(timeout);

            var response = await _httpClient.GetAsync(healthUrl, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return ApiHealthStatus.Healthy;
            }

            LastErrorMessage = $"服务器响应异常: HTTP {(int)response.StatusCode}";
            return ApiHealthStatus.Unhealthy;
        }
        catch (TaskCanceledException)
        {
            LastErrorMessage = $"连接超时({timeout}ms)";
            return ApiHealthStatus.Unhealthy;
        }
        catch (HttpRequestException)
        {
            LastErrorMessage = "网络连接失败，请稍后重试";
            return ApiHealthStatus.Unhealthy;
        }
        catch (Exception)
        {
            LastErrorMessage = "健康检查失败，请稍后重试";
            return ApiHealthStatus.Unhealthy;
        }
    }
}
