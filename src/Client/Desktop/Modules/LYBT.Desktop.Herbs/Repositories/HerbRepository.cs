using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Repositories
{
    /// <summary>
    /// 药材数据仓储实现 - ADR-002合规版本
    /// 直接调用IHerbApi（Refit HTTP客户端），符合架构决策
    /// </summary>
    public class HerbRepository : IHerbRepository
    {
        private readonly IHerbApi _herbApi;
        private readonly ILogger<HerbRepository> _logger;

        public HerbRepository(
            IHerbApi herbApi,
            ILogger<HerbRepository> logger)
        {
            _herbApi = herbApi;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有草药列表（不分页，用于兼容旧代码）
        /// </summary>
        public async Task<List<HerbDto>> GetAllAsync()
        {
            try
            {
                // 获取第一页，大页数
                var response = await _herbApi.GetHerbsAsync(page: 1, pageSize: 1000);
                return response.Content?.Items ?? new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取草药列表失败");
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取草药详情
        /// </summary>
        public async Task<HerbDto> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _herbApi.GetHerbByIdAsync(id);
                return response.Content ?? throw new InvalidOperationException($"草药 {id} 不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取草药详情失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建新草药（使用CreateDto）
        /// </summary>
        public async Task<HerbDto> CreateAsync(HerbCreateDto herb)
        {
            if (herb == null)
                throw new ArgumentNullException(nameof(herb));

            try
            {
                var response = await _herbApi.CreateHerbAsync(herb);
                return response.Content ?? throw new InvalidOperationException("创建草药失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建草药失败");
                throw;
            }
        }

        /// <summary>
        /// 更新草药信息（使用UpdateDto）
        /// </summary>
        public async Task<HerbDto> UpdateAsync(HerbUpdateDto herb)
        {
            if (herb?.Id == null || herb.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update herb with null or invalid id");
                throw new ArgumentException("Herb ID is required", nameof(herb));
            }

            try
            {
                var response = await _herbApi.UpdateHerbAsync(herb.Id, herb);
                return response.Content ?? throw new InvalidOperationException($"更新草药失败，ID: {herb.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新草药失败，ID: {Id}", herb.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除草药（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _herbApi.DeleteHerbAsync(id);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除草药失败，ID: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 搜索草药（关键字查询）
        /// </summary>
        public async Task<List<HerbDto>> SearchAsync(string keyword)
        {
            try
            {
                var response = await _herbApi.GetHerbsAsync(page: 1, pageSize: 1000, keyword: keyword);
                return response.Content?.Items ?? new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索草药失败，关键字: {Keyword}", keyword);
                throw;
            }
        }

        /// <summary>
        /// 分页查询草药列表（服务端分页）- P0性能修复
        /// </summary>
        public async Task<PagedResult<HerbDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var response = await _herbApi.GetHerbsAsync(page, pageSize, keyword);
                return response.Content ?? new PagedResult<HerbDto>
                {
                    Items = new List<HerbDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询草药失败，Page: {Page}, PageSize: {PageSize}, Keyword: {Keyword}",
                    page, pageSize, keyword);
                throw;
            }
        }
    }
}
