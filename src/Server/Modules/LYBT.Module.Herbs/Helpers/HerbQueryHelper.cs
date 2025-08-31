using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Helpers
{
    /// <summary>
    /// HerbService查询助手类 - UltraThink Helper模式
    /// 负责所有复杂查询、搜索和统计逻辑
    /// </summary>
    public class HerbQueryHelper
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbQueryHelper> _logger;

        public HerbQueryHelper(AppDbContext context, IMapper mapper, ILogger<HerbQueryHelper> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 复杂分页查询 - 支持多维度搜索条件
        /// </summary>
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
        {
            try
            {
                var dbQuery = _context.Herbs.AsQueryable();

                // 药材名称搜索
                if (!string.IsNullOrWhiteSpace(query.Name))
                {
                    var keyword = query.Name.Trim();
                    dbQuery = dbQuery.Where(h => h.Name.Contains(keyword));
                }

                // 拼音码搜索
                if (!string.IsNullOrWhiteSpace(query.PinYinCode))
                {
                    var pinyin = query.PinYinCode.Trim().ToUpperInvariant();
                    dbQuery = dbQuery.Where(h => h.PinYinCode != null && h.PinYinCode.Contains(pinyin));
                }

                // 产地搜索
                if (!string.IsNullOrWhiteSpace(query.Origin))
                {
                    var origin = query.Origin.Trim();
                    dbQuery = dbQuery.Where(h => h.Origin != null && h.Origin.Contains(origin));
                }

                // 规格搜索
                if (!string.IsNullOrWhiteSpace(query.Spec))
                {
                    var spec = query.Spec.Trim();
                    dbQuery = dbQuery.Where(h => h.Spec != null && h.Spec.Contains(spec));
                }

                // 价格范围筛选
                if (query.MinPrice.HasValue)
                {
                    dbQuery = dbQuery.Where(h => h.Price >= query.MinPrice.Value);
                }
                if (query.MaxPrice.HasValue)
                {
                    dbQuery = dbQuery.Where(h => h.Price <= query.MaxPrice.Value);
                }

                // 状态筛选
                if (query.Status.HasValue)
                {
                    dbQuery = dbQuery.Where(h => h.Status == query.Status.Value);
                }
                else
                {
                    // 默认只显示启用的药材
                    dbQuery = dbQuery.Where(h => h.Status == CommonStatus.Enabled);
                }

                // 获取总数
                var total = await dbQuery.CountAsync();

                // 分页查询
                var models = await dbQuery
                    .OrderBy(h => h.Name)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(models);

                var result = new PagedResult<HerbDto>
                {
                    TotalCount = total,
                    Items = dtos,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询药材失败");
                return ServiceResult<PagedResult<HerbDto>>.Failure("分页查询药材失败", ex);
            }
        }

        /// <summary>
        /// 药材搜索 - 支持名称和拼音码模糊匹配
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());
                }

                keyword = keyword.ToLower();
                var models = await _context.Herbs
                    .Where(h => h.Status == CommonStatus.Enabled && (
                        h.Name.ToLower().Contains(keyword) ||
                        (h.PinYinCode != null && h.PinYinCode.ToLower().Contains(keyword))
                    ))
                    .OrderBy(h => h.Name)
                    .Take(20)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(models);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败: {Keyword}", keyword);
                return ServiceResult<List<HerbDto>>.Failure("搜索药材失败", ex);
            }
        }

        /// <summary>
        /// 获取可用药材列表（启用状态）
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        {
            try
            {
                var models = await _context.Herbs
                    .Where(h => h.Status == CommonStatus.Enabled)
                    .OrderBy(h => h.Name)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(models);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用药材列表失败");
                return ServiceResult<List<HerbDto>>.Failure("获取可用药材列表失败", ex);
            }
        }

        /// <summary>
        /// 根据ID列表批量获取药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            try
            {
                var models = await _context.Herbs
                    .Where(h => ids.Contains(h.Id))
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(models);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量获取药材失败");
                return ServiceResult<List<HerbDto>>.Failure("批量获取药材失败", ex);
            }
        }

        /// <summary>
        /// 按价格区间查询药材
        /// </summary>
        public async Task<ServiceResult<List<HerbDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            try
            {
                var herbs = await _context.Herbs
                    .Where(h => h.Status == CommonStatus.Enabled && h.Price >= minPrice && h.Price <= maxPrice)
                    .OrderBy(h => h.Price)
                    .ToListAsync();

                var dtos = _mapper.Map<List<HerbDto>>(herbs);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按价格区间查询药材失败: {MinPrice}-{MaxPrice}", minPrice, maxPrice);
                return ServiceResult<List<HerbDto>>.Failure("按价格区间查询药材失败", ex);
            }
        }

        /// <summary>
        /// 获取药材统计数据
        /// </summary>
        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            try
            {
                var herbs = await _context.Herbs.Where(h => h.Status == CommonStatus.Enabled).ToListAsync();
                var stats = new Dictionary<int, int>
                {
                    { 0, herbs.Count }, // 总数
                    { 1, 0 },          // 缺货数（简化版不支持库存）
                    { 2, herbs.Count }, // 充足数（默认所有都充足）
                    { 3, 0 }           // 过期数（简化版不支持过期管理）
                };

                return ServiceResult<Dictionary<int, int>>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材统计数据失败");
                return ServiceResult<Dictionary<int, int>>.Failure("获取统计数据失败", ex);
            }
        }

        /// <summary>
        /// 获取已禁用药材统计（管理员功能）
        /// </summary>
        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            try
            {
                var herbs = await _context.Herbs.Where(h => h.Status == CommonStatus.Enabled).ToListAsync();

                var stats = new HerbStockStatisticsDto
                {
                    TotalCount = herbs.Count,
                    OutOfStockCount = 0, // 简化版不支持库存
                    WarningCount = 0, // 简化版不支持库存预警
                    SufficientCount = herbs.Count, // 默认所有药材都充足
                    TotalStockValue = 0, // 简化版不计算库存价值
                    ExpiringCount = 0, // 简化版不支持过期管理
                    ExpiredCount = 0 // 简化版不支持过期管理
                };

                return ServiceResult<HerbStockStatisticsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取库存统计失败");
                return ServiceResult<HerbStockStatisticsDto>.Failure("获取库存统计失败", ex);
            }
        }

        /// <summary>
        /// 构建启用状态的基础查询
        /// </summary>
        public IQueryable<Herb> GetEnabledHerbsQuery()
        {
            return _context.Herbs.Where(h => h.Status == CommonStatus.Enabled);
        }

        /// <summary>
        /// 构建带搜索条件的查询
        /// </summary>
        public IQueryable<Herb> BuildSearchQuery(string? keyword)
        {
            var query = GetEnabledHerbsQuery();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchTerm = keyword.ToLower();
                query = query.Where(h => 
                    h.Name.ToLower().Contains(searchTerm) ||
                    (h.PinYinCode != null && h.PinYinCode.ToLower().Contains(searchTerm)));
            }

            return query;
        }
    }
}