using AutoMapper;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services
{

    /// <summary>
    /// 药材查询服务 - UltraThink架构
    /// 职责：复杂查询、搜索、筛选、分页等只读操作
    /// </summary>
    public class HerbQueryService : IHerbQueryService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbQueryService> _logger;

        public HerbQueryService(
            AppDbContext context,
            IMapper mapper,
            ILogger<HerbQueryService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取所有启用状态的药材列表
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
        {
            try
            {
                var herbs = await BuildBaseQuery()
                    .OrderBy(h => h.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(herbs);
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
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
        {
            try
            {
                query ??= new HerbPagedQueryDto();

                var queryable = BuildBaseQuery();

                // 关键词搜索
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    var keyword = query.Keyword.Trim();
                    queryable = queryable.Where(h =>
                        h.Name.Contains(keyword) ||
                        (h.PinYinCode != null && h.PinYinCode.Contains(keyword)) ||
                        (h.Origin != null && h.Origin.Contains(keyword)) ||
                        (h.Effect != null && h.Effect.Contains(keyword)));
                }

                // 价格范围筛选
                if (query.MinPrice.HasValue)
                {
                    queryable = queryable.Where(h => h.Price >= query.MinPrice.Value);
                }

                if (query.MaxPrice.HasValue)
                {
                    queryable = queryable.Where(h => h.Price <= query.MaxPrice.Value);
                }

                // 状态筛选
                if (query.Status.HasValue)
                {
                    queryable = queryable.Where(h => h.Status == query.Status.Value);
                }

                // 总数量
                var totalCount = await queryable.CountAsync();

                // 排序 - 简化处理，默认按名称排序
                queryable = queryable.OrderBy(h => h.Name);

                // 分页
                var pageIndex = Math.Max(query.PageIndex, 1);
                var pageSize = Math.Clamp(query.PageSize, 10, 100);

                var herbs = await queryable
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(herbs);
                var pagedResult = new PagedResult<HerbDto>(dtos, totalCount, pageIndex, pageSize);

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

                var herbs = await BuildBaseQuery()
                    .Where(h => h.Name.Contains(keyword) || (h.PinYinCode != null && h.PinYinCode.Contains(keyword)))
                    .OrderByDescending(h => h.Name.StartsWith(keyword)) // 以关键词开头的排前面
                    .ThenByDescending(h => h.PinYinCode != null && h.PinYinCode.StartsWith(keyword.ToUpper()))
                    .ThenBy(h => h.Name)
                    .Take(50) // 限制搜索结果数量
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(herbs);
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
                var herbs = await _context.Herbs
                    .Where(h => h.Status == CommonStatus.Enabled)
                    .OrderBy(h => h.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(herbs);
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

                var herbs = await _context.Herbs
                    .Where(h => ids.Contains(h.Id) && h.Status != CommonStatus.Disabled)
                    .OrderBy(h => h.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(herbs);
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

                var herbs = await BuildBaseQuery()
                    .Where(h => h.Price >= minPrice && h.Price <= maxPrice)
                    .OrderBy(h => h.Price)
                    .ThenBy(h => h.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(herbs);
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

                var herb = await BuildBaseQuery()
                    .FirstOrDefaultAsync(h => h.Name == name.Trim());

                if (herb == null)
                {
                    return ServiceResult<HerbDto>.Failure($"未找到名称为 '{name}' 的药材");
                }

                var dto = _mapper.Map<HerbDto>(herb);
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

                var herbs = await BuildBaseQuery()
                    .OrderBy(h => h.Name) // 简化实现，实际应该按使用频率排序
                    .Take(count)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(herbs);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取热门药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"获取热门药材失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 构建基础查询 - 只查询启用状态的药材
        /// </summary>
        private IQueryable<Herb> BuildBaseQuery()
        {
            return _context.Herbs.Where(h => h.Status == CommonStatus.Enabled);
        }

        #endregion 私有方法
    }
}
