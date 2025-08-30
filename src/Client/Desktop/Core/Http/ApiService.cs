using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Http
{
    /// <summary>
    /// API服务基类 - 提供统一的API调用抽象
    /// </summary>
    public interface IApiService
    {
        Task<TResponse?> GetAsync<TResponse>(string endpoint, object? parameters = null, CancellationToken cancellationToken = default) where TResponse : class;
        Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TResponse : class;
        Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TResponse : class;
        Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TResponse : class;
        Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
        Task<Stream> DownloadAsync(string endpoint, CancellationToken cancellationToken = default);
        Task<TResponse?> UploadAsync<TResponse>(string endpoint, Stream file, string fileName, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) where TResponse : class;
    }

    /// <summary>
    /// API服务实现
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache? _cache;
        private readonly ILogger<ApiService>? _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly RequestDeduplicator _deduplicator;

        public ApiService(
            HttpClient httpClient,
            IMemoryCache? cache = null,
            ILogger<ApiService>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cache = cache;
            _logger = logger;
            _deduplicator = new RequestDeduplicator();
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// GET请求
        /// </summary>
        public async Task<TResponse?> GetAsync<TResponse>(
            string endpoint,
            object? parameters = null,
            CancellationToken cancellationToken = default)
            where TResponse : class
        {
            var url = BuildUrl(endpoint, parameters);
            
            // 尝试从缓存获取
            if (_cache != null)
            {
                var cacheKey = $"GET:{url}";
                var cached = _cache.Get<TResponse>(cacheKey);
                if (cached != null)
                {
                    _logger?.LogDebug($"缓存命中: {url}");
                    return cached;
                }
            }
            
            // 去重处理
            return await _deduplicator.ExecuteAsync(url, async () =>
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                var result = await HandleResponseAsync<TResponse>(response);
                
                // 缓存成功的响应
                if (_cache != null && response.IsSuccessStatusCode && result != null)
                {
                    var cacheKey = $"GET:{url}";
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                }
                
                return result;
            });
        }

        /// <summary>
        /// POST请求
        /// </summary>
        public async Task<TResponse?> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TResponse : class
        {
            var content = CreateJsonContent(request);
            
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            return await HandleResponseAsync<TResponse>(response);
        }

        /// <summary>
        /// PUT请求
        /// </summary>
        public async Task<TResponse?> PutAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TResponse : class
        {
            var content = CreateJsonContent(request);
            
            using var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
            return await HandleResponseAsync<TResponse>(response);
        }

        /// <summary>
        /// PATCH请求
        /// </summary>
        public async Task<TResponse?> PatchAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TResponse : class
        {
            var content = CreateJsonContent(request);
            
            using var response = await _httpClient.PatchAsync(endpoint, content, cancellationToken);
            return await HandleResponseAsync<TResponse>(response);
        }

        /// <summary>
        /// DELETE请求
        /// </summary>
        public async Task<bool> DeleteAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        public async Task<Stream> DownloadAsync(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStreamAsync();
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        public async Task<TResponse?> UploadAsync<TResponse>(
            string endpoint,
            Stream file,
            string fileName,
            Dictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
            where TResponse : class
        {
            using var content = new MultipartFormDataContent();
            
            // 添加文件
            var fileContent = new StreamContent(file);
            content.Add(fileContent, "file", fileName);
            
            // 添加元数据
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    content.Add(new StringContent(kvp.Value), kvp.Key);
                }
            }
            
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            return await HandleResponseAsync<TResponse>(response);
        }

        /// <summary>
        /// 处理响应
        /// </summary>
        private async Task<TResponse?> HandleResponseAsync<TResponse>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError($"API错误: {response.StatusCode}, 内容: {content}");
                throw new ApiException(response.StatusCode, content);
            }
            
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }
            
            try
            {
                return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, $"JSON反序列化失败: {content}");
                throw new ApiException(response.StatusCode, "响应格式错误", "GET", content, ex);
            }
        }

        /// <summary>
        /// 创建JSON内容
        /// </summary>
        private StringContent CreateJsonContent<T>(T data)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// 构建URL
        /// </summary>
        private string BuildUrl(string endpoint, object? parameters)
        {
            if (parameters == null)
            {
                return endpoint;
            }
            
            var queryString = BuildQueryString(parameters);
            return string.IsNullOrEmpty(queryString) 
                ? endpoint 
                : $"{endpoint}?{queryString}";
        }

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        private string BuildQueryString(object parameters)
        {
            var properties = parameters.GetType().GetProperties();
            var queryParts = new List<string>();
            
            foreach (var property in properties)
            {
                var value = property.GetValue(parameters);
                if (value != null)
                {
                    queryParts.Add($"{property.Name}={Uri.EscapeDataString(value.ToString()!)}");
                }
            }
            
            return string.Join("&", queryParts);
        }
    }

    // ApiException 现在使用 LYBT.Shared.Models.Exceptions.ApiException

    /// <summary>
    /// 请求去重器
    /// </summary>
    internal class RequestDeduplicator
    {
        private readonly Dictionary<string, Task<object?>> _pendingRequests = new();
        private readonly object _lock = new();

        public async Task<T?> ExecuteAsync<T>(string key, Func<Task<T?>> factory)
        {
            Task<object?> task;
            bool isNew = false;
            
            lock (_lock)
            {
                if (!_pendingRequests.TryGetValue(key, out task!))
                {
                    task = ExecuteInternalAsync(factory);
                    _pendingRequests[key] = task;
                    isNew = true;
                }
            }
            
            try
            {
                var result = await task;
                return (T?)result;
            }
            finally
            {
                if (isNew)
                {
                    lock (_lock)
                    {
                        _pendingRequests.Remove(key);
                    }
                }
            }
        }

        private async Task<object?> ExecuteInternalAsync<T>(Func<Task<T?>> factory)
        {
            return await factory();
        }
    }

    /// <summary>
    /// 泛型API服务
    /// </summary>
    public class ApiService<TEntity> : IApiService where TEntity : class
    {
        private readonly IApiService _apiService;
        private readonly string _baseEndpoint;

        public ApiService(IApiService apiService, string baseEndpoint)
        {
            _apiService = apiService;
            _baseEndpoint = baseEndpoint;
        }

        public async Task<IEnumerable<TEntity>?> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _apiService.GetAsync<IEnumerable<TEntity>>(_baseEndpoint, cancellationToken: cancellationToken);
        }

        public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _apiService.GetAsync<TEntity>($"{_baseEndpoint}/{id}", cancellationToken: cancellationToken);
        }

        public async Task<TEntity?> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            return await _apiService.PostAsync<TEntity, TEntity>(_baseEndpoint, entity, cancellationToken);
        }

        public async Task<TEntity?> UpdateAsync(Guid id, TEntity entity, CancellationToken cancellationToken = default)
        {
            return await _apiService.PutAsync<TEntity, TEntity>($"{_baseEndpoint}/{id}", entity, cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _apiService.DeleteAsync($"{_baseEndpoint}/{id}", cancellationToken);
        }

        // IApiService implementation
        public Task<TResponse?> GetAsync<TResponse>(string endpoint, object? parameters = null, CancellationToken cancellationToken = default)
            where TResponse : class
            => _apiService.GetAsync<TResponse>(endpoint, parameters, cancellationToken);

        public Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
            where TResponse : class
            => _apiService.PostAsync<TRequest, TResponse>(endpoint, request, cancellationToken);

        public Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
            where TResponse : class
            => _apiService.PutAsync<TRequest, TResponse>(endpoint, request, cancellationToken);

        public Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
            where TResponse : class
            => _apiService.PatchAsync<TRequest, TResponse>(endpoint, request, cancellationToken);

        public Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
            => _apiService.DeleteAsync(endpoint, cancellationToken);

        public Task<Stream> DownloadAsync(string endpoint, CancellationToken cancellationToken = default)
            => _apiService.DownloadAsync(endpoint, cancellationToken);

        public Task<TResponse?> UploadAsync<TResponse>(string endpoint, Stream file, string fileName, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
            where TResponse : class
            => _apiService.UploadAsync<TResponse>(endpoint, file, fileName, metadata, cancellationToken);
    }
}