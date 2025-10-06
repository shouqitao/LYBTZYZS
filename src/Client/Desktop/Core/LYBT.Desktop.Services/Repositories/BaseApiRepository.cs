using LYBT.Desktop.Services.Http;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Repositories
{
    /// <summary>
    /// API Repository基类 - UltraThink架构
    /// </summary>
    public abstract class BaseApiRepository<T> where T : class
    {
        protected readonly IApiService _apiService;
        protected readonly ILogger _logger;
        protected readonly string _endpoint;

        protected BaseApiRepository(
            IApiService apiService,
            ILogger logger,
            string endpoint)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            // 服务端返回 ApiResponse<PagedResult<T>> 格式
            var response = await _apiService.GetAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<LYBT.Shared.Models.Contracts.Common.PagedResult<T>>>(_endpoint);
            
            if (response?.Success == true && response.Data?.Items != null)
            {
                return response.Data.Items.ToList();
            }
            
            _logger?.LogWarning("API返回失败或数据为空: {Message}", response?.Message ?? "未知错误");
            return new List<T>();
        }

        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID不能为空", nameof(id));

            return (await _apiService.GetAsync<T>($"{_endpoint}/{id}"))!;
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return (await _apiService.PostAsync<T, T>(_endpoint, entity))!;
        }

        public virtual async Task<T> UpdateAsync(Guid id, T entity)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID不能为空", nameof(id));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return (await _apiService.PutAsync<T, T>($"{_endpoint}/{id}", entity))!;
        }

        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID不能为空", nameof(id));

            await _apiService.DeleteAsync($"{_endpoint}/{id}");
            return true;
        }

        public virtual async Task<List<T>> SearchAsync(string keyword)
        {
            var query = new { keyword };
            var result = await _apiService.GetAsync<List<T>>($"{_endpoint}/search", query);
            return result ?? new List<T>();
        }
    }
}
