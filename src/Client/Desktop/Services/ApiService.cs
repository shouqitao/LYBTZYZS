using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 真实的API服务实现
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenManager _tokenManager;

        public ApiService(HttpClient httpClient, ITokenManager tokenManager)
        {
            _httpClient = httpClient;
            _tokenManager = tokenManager;

            // 设置基础URL - 使用配置文件中的地址
            _httpClient.BaseAddress = new Uri(LYBT.Desktop.Core.Configuration.ApiConfiguration.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LYBT.WPF.Client");
        }

        /// <summary>
        /// 发送GET请求
        /// </summary>
        public async Task<ServiceResult<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                await EnsureAuthenticated();
                var response = await _httpClient.GetAsync(endpoint);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<T>.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        public async Task<ServiceResult<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                // 登录请求不需要添加Authorization头
                if (!endpoint.Contains("login", StringComparison.OrdinalIgnoreCase))
                {
                    await EnsureAuthenticated();
                }

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<T>.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// 发送PUT请求
        /// </summary>
        public async Task<ServiceResult<T>> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                await EnsureAuthenticated();
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(endpoint, content);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<T>.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// 发送DELETE请求
        /// </summary>
        public async Task<ServiceResult<T>> DeleteAsync<T>(string endpoint)
        {
            try
            {
                await EnsureAuthenticated();
                var response = await _httpClient.DeleteAsync(endpoint);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<T>.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// 发送PATCH请求
        /// </summary>
        public async Task<ServiceResult<T>> PatchAsync<T>(string endpoint, object data)
        {
            try
            {
                await EnsureAuthenticated();
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync(endpoint, content);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<T>.Failure(ex.Message, ex);
            }
        }

        /// <summary>
        /// 确保已认证
        /// </summary>
        private Task EnsureAuthenticated()
        {
            var token = _tokenManager.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 处理HTTP响应
        /// </summary>
        private async Task<ServiceResult<T>> ProcessResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // 现在后端直接返回数据，不再包装在 ApiResponse 中
                    if (string.IsNullOrWhiteSpace(content) || content == "null")
                    {
                        return ServiceResult<T>.Success(default(T)!);
                    }

                    var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });
                    return ServiceResult<T>.Success(result!);
                }
                catch (JsonException ex)
                {
                    return ServiceResult<T>.Failure($"响应数据格式错误: {ex.Message}", ex);
                }
            }
            else
            {
                // 尝试解析错误响应
                string errorMessage;
                try
                {
                    var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });
                    errorMessage = problemDetails?.Detail ?? problemDetails?.Title ?? $"请求失败: {response.StatusCode}";
                }
                catch
                {
                    errorMessage = $"请求失败: {response.StatusCode} - {content}";
                }

                return ServiceResult<T>.Failure(errorMessage);
            }
        }
    }
}