using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace LYBT.Desktop.Foundation.HealthCheck;

/// <summary>
/// WebAPI 健康检查服务实现
/// </summary>
public class ApiHealthCheckService : IApiHealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public string? LastErrorMessage { get; private set; }

    public ApiHealthCheckService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// 异步检查 WebAPI 连接状态
    /// </summary>
    public async Task<ApiHealthStatus> CheckHealthAsync(int timeout = 5000)
    {
        LastErrorMessage = null;

        try
        {
            // 从配置中读取 WebAPI BaseUrl
            var baseUrl = _configuration["Lybt:Client:Api:BaseUrl"] ?? "https://localhost:5001";
            var healthUrl = $"{baseUrl.TrimEnd('/')}/health";

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
