using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Core.Models.Authentication;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Services
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
            
            // 设置基础URL - 使用实际服务器地址
            _httpClient.BaseAddress = new Uri("http://192.168.190.243:5000/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LYBT.WPF.Client");
        }

        /// <summary>
        /// 发送GET请求
        /// </summary>
        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                await EnsureAuthenticated();
                var response = await _httpClient.GetAsync(endpoint);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
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
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// 发送PUT请求
        /// </summary>
        public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
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
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// 发送DELETE请求
        /// </summary>
        public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
        {
            try
            {
                await EnsureAuthenticated();
                var response = await _httpClient.DeleteAsync(endpoint);
                return await ProcessResponse<T>(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// 发送PATCH请求
        /// </summary>
        public async Task<ApiResponse<T>> PatchAsync<T>(string endpoint, object data)
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
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
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
        private async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new ApiResponse<T> { IsSuccess = false, Message = "响应数据解析失败" };
                }
                catch (JsonException ex)
                {
                    return new ApiResponse<T>
                    {
                        IsSuccess = false,
                        Message = $"响应数据格式错误: {ex.Message}"
                    };
                }
            }
            else
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    Message = $"请求失败: {response.StatusCode} - {content}"
                };
            }
        }
    }
}