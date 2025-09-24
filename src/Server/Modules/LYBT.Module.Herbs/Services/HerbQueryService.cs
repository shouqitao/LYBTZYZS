using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services
{

    /// <summary>
    /// 药材查询服务 - UltraThink架构重构版
    /// 职责：复杂查询、搜索、筛选、分页等只读操作
    /// 改为使用ReadRepository，移除直接的DbContext依赖
    /// </summary>
    public class HerbQueryService : IHerbQueryService
    {
        private readonly IHerbReadRepository _readRepository;
        private readonly ILogger<HerbQueryService> _logger;

        public HerbQueryService(
            IHerbReadRepository readRepository,
            ILogger<HerbQueryService> logger)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<HerbDto>.Failure("药材ID不能为空");
                }

                var dto = await _readRepository.GetHerbDtoByIdAsync(id);
                if (dto == null)
                {
                    return ServiceResult<HerbDto>.Failure("药材不存在");
                }

                return ServiceResult<HerbDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败: {Id}", id);
                return ServiceResult<HerbDto>.Failure($"获取药材详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有启用状态的药材列表
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
        {
            try
            {
                var dtos = await _readRepository.GetAllHerbDtosAsync();
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材列表失败");
                return ServiceResult<List<HerbDto>>.Failure($"获取药材列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbSearchDto query)
        {
            try
            {
                query ??= new HerbSearchDto();

                var pageIndex = Math.Max(query.PageIndex, 1);
                var pageSize = Math.Clamp(query.PageSize, 10, 100);

                var searchDto = new HerbSearchDto
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Keyword = query.Keyword,
                    MinPrice = query.MinPrice,
                    MaxPrice = query.MaxPrice,
                    IncludeExpired = query.IncludeExpired
                };

                var pagedResult = await _readRepository.GetPagedHerbDtosAsync(searchDto);

                return ServiceResult<PagedResult<HerbDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询药材失败");
                return ServiceResult<PagedResult<HerbDto>>.Failure($"分页查询药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索药材（根据名称、拼音码）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return await GetAllAsync();
                }

                keyword = keyword.Trim();
                var dtos = await _readRepository.SearchHerbDtosAsync(keyword, 50);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败: {Keyword}", keyword);
                return ServiceResult<List<HerbDto>>.Failure($"搜索药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取可用药材列表（状态为启用）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        {
            try
            {
                var dtos = await _readRepository.GetAllHerbDtosAsync();
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用药材列表失败");
                return ServiceResult<List<HerbDto>>.Failure($"获取可用药材列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据ID列表批量获取药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
                }

                var dtos = await _readRepository.GetHerbDtosByIdsAsync(ids);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量获取药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"批量获取药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 按价格区间查询药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            try
            {
                if (minPrice < 0 || maxPrice < 0)
                {
                    return ServiceResult<List<HerbDto>>.Failure("价格不能为负数");
                }

                if (minPrice > maxPrice)
                {
                    return ServiceResult<List<HerbDto>>.Failure("最小价格不能大于最大价格");
                }

                var dtos = await _readRepository.GetHerbDtosByPriceRangeAsync(minPrice, maxPrice);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按价格区间查询药材失败: {MinPrice}-{MaxPrice}", minPrice, maxPrice);
                return ServiceResult<List<HerbDto>>.Failure($"按价格区间查询药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据名称精确查找药材
        /// </summary>
        public async Task<ServiceResult<HerbDto>> GetByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return ServiceResult<HerbDto>.Failure("药材名称不能为空");
                }

                var dto = await _readRepository.GetHerbDtoByNameAsync(name.Trim());
                if (dto == null)
                {
                    return ServiceResult<HerbDto>.Failure($"未找到名称为 '{name}' 的药材");
                }

                return ServiceResult<HerbDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据名称查找药材失败: {Name}", name);
                return ServiceResult<HerbDto>.Failure($"根据名称查找药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取热门药材（按使用频率排序）
        /// 注：当前简化实现，按名称排序。实际项目中可根据处方使用频率统计
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetPopularHerbsAsync(int count = 20)
        {
            try
            {
                count = Math.Clamp(count, 1, 50);

                var dtos = await _readRepository.GetPopularHerbDtosAsync(count);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取热门药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"获取热门药材失败: {ex.Message}");
            }
        }


    }
}
