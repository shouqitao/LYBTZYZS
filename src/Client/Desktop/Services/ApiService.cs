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

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// API服务 - UltraThink精简版
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenManager _tokenManager;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(HttpClient httpClient, ITokenManager tokenManager)
        {
            _httpClient = httpClient;
            _tokenManager = tokenManager;

            _httpClient.BaseAddress = new Uri(LYBT.Desktop.Core.Configuration.ApiConfiguration.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LYBT.WPF.Client");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<ServiceResult<T>> GetAsync<T>(string endpoint)
        {
            return await ExecuteAsync<T>(async () =>
            {
                await EnsureAuthenticated();
                return await _httpClient.GetAsync(endpoint);
            });
        }

        public async Task<ServiceResult<T>> PostAsync<T>(string endpoint, object data)
        {
            return await ExecuteAsync<T>(async () =>
            {
                if (!endpoint.Contains("login", StringComparison.OrdinalIgnoreCase))
                    await EnsureAuthenticated();

                var content = CreateJsonContent(data);
                return await _httpClient.PostAsync(endpoint, content);
            });
        }

        public async Task<ServiceResult<T>> PutAsync<T>(string endpoint, object data)
        {
            return await ExecuteAsync<T>(async () =>
            {
                await EnsureAuthenticated();
                var content = CreateJsonContent(data);
                return await _httpClient.PutAsync(endpoint, content);
            });
        }

        public async Task<ServiceResult<T>> DeleteAsync<T>(string endpoint)
        {
            return await ExecuteAsync<T>(async () =>
            {
                await EnsureAuthenticated();
                return await _httpClient.DeleteAsync(endpoint);
            });
        }

        public async Task<ServiceResult<T>> PatchAsync<T>(string endpoint, object data)
        {
            return await ExecuteAsync<T>(async () =>
            {
                await EnsureAuthenticated();
                var content = CreateJsonContent(data);
                return await _httpClient.PatchAsync(endpoint, content);
            });
        }

        private async Task<ServiceResult<T>> ExecuteAsync<T>(Func<Task<HttpResponseMessage>> request)
        {
            try
            {
                var response = await request();
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<T>.Failure(ex.Message, ex);
            }
        }

        private StringContent CreateJsonContent(object data)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

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

        private async Task<ServiceResult<T>> ProcessResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return ServiceResult<T>.Failure(GetErrorMessage(content, response.StatusCode));

            if (string.IsNullOrWhiteSpace(content) || content == "null")
                return ServiceResult<T>.Success(default(T)!);

            try
            {
                var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return ServiceResult<T>.Success(result!);
            }
            catch (JsonException ex)
            {
                return ServiceResult<T>.Failure($"响应数据格式错误: {ex.Message}", ex);
            }
        }

        private string GetErrorMessage(string content, System.Net.HttpStatusCode statusCode)
        {
            try
            {
                var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content, _jsonOptions);
                return problemDetails?.Detail ?? problemDetails?.Title ?? $"请求失败: {statusCode}";
            }
            catch
            {
                return $"请求失败: {statusCode} - {content}";
            }
        }
    }
}